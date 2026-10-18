using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.OS;
using Android.Bluetooth;
using Android.Content;
using System.BluetoothLe.Utils;
using Trace = System.BluetoothLe.Trace;
using System.Threading;
using System.BluetoothLe.EventArgs;

namespace System.BluetoothLe
{
    public partial class Device
    {
        #region Fields
        /// <summary>
        /// we have to keep a reference to this because Android's api is weird and requires
        /// the GattServer in order to do nearly anything, including enumerating services
        /// </summary>
        internal BluetoothGatt _gatt;

        /// <summary>
        /// we also track this because of google's weird API. the gatt callback is where
        /// we'll get notified when services are enumerated.
        /// Created on the first connect rather than in the constructor: a scan produces a Device per
        /// advertisement, and allocating a BluetoothGattCallback - a Java peer - for every advertisement
        /// of every device in range is pure churn for objects that will never see a GATT operation.
        /// </summary>
        private GattCallback _gattCallback;

        /// <summary>
        /// the registration must be disposed to avoid disconnecting after a connection
        /// </summary>
        private CancellationTokenRegistration _connectCancellationTokenRegistration;

        #endregion

        #region Properties

        internal BluetoothDevice NativeDevice { get; private set; }

        internal bool IsOperationRequested { get; set; }

        #endregion

        #region Constructors
        internal Device(Adapter adapter, BluetoothDevice nativeDevice, BluetoothGatt gatt, int rssi, byte[] advertisementData = null) : this(adapter)
        {
            NativeDevice = nativeDevice;

            Update(nativeDevice, gatt);
            Rssi = rssi;
            AdvertisementRecords = ParseScanRecord(advertisementData);
        }

        #endregion

        #region Methods

        #region Private
        private Guid ParseDeviceId() => ParseDeviceId(NativeDevice.Address);

        /// <summary>
        /// The id a device with this MAC address is given: the address in the last six bytes of an otherwise
        /// zero GUID. Shared with the adapter, which has only an address when the system reports a bond change.
        /// </summary>
        internal static Guid ParseDeviceId(string address)
        {
            var deviceGuid = new byte[16];
            var macWithoutColons = address.Replace(":", "");
            var macBytes = Enumerable.Range(0, macWithoutColons.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(macWithoutColons.Substring(x, 2), 16))
                .ToArray();
            macBytes.CopyTo(deviceGuid, 10);
            return new Guid(deviceGuid);
        }

        private void ConnectToGattForceBleTransportAPI(bool autoconnect, CancellationToken cancellationToken)
        {
            // The transport parameter exists from API 18 but only became public API at 23, so below 23 it
            // has to be reached by reflection. The branch is still live: the package supports API 21.
            if (!OperatingSystem.IsAndroidVersionAtLeast(23))
            {
                var m = NativeDevice.Class.GetDeclaredMethod("connectGatt", new Java.Lang.Class[] {
                                Java.Lang.Class.FromType(typeof(Context)),
                                Java.Lang.Boolean.Type,
                                Java.Lang.Class.FromType(typeof(BluetoothGattCallback)),
                                Java.Lang.Integer.Type});

                var transport = NativeDevice.Class.GetDeclaredField("TRANSPORT_LE").GetInt(null); // LE = 2, BREDR = 1, AUTO = 0
                m.Invoke(NativeDevice, Application.Context, false, _gattCallback, transport);
            }
            else
            {
                var connectGatt = NativeDevice.ConnectGatt(Application.Context, autoconnect, _gattCallback, BluetoothTransports.Le);
                _connectCancellationTokenRegistration.Dispose();
                _connectCancellationTokenRegistration = cancellationToken.Register(() => DisconnectAndClose(connectGatt));
            }

        }

