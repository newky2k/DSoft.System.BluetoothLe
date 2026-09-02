using System;
using System.BluetoothLe.EventArgs;
using System.BluetoothLe.Utils;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace System.BluetoothLe
{
    /// <summary>
    /// The Bluetooth Low Energy central. Discovers devices, connects to them and keeps track of which of
    /// them are currently connected.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An adapter is obtained from <see cref="BluetoothLE.Adapter"/>; it is not constructed directly, because
    /// every platform needs a native manager handed to it that only the platform initialisation can supply.
    /// </para>
    /// <para>
    /// Every event on this type is raised on whichever thread the platform delivered the callback on - the
    /// CoreBluetooth delegate queue on Apple, a binder thread on Android, a thread-pool thread on Windows.
    /// Handlers are therefore not marshalled to any UI thread, and a consumer that updates a view model from
    /// one must marshal it itself. An exception thrown by a handler is traced and swallowed rather than left
    /// to unwind into a native frame, where on Apple and Android it terminates the process.
    /// </para>
    /// </remarks>
    public partial class Adapter : IDisposable
    {
        #region Fields

        // Held as an int rather than a bool so that starting a scan can be a single atomic compare-and-exchange.
        // With a bool the "am I already scanning?" check and the "I am scanning now" assignment were two steps,
        // and two threads calling StartScanningForDevicesAsync at once could both pass the check.
        private int _isScanning;

        private CancellationTokenSource _scanCancellationTokenSource;

        // Completed by CleanupScan on every exit path, so StopScanningForDevicesAsync can genuinely wait for the
        // scan to have finished rather than just requesting that it stop.
        private TaskCompletionSource<bool> _scanCompletionSource;

        private Func<Device, bool> _currentScanDeviceFilter;

        // Set by HandleScanFailed immediately before it cancels the scan, so that the cancellation the awaiting
        // StartScanningForDevicesAsync observes can be reported as the failure it actually was.
        private ScanFailureReason? _scanFailureReason;

        // Keyed by device id rather than by a platform-specific string. Android keyed the old public dictionary
        // by MAC address and Apple and Windows by a stringified Guid, so a consumer reading it had to know which
        // platform it was running on to construct a key.
        private readonly ConcurrentDictionary<Guid, Device> _connectedDeviceRegistry = new ConcurrentDictionary<Guid, Device>();

        private bool _isDisposed;

        #endregion

        #region Events

        /// <summary>
        /// Raised for every advertisement received from a device that passes the current scan's filter,
        /// including repeat advertisements from a device that has already been discovered.
        /// </summary>
        public event EventHandler<DeviceEventArgs> DeviceAdvertised;

        /// <summary>
        /// Raised once per device per scan, the first time a device passing the current scan's filter is seen.
        /// The discovered-device registry is cleared at the start of every scan, so a device seen in a previous
        /// scan is reported again in the next one.
        /// </summary>
        public event EventHandler<DeviceEventArgs> DeviceDiscovered;

        /// <summary>Raised when a device reaches the connected state.</summary>
        public event EventHandler<DeviceEventArgs> DeviceConnected;

        /// <summary>Raised when a device disconnects because it was asked to.</summary>
        public event EventHandler<DeviceEventArgs> DeviceDisconnected;

        /// <summary>Raised when a connected device disconnects without having been asked to.</summary>
        public event EventHandler<DeviceErrorEventArgs> DeviceConnectionLost;

        /// <summary>Raised when a connection attempt fails.</summary>
        public event EventHandler<DeviceErrorEventArgs> DeviceConnectionError;

        /// <summary>
        /// Raised when a scan ends because <see cref="ScanTimeout"/> elapsed. It is not raised for a scan that
        /// was cancelled, stopped or failed.
        /// </summary>
        public event EventHandler ScanTimeoutElapsed;

        /// <summary>
        /// Raised when the platform refuses to start a scan, or abandons one it had started. The awaiting
        /// <see cref="StartScanningForDevicesAsync"/> faults with an <see cref="AdapterScanException"/> carrying
        /// the same reason; this event exists for consumers that start scans without holding onto the task.
        /// </summary>
        public event EventHandler<ScanFailedEventArgs> ScanFailed;

        #endregion

        #region Properties

        /// <summary>Whether a scan is currently running.</summary>
        public bool IsScanning => Volatile.Read(ref _isScanning) == 1;

        /// <summary>
        /// How long a scan runs before it stops on its own, <b>in milliseconds</b>. The default is ten seconds.
        /// </summary>
        public int ScanTimeout { get; set; } = 10000;

        /// <summary>
        /// The power/latency trade-off requested of the platform scanner. Honoured on Android and Windows;
        /// Apple offers no equivalent knob and ignores it.
        /// </summary>
        public ScanMode ScanMode { get; set; } = ScanMode.LowPower;

        /// <summary>
        /// The devices seen during the current or most recent scan. Cleared at the start of every scan.
        /// </summary>
        protected ConcurrentDictionary<Guid, Device> DiscoveredDevicesRegistry { get; } = new ConcurrentDictionary<Guid, Device>();

        /// <summary>
        /// The devices discovered by the current or most recent scan. Cleared when a new scan starts.
        /// </summary>
        public virtual IReadOnlyList<Device> DiscoveredDevices => DiscoveredDevicesRegistry.Values.ToList();

        /// <summary>
        /// The devices this adapter currently believes to be connected. This is a snapshot; it is not live.
        /// </summary>
        public IReadOnlyList<Device> ConnectedDevices => _connectedDeviceRegistry.Values.ToList();

        #endregion

        #region Scanning

        /// <summary>
        /// Scans for devices until <see cref="ScanTimeout"/> elapses, <see cref="StopScanningForDevicesAsync"/>
        /// is called, or <paramref name="cancellationToken"/> is cancelled.
        /// </summary>
        /// <param name="serviceUuids">
        /// Services a device must advertise to be reported. Applied by the platform scanner where it can be,
        /// so filtering here is materially cheaper than filtering in <paramref name="deviceFilter"/>. Null or
        /// empty scans for everything.
        /// </param>
        /// <param name="deviceFilter">
        /// An additional managed filter applied to every advertisement. A device it rejects raises neither
        /// <see cref="DeviceAdvertised"/> nor <see cref="DeviceDiscovered"/>.
        /// </param>
        /// <param name="allowDuplicatesKey">
        /// Whether to report every advertisement from a device rather than coalescing them. Honoured on Apple.
        /// Android and Windows have no equivalent setting and silently ignore it - on those platforms
        /// <see cref="DeviceAdvertised"/> already fires per advertisement.
        /// </param>
        /// <param name="cancellationToken">Cancels the scan. A cancelled scan completes normally rather than faulting.</param>
        /// <returns>A task that completes when the scan has ended and the platform scanner has been stopped.</returns>
        /// <exception cref="InvalidOperationException">
        /// A scan is already running. This is a deliberate change in 4.0: the previous behaviour was to trace
        /// and return, so a caller awaiting the returned task believed a scan it did not own had finished the
        /// instant it started, and the two callers' filters and results silently interfered.
        /// </exception>
        /// <exception cref="AdapterScanException">The platform refused to start, or abandoned, the scan.</exception>
        public async Task StartScanningForDevicesAsync(Guid[] serviceUuids = null, Func<Device, bool> deviceFilter = null, bool allowDuplicatesKey = false, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (Interlocked.CompareExchange(ref _isScanning, 1, 0) != 0)
            {
                throw new InvalidOperationException("A scan is already in progress. Await the running scan, or call StopScanningForDevicesAsync, before starting another.");
            }

            // The token source is held in a local as well as in the field. Everything on this path uses the
            // local, so a concurrent StopScanningForDevicesAsync or Dispose clearing the field cannot turn a
            // dereference here into a NullReferenceException part way through the scan.
            var cts = new CancellationTokenSource();
            var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            _scanCancellationTokenSource = cts;
            _scanCompletionSource = completion;
            _scanFailureReason = null;
            _currentScanDeviceFilter = deviceFilter ?? (d => true);
            serviceUuids ??= [];

            var timedOut = false;

            try
            {
                DiscoveredDevicesRegistry.Clear();

                using (cancellationToken.Register(() =>
                {
                    try
                    {
                        cts.Cancel();
                    }
                    catch (ObjectDisposedException)
                    {
                        // The scan finished and disposed its source between the caller cancelling and this
                        // callback running. There is nothing left to cancel.
                    }
                }))
                {
                    await StartScanningForDevicesNativeAsync(serviceUuids, allowDuplicatesKey, cts.Token).ConfigureAwait(false);
                    await Task.Delay(ScanTimeout, cts.Token).ConfigureAwait(false);

                    timedOut = true;
                    Trace.Message("Adapter: Scan timeout has elapsed.");
                }
            }
            catch (OperationCanceledException)
            {
                // Catching OperationCanceledException rather than TaskCanceledException alone: a token
                // cancelled inside a native start reaches us as the base type, and the previous code let that
                // escape without ever clearing IsScanning, wedging the adapter for the process lifetime.
                var reason = _scanFailureReason;
                if (reason.HasValue)
                {
                    throw new AdapterScanException(reason.Value);
                }

                Trace.Message("Adapter: Scan was cancelled.");
            }
            finally
            {
                // In the finally rather than in each branch, so that a native start that throws something
                // unexpected still stops the scanner and releases the scanning state.
                CleanupScan();
            }

            if (timedOut)
            {
                // Raised after CleanupScan, so that a handler is free to start the next scan.
                Raise(ScanTimeoutElapsed, nameof(ScanTimeoutElapsed));
            }
        }

        /// <summary>
        /// Stops the running scan and waits for it to have finished.
        /// </summary>
        /// <remarks>
        /// In 4.0 the returned task genuinely completes only once the scan has stopped and the platform
        /// scanner has been torn down. Previously it completed as soon as cancellation had been requested, so
        /// a caller that stopped one scan and immediately started another raced the first scan's teardown.
        /// </remarks>
        public Task StopScanningForDevicesAsync()
        {
            var completion = Volatile.Read(ref _scanCompletionSource);
            var cts = Volatile.Read(ref _scanCancellationTokenSource);

            if (completion == null && cts == null)
            {
                Trace.Message("Adapter: No scan is running.");
                return Task.CompletedTask;
            }

            if (cts != null)
            {
                try
                {
                    if (!cts.IsCancellationRequested)
                    {
                        cts.Cancel();
                    }
                }
                catch (ObjectDisposedException)
                {
                    // The scan completed between the snapshot above and this call; nothing to stop.
                }
            }

            return completion?.Task ?? Task.CompletedTask;
        }

        private void CleanupScan()
        {
            Trace.Message("Adapter: Stopping the scan for devices.");

            try
            {
                StopScanNative();
            }
            catch (Exception ex)
            {
                // A platform that refuses to stop a scanner must not prevent the adapter from returning to a
                // usable state; that failure mode is exactly what left IsScanning stuck true before.
                Trace.Message("Adapter: Stopping the native scan failed: {0}", ex);
            }

            Interlocked.Exchange(ref _scanCancellationTokenSource, null)?.Dispose();
            _currentScanDeviceFilter = null;

            var completion = Interlocked.Exchange(ref _scanCompletionSource, null);

            // Cleared before the waiters are released, so anyone woken by StopScanningForDevicesAsync sees
            // IsScanning false and may start a new scan immediately.
            Volatile.Write(ref _isScanning, 0);

            completion?.TrySetResult(true);
        }

        #endregion

        #region Connecting

        /// <summary>
        /// Connects to a device that has already been discovered.
        /// </summary>
        /// <param name="device">The device to connect to.</param>
        /// <param name="connectParameters">Platform-specific connection options.</param>
        /// <param name="cancellationToken">Abandons the connection attempt and asks the platform to cancel it.</param>
        /// <exception cref="DeviceConnectionException">The platform reported that the connection failed.</exception>
        public async Task ConnectToDeviceAsync(Device device, ConnectParameters connectParameters = default, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (device is null)
            {
                throw new ArgumentNullException(nameof(device));
            }

            if (device.State == DeviceState.Connected)
            {
                return;
            }

            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                await TaskBuilder.FromEvent<bool, EventHandler<DeviceEventArgs>, EventHandler<DeviceErrorEventArgs>>(
                    execute: () => ConnectToDeviceNativeAsync(device, connectParameters, cts.Token),

                    getCompleteHandler: (complete, reject) => (sender, args) =>
                    {
                        if (args.Device.Id == device.Id)
                        {
                            Trace.Message("ConnectToDeviceAsync Connected: {0} {1}", args.Device.Id, args.Device.Name);
                            complete(true);
                        }
                    },
                    subscribeComplete: handler => DeviceConnected += handler,
                    unsubscribeComplete: handler => DeviceConnected -= handler,

                    getRejectHandler: reject => (sender, args) =>
                    {
                        if (args.Device.Id == device.Id)
                        {
                            Trace.Message("ConnectAsync Error: {0} {1}", args.Device.Id, args.Device.Name);
                            reject(new DeviceConnectionException(args.Device.Id, args.Device.Name, args.ErrorMessage));
                        }
                    },

                    subscribeReject: handler => DeviceConnectionError += handler,
                    unsubscribeReject: handler => DeviceConnectionError -= handler,
                    token: cts.Token).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Connects to a device by identifier, whether or not it has been discovered in this session.
        /// </summary>
        /// <remarks>
        /// A device already in the discovered-device registry is reused rather than reconstructed. Minting a
        /// second instance for the same peripheral produced a second <c>BluetoothGatt</c> on Android, and left
        /// a consumer subscribed to notifications on an instance the library no longer used.
        /// </remarks>
        /// <exception cref="DeviceNotFoundException">The platform has no record of a device with this identifier.</exception>
        /// <exception cref="DeviceConnectionException">The platform reported that the connection failed.</exception>
        public Task<Device> ConnectToKnownDeviceAsync(Guid deviceGuid, ConnectParameters connectParameters = default, CancellationToken cancellationToken = default)
            => ConnectToKnownDeviceCoreAsync(deviceGuid, connectParameters, throwIfNotFound: true, cancellationToken);

        /// <summary>
        /// Connects to a device by identifier, returning <c>null</c> rather than throwing when the platform has
        /// no record of it.
        /// </summary>
        /// <remarks>
        /// This replaces the <c>dontThrowExceptionOnNotFound</c> parameter of the 3.x platform-specific
        /// overloads. A boolean that changes whether a method throws is a second method wearing a disguise, and
        /// it read at the call site as the opposite of what it did.
        /// </remarks>
        public Task<Device> TryConnectToKnownDeviceAsync(Guid deviceGuid, ConnectParameters connectParameters = default, CancellationToken cancellationToken = default)
            => ConnectToKnownDeviceCoreAsync(deviceGuid, connectParameters, throwIfNotFound: false, cancellationToken);

        private async Task<Device> ConnectToKnownDeviceCoreAsync(Guid deviceGuid, ConnectParameters connectParameters, bool throwIfNotFound, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (DiscoveredDevicesRegistry.TryGetValue(deviceGuid, out var known) && known is not null)
            {
                await ConnectToDeviceAsync(known, connectParameters, cancellationToken).ConfigureAwait(false);
                return known;
            }

            var device = await ConnectToKnownDeviceNativeAsync(deviceGuid, connectParameters, cancellationToken).ConfigureAwait(false);
            if (device is null)
            {
                if (throwIfNotFound)
                {
                    throw new DeviceNotFoundException(deviceGuid);
                }

                return null;
            }

            DiscoveredDevicesRegistry[device.Id] = device;

            await ConnectToDeviceAsync(device, connectParameters, cancellationToken).ConfigureAwait(false);
            return device;
        }

        /// <summary>
        /// Disconnects a connected device. Completes without doing anything when the device is not connected.
        /// </summary>
        /// <param name="device">The device to disconnect.</param>
        /// <param name="cancellationToken">Abandons the wait for the disconnection to be reported.</param>
        public Task DisconnectDeviceAsync(Device device, CancellationToken cancellationToken = default)
        {
            if (device is null)
            {
                throw new ArgumentNullException(nameof(device));
            }

            // A keyed lookup rather than the old ConnectedDevices.Contains, which materialised the whole
            // registry into a list and then walked it on every disconnect.
            if (!_connectedDeviceRegistry.ContainsKey(device.Id))
            {
                Trace.Message("Disconnect async: device {0} is not in the list of connected devices.", device.NameOrId);
                return Task.CompletedTask;
            }

            return TaskBuilder.FromEvent<bool, EventHandler<DeviceEventArgs>, EventHandler<DeviceErrorEventArgs>>(
               execute: () => { DisconnectDeviceNative(device); return Task.CompletedTask; },

               getCompleteHandler: (complete, reject) => ((sender, args) =>
               {
                   if (args.Device.Id == device.Id)
                   {
                       Trace.Message("DisconnectAsync Disconnected: {0} {1}", args.Device.Id, args.Device.Name);
                       complete(true);
                   }
               }),
               subscribeComplete: handler => DeviceDisconnected += handler,
               unsubscribeComplete: handler => DeviceDisconnected -= handler,

               getRejectHandler: reject => ((sender, args) =>
               {
                   if (args.Device.Id == device.Id)
                   {
                       Trace.Message("DisconnectAsync Error: {0} {1}", args.Device.Id, args.Device.Name);
                       reject(new DeviceConnectionException(args.Device.Id, args.Device.Name, args.ErrorMessage ?? "The disconnect operation failed."));
                   }
               }),
               subscribeReject: handler => DeviceConnectionError += handler,
               unsubscribeReject: handler => DeviceConnectionError -= handler,
               token: cancellationToken);
        }

        #endregion

        #region Registries

        /// <summary>
        /// Looks up a connected device by identifier.
        /// </summary>
        /// <remarks>
        /// This and <see cref="ConnectedDevices"/> replace the public <c>ConnectedDeviceRegistry</c> dictionary
        /// removed in 4.0, which was both mutable by consumers and keyed differently on each platform.
        /// </remarks>
        public bool TryGetConnectedDevice(Guid id, out Device device) => _connectedDeviceRegistry.TryGetValue(id, out device);

        /// <summary>
        /// Empties the discovered- and connected-device registries.
        /// </summary>
        /// <remarks>
        /// Borrowed from upstream Plugin.BLE. It matters because the registries hold strong references to
        /// <see cref="Device"/> instances, which in turn hold native peripheral handles; an application that
        /// scans repeatedly over a long session otherwise accumulates them for its lifetime. This does not
        /// disconnect anything - disconnect first, or the adapter will forget devices it still has links to.
        /// </remarks>
        public void ClearDeviceRegistries()
        {
            DiscoveredDevicesRegistry.Clear();
            _connectedDeviceRegistry.Clear();
        }

        internal void RegisterConnectedDevice(Device device)
        {
            if (device is null)
            {
                return;
            }

            _connectedDeviceRegistry[device.Id] = device;
        }

        internal bool TryRemoveConnectedDevice(Guid id, out Device device) => _connectedDeviceRegistry.TryRemove(id, out device);

        /// <summary>
        /// Drops every reference this adapter holds to a device. Called from <see cref="Device.Dispose()"/>, so
        /// that a disposed device does not stay reachable through <see cref="DiscoveredDevices"/>.
        /// </summary>
        internal void RemoveDeviceFromRegistries(Device device)
        {
            if (device is null)
            {
                return;
            }

            _connectedDeviceRegistry.TryRemove(device.Id, out _);
            DiscoveredDevicesRegistry.TryRemove(device.Id, out _);
        }

        #endregion

        #region Platform callbacks

        internal void HandleDiscoveredDevice(Device device)
        {
            // Advertisements already queued in the platform scanner keep arriving for a short while after a
            // scan is stopped. Reporting them meant a device could be discovered after the task representing
            // the scan had completed, and after the filter that should have judged it had been cleared.
            if (!IsScanning)
            {
                return;
            }

            var filter = _currentScanDeviceFilter;
            if (filter != null && !filter(device))
            {
                return;
            }

            Raise(DeviceAdvertised, new DeviceEventArgs(device), nameof(DeviceAdvertised));

            if (DiscoveredDevicesRegistry.ContainsKey(device.Id))
            {
                return;
            }

            DiscoveredDevicesRegistry[device.Id] = device;
            Raise(DeviceDiscovered, new DeviceEventArgs(device), nameof(DeviceDiscovered));
        }

        internal void HandleConnectedDevice(Device device)
        {
            device.HasBeenConnected = true;
            device.RaiseStateChanged();

            Raise(DeviceConnected, new DeviceEventArgs(device), nameof(DeviceConnected));
        }

        internal void HandleDisconnectedDevice(bool disconnectRequested, Device device)
        {
            device.RaiseStateChanged();

            if (disconnectRequested)
            {
                Trace.Message("DisconnectedPeripheral by user: {0}", device.NameOrId);
                Raise(DeviceDisconnected, new DeviceEventArgs(device), nameof(DeviceDisconnected));
            }
            else
            {
                Trace.Message("DisconnectedPeripheral by lost signal: {0}", device.NameOrId);
                Raise(DeviceConnectionLost, new DeviceErrorEventArgs(device, "The connection was lost."), nameof(DeviceConnectionLost));

                if (DiscoveredDevicesRegistry.TryRemove(device.Id, out _))
                {
                    Trace.Message("Removed device from discovered devices list: {0}", device.NameOrId);
                }
            }
        }

        internal void HandleConnectionFail(Device device, string errorMessage)
        {
            Trace.Message("Failed to connect peripheral {0}: {1}", device.Id, device.NameOrId);
            Raise(DeviceConnectionError, new DeviceErrorEventArgs(device, errorMessage), nameof(DeviceConnectionError));
        }

        /// <summary>
        /// Reports that the platform refused or abandoned the scan. Raises <see cref="ScanFailed"/> and then
        /// cancels the running scan, which faults the awaiting <see cref="StartScanningForDevicesAsync"/> with
        /// an <see cref="AdapterScanException"/>.
        /// </summary>
        internal void HandleScanFailed(ScanFailureReason reason)
        {
            Trace.Message("Adapter: The scan failed: {0}", reason);

            // Recorded before the cancellation, because the cancellation is what wakes the awaiting scan and it
            // needs the reason to be visible by the time it looks.
            _scanFailureReason = reason;

            Raise(ScanFailed, new ScanFailedEventArgs(reason), nameof(ScanFailed));

            var cts = Volatile.Read(ref _scanCancellationTokenSource);
            try
            {
                cts?.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // The scan has already ended; the failure is only worth tracing at this point.
            }
        }

        #endregion

        #region Lifetime

        /// <summary>
        /// Stops any running scan and releases the platform resources this adapter holds. It does not
        /// disconnect connected devices - disconnect them first if that is what you want.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            var cts = Volatile.Read(ref _scanCancellationTokenSource);
            try
            {
                cts?.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // The scan ended and disposed its own source between the snapshot above and this call. There
                // is nothing left to cancel, and disposal must not fail because of a race it has already won.
            }

            DisposeNative();

            ClearDeviceRegistries();

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Implemented by each platform partial to unsubscribe its native callbacks and release its handles.
        /// </summary>
        partial void DisposeNative();

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(Adapter));
            }
        }

        #endregion

        #region Private Methods

        // The handler is snapshotted into a parameter, so an unsubscribe racing the invocation cannot turn it
        // into a NullReferenceException. Consumer handlers run on native callback threads: an exception
        // escaping one unwinds into an ObjC or JNI frame, which terminates the process rather than surfacing
        // anywhere a consumer can see it.
        private void Raise<TArgs>(EventHandler<TArgs> handler, TArgs args, string eventName)
        {
            if (handler is null)
            {
                return;
            }

            try
            {
                handler(this, args);
            }
            catch (Exception ex)
            {
                Trace.Message("Adapter: a {0} handler threw and the exception was swallowed: {1}", eventName, ex);
            }
        }

        private void Raise(EventHandler handler, string eventName)
        {
            if (handler is null)
            {
                return;
            }

            try
            {
                handler(this, System.EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Trace.Message("Adapter: a {0} handler threw and the exception was swallowed: {1}", eventName, ex);
            }
        }

        #endregion
    }
}