        private async Task<IReadOnlyList<Service>> DiscoverServicesInternal(CancellationToken cancellationToken)
        {
            return await TaskBuilder
                .FromEvent<IReadOnlyList<Service>, EventHandler<ServicesDiscoveredCallbackEventArgs>, EventHandler>(
                    execute: () =>
                    {
                        if (!_gatt.DiscoverServices())
                        {
                            throw new Exception("Could not start service discovery");
                        }

                        return Task.CompletedTask;
                    },
                    getCompleteHandler: (complete, reject) => ((sender, args) =>
                    {
                        complete(_gatt.Services.Select(service => new Service(service, _gatt, _gattCallback, this)).ToList());
                    }),
                    subscribeComplete: handler => _gattCallback.ServicesDiscovered += handler,
                    unsubscribeComplete: handler => _gattCallback.ServicesDiscovered -= handler,
                    getRejectHandler: reject => ((sender, args) =>
                    {
                        reject(new Exception($"Device {Name} disconnected while fetching services."));
                    }),
                    subscribeReject: handler => _gattCallback.ConnectionInterrupted += handler,
                    unsubscribeReject: handler => _gattCallback.ConnectionInterrupted -= handler,
                    token: cancellationToken);
        }

        private static List<AdvertisementRecord> ParseScanRecord(byte[] scanRecord)
        {
            var records = new List<AdvertisementRecord>();

            if (scanRecord == null)
                return records;

            // A malformed or truncated advertisement is a remote device's fault, not ours, and this runs on
            // a scan callback thread where an escaping exception takes the process down. Keep whatever
            // parsed cleanly and stop at the first thing that does not.
            try
            {
                int index = 0;
                while (index < scanRecord.Length)
                {
                    byte length = scanRecord[index++];
                    //Done once we run out of records
                    // 1 byte for type and length-1 bytes for data
                    if (length == 0) break;

                    int type = scanRecord[index];
                    //Done if our record isn't a valid type
                    if (type == 0) break;

                    if (!Enum.IsDefined(typeof(AdvertisementRecordType), type))
                    {
                        Trace.Message("Advertisement record type not defined: {0}", type);
                        break;
                    }

                    // A device may advertise a length that runs off the end of the buffer.
                    if (index + length > scanRecord.Length) break;

                    //data length is length -1 because type takes the first byte
                    byte[] data = new byte[length - 1];
                    Array.Copy(scanRecord, index + 1, data, 0, length - 1);

                    // don't forget that data is little endian so reverse
                    // Supplement to Bluetooth Core Specification 1
                    // NOTE: all relevant devices are already little endian, so this is not necessary for any type except UUIDs
                    // ServiceDataUuid32Bit is deliberately NOT in this list: it is a UUID followed by a
                    // service-data payload, so reversing the record corrupts both halves.
                    switch ((AdvertisementRecordType)type)
                    {
                        case AdvertisementRecordType.SsUuids16Bit:
                        case AdvertisementRecordType.SsUuids32Bit:
                        case AdvertisementRecordType.SsUuids128Bit:
                        case AdvertisementRecordType.UuidsComplete16Bit:
                        case AdvertisementRecordType.UuidsComplete32Bit:
                        case AdvertisementRecordType.UuidsComplete128Bit:
                        case AdvertisementRecordType.UuidsIncomplete16Bit:
                        case AdvertisementRecordType.UuidsIncomplete32Bit:
                        case AdvertisementRecordType.UuidsIncomplete128Bit:
                            Array.Reverse(data);
                            break;
                    }
                    var record = new AdvertisementRecord((AdvertisementRecordType)type, data);

                    Trace.Message(record.ToString());

                    records.Add(record);

                    //Advance
                    index += length;
                }
            }
            catch (Exception ex)
            {
                Trace.Message("Failed to parse advertisement record, keeping {0} record(s) parsed so far: {1}", records.Count, ex.Message);
            }

            return records;
        }
        #endregion

        #region Protected
        private DeviceState GetState()
        {
            var manager = (BluetoothManager)Application.Context.GetSystemService(Context.BluetoothService);
            var state = manager.GetConnectionState(NativeDevice, ProfileType.Gatt);

            switch (state)
            {
                case ProfileState.Connected:
                    // if the device does not have a gatt instance we can't use it in the app, so we need to explicitly be able to connect it
                    // even if the profile state is connected
                    return _gatt != null ? DeviceState.Connected : DeviceState.Limited;

                case ProfileState.Connecting:
                    return DeviceState.Connecting;

                case ProfileState.Disconnected:
                case ProfileState.Disconnecting:
                default:
                    return DeviceState.Disconnected;
            }
        }

        private DeviceBondState GetBondState()
        {
            // No native device: a stand-in built through the protected constructor.
            if (NativeDevice == null)
            {
                return DeviceBondState.Unknown;
            }

            // Throws SecurityException on Android 12 and later without BLUETOOTH_CONNECT; BondState reports
            // that as Unknown.
            return ToBondState(NativeDevice.BondState);
        }

        private Task<bool> CreateBondNativeAsync(CancellationToken cancellationToken)
        {
            if (NativeDevice == null || Adapter == null)
            {
                return Task.FromResult(false);
            }

            var current = ToBondState(NativeDevice.BondState);

            if (current == DeviceBondState.Bonded)
            {
                return Task.FromResult(true);
            }

            // Already bonding - started by the system on access to a protected characteristic, or by an earlier
            // call: wait for that rather than asking again, which Android refuses while one is in progress.
            var alreadyBonding = current == DeviceBondState.Bonding;

            return AwaitBondOutcomeAsync(
                start: () => alreadyBonding || NativeDevice.CreateBond(),
                success: DeviceBondState.Bonded,
                failure: DeviceBondState.NotBonded,
                cancellationToken);
        }

        private Task<bool> RemoveBondNativeAsync(CancellationToken cancellationToken)
        {
            if (NativeDevice == null || Adapter == null)
            {
                return Task.FromResult(false);
            }

            if (ToBondState(NativeDevice.BondState) == DeviceBondState.NotBonded)
            {
                return Task.FromResult(true);
            }

            return AwaitBondOutcomeAsync(
                start: InvokeRemoveBond,
                success: DeviceBondState.NotBonded,
                failure: null,
                cancellationToken);
        }

        /// <summary>
        /// Calls the hidden <c>BluetoothDevice.removeBond()</c>. Any refusal - the method missing, blocked as a
        /// non-SDK interface, or denied for lack of a privileged permission - is reported as false.
        /// </summary>
        private bool InvokeRemoveBond()
        {
            try
            {
                var method = NativeDevice.Class.GetMethod("removeBond");
                var result = method.Invoke(NativeDevice);

                return result is Java.Lang.Boolean started && started.BooleanValue();
            }
            catch (Exception ex)
            {
                Trace.Message("Device: removeBond is not available for {0}: {1}", NameOrId, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Starts a bond operation and waits for the adapter to report this device reaching
        /// <paramref name="success"/> (true) or <paramref name="failure"/> (false).
        /// </summary>
        private async Task<bool> AwaitBondOutcomeAsync(Func<bool> start, DeviceBondState success, DeviceBondState? failure, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var outcome = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            void OnBondStateChanged(object sender, DeviceBondStateChangedEventArgs args)
            {
                if (args.Device.Id != Id)
                {
                    return;
                }

                if (args.State == success)
                {
                    outcome.TrySetResult(true);
                }
                else if (args.State == failure)
                {
                    outcome.TrySetResult(false);
                }
            }

            // Subscribed before starting, so a fast outcome cannot be missed.
            Adapter.DeviceBondStateChanged += OnBondStateChanged;

            try
            {
                if (!start())
                {
                    return false;
                }

                // The outcome may already have been reached between reading the state and subscribing. Only the
                // success state is checked here: straight after createBond the system can still report NotBonded
                // for a moment, and taking that as a failure would end the wait before bonding has begun.
                if (ToBondState(NativeDevice.BondState) == success)
                {
                    return true;
                }

                using (cancellationToken.Register(() => outcome.TrySetCanceled(cancellationToken)))
                {
                    return await outcome.Task;
                }
            }
            finally
            {
                Adapter.DeviceBondStateChanged -= OnBondStateChanged;
            }
        }

        internal static DeviceBondState ToBondState(Bond bond)
        {
            switch (bond)
            {
                case Bond.None:
                    return DeviceBondState.NotBonded;
                case Bond.Bonding:
                    return DeviceBondState.Bonding;
                case Bond.Bonded:
                    return DeviceBondState.Bonded;
                default:
                    return DeviceBondState.Unknown;
            }
        }

        private async Task<bool> UpdateRssiNativeAsync(CancellationToken cancellationToken)
        {
            if (_gatt == null || _gattCallback == null)
            {
                Trace.Message("You can't read the RSSI value for disconnected devices except on discovery on Android. Device is {0}", State);
                return false;
            }

            return await TaskBuilder.FromEvent<bool, EventHandler<RssiReadCallbackEventArgs>, EventHandler>(
              execute: () => { _gatt.ReadRemoteRssi(); return Task.CompletedTask; },
              getCompleteHandler: (complete, reject) => ((sender, args) =>
              {
                  if (args.Error == null)
                  {
                      Trace.Message("Read RSSI for {0} {1}: {2}", Id, Name, args.Rssi);
                      Rssi = args.Rssi;
                      complete(true);
                  }
                  else
                  {
                      Trace.Message($"Failed to read RSSI for device {Id}-{Name}. {args.Error.Message}");
                      complete(false);
                  }
              }),
              subscribeComplete: handler => _gattCallback.RemoteRssiRead += handler,
              unsubscribeComplete: handler => _gattCallback.RemoteRssiRead -= handler,
              getRejectHandler: reject => ((sender, args) =>
              {
                  reject(new Exception($"Device {Name} disconnected while updating rssi."));
              }),
              subscribeReject: handler => _gattCallback.ConnectionInterrupted += handler,
              unsubscribeReject: handler => _gattCallback.ConnectionInterrupted -= handler,
              token: cancellationToken);
        }

        private async Task<int> RequestMtuNativeAsync(int requestValue, CancellationToken cancellationToken)
        {
            if (_gatt == null || _gattCallback == null)
            {
                Trace.Message("You can't request a MTU for disconnected devices. Device is {0}", State);
                return -1;
            }

            if (Build.VERSION.SdkInt < BuildVersionCodes.Lollipop)
            {
                Trace.Message($"Request MTU not supported in this Android API level");
                return -1;
            }

            return await TaskBuilder.FromEvent<int, EventHandler<MtuRequestCallbackEventArgs>, EventHandler>(
              execute: () => { _gatt.RequestMtu(requestValue); return Task.CompletedTask; },
              getCompleteHandler: (complete, reject) => ((sender, args) =>
              {
                  if (args.Error != null)
                  {
                      Trace.Message($"Failed to request MTU ({requestValue}) for device {Id}-{Name}. {args.Error.Message}");
                      reject(new Exception($"Request MTU error: {args.Error.Message}"));
                  }
                  else
                  {
                      complete(args.Mtu);
                  }
              }),
              subscribeComplete: handler => _gattCallback.MtuRequested += handler,
              unsubscribeComplete: handler => _gattCallback.MtuRequested -= handler,
              getRejectHandler: reject => ((sender, args) =>
              {
                  reject(new Exception($"Device {Name} disconnected while requesting MTU."));
              }),
              subscribeReject: handler => _gattCallback.ConnectionInterrupted += handler,
              unsubscribeReject: handler => _gattCallback.ConnectionInterrupted -= handler,
              token: cancellationToken
            );
        }

        private bool UpdateConnectionIntervalNative(ConnectionInterval interval)
        {
            if (_gatt == null || _gattCallback == null)
            {
                Trace.Message("You can't update a connection interval for disconnected devices. Device is {0}", State);
                return false;
            }

            if (Build.VERSION.SdkInt < BuildVersionCodes.Lollipop)
            {
                Trace.Message($"Update connection interval parameter is not supported in this Android API level");
                return false;
            }

            try
            {
                // map to android gattConnectionPriorities
                // https://developer.android.com/reference/android/bluetooth/BluetoothGatt.html#CONNECTION_PRIORITY_BALANCED
                return _gatt.RequestConnectionPriority((GattConnectionPriority)(int)interval);
            }
            catch (Exception ex)
            {
                throw new Exception($"Update Connection Interval fails with error. {ex.Message}");
            }
        }

        private async Task<IReadOnlyList<Service>> GetServicesNativeAsync(CancellationToken cancellationToken)
        {
            if (_gattCallback == null || _gatt == null)
            {
                return new List<Service>();
            }

            // _gatt.Services is already populated if device service discovery was already done
            if (_gatt.Services.Any())
            {
                return _gatt.Services.Select(service => new Service(service, _gatt, _gattCallback, this)).ToList();
            }

            return await DiscoverServicesInternal(cancellationToken);
        }

        #endregion

        #region Methods

        partial void DisposeNative()
        {
            _connectCancellationTokenRegistration.Dispose();
            _connectCancellationTokenRegistration = default;

            _gatt?.Close();
            _gatt = null;

            _gattCallback?.Dispose();
            _gattCallback = null;
        }

        internal void Update(BluetoothDevice nativeDevice, BluetoothGatt gatt)
        {
            _connectCancellationTokenRegistration.Dispose();
            _connectCancellationTokenRegistration = new CancellationTokenRegistration();

            NativeDevice = nativeDevice;
            _gatt = gatt;


            Id = ParseDeviceId();
            Name = NativeDevice.Name;
        }

        internal void Connect(ConnectParameters connectParameters, CancellationToken cancellationToken)
        {
            IsOperationRequested = true;

            // First point at which a GATT callback is actually needed. Both branches below use it, and
            // nothing that runs before a connect touches it - every other read site requires _gatt too,
            // and _gatt cannot exist before this call.
            _gattCallback ??= new GattCallback(Adapter, this);

            if (connectParameters.ForceBleTransport)
            {
                ConnectToGattForceBleTransportAPI(connectParameters.AutoConnect, cancellationToken);
            }
            else
            {
                var connectGatt = NativeDevice.ConnectGatt(Application.Context, connectParameters.AutoConnect, _gattCallback);
                _connectCancellationTokenRegistration.Dispose();
                _connectCancellationTokenRegistration = cancellationToken.Register(() => DisconnectAndClose(connectGatt));
            }
        }

        /// <summary>
        /// This method is only called by a user triggered disconnect.
        /// A user will first trigger _gatt.disconnect -> which in turn will trigger _gatt.Close() via the gattCallback
        /// </summary>
        internal void Disconnect()
        {
            if (_gatt != null)
            {
                IsOperationRequested = true;

                ClearServices();

                _gatt.Disconnect();
            }
            else
            {
                Trace.Message("[Warning]: Can't disconnect {0}. Gatt is null.", Name);
            }
        }

        /// <summary>
        /// CloseGatt is called by the gattCallback in case of user disconnect or a disconnect by signal loss or a connection error. 
        /// Cleares all cached services.
        /// </summary>
        internal void CloseGatt()
        {
            _gatt?.Close();
            _gatt = null;

            // CloseGatt might get called on signal loss without Disconnect being called, so we have to make sure we clear the services.
            // Clear services & characteristics otherwise we will get gatt operations returning FALSE when connecting to the same Device instance at a later time.
            // The GattCallback is deliberately NOT disposed here. Operations that were in flight when the
            // link dropped are still inside a finally that unsubscribes from it, and nulling the field out
            // from under them would replace the real rejection with a NullReferenceException. It is a single
            // object per device, reused by the next connect, and released in DisposeNative.
            ClearServices();
        }

        private void DisconnectAndClose(BluetoothGatt gatt)
        {
            gatt.Disconnect();
            gatt.Close();
        }
        #endregion


        #endregion
    }
}