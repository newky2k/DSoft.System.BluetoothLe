using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.BluetoothLe;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace System.BluetoothLe
{
    /// <summary>
    /// A remote Bluetooth Low Energy peripheral.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Threading: <see cref="PropertyChanged"/> is raised on whichever thread the platform delivered the
    /// underlying native callback on - a CoreBluetooth queue on Apple, a binder thread on Android - and it
    /// is deliberately <b>not</b> marshalled to the UI thread. Marshalling here would reorder notifications
    /// relative to the GATT operations that caused them, which on a medical-adjacent stack is a worse
    /// failure than making the consumer marshal. Marshal in your view model.
    /// </para>
    /// <para>
    /// Lifetime: a device is owned by the <see cref="Adapter"/> that discovered it. Disposing it releases
    /// the native handles and cached services; it does <b>not</b> disconnect. Disconnect explicitly, and
    /// await that, before disposing.
    /// </para>
    /// </remarks>
    public partial class Device : IDisposable, ICancellationMaster, INotifyPropertyChanged
    {
        #region Fields
        private readonly Adapter Adapter;
        private readonly List<Service> KnownServices = new List<Service>();
        private string _name;
        private int _rssi;
        private Guid _id;
        private IReadOnlyList<AdvertisementRecord> _advertisementRecords;
        private bool _isDisposed;

        /// <summary>
        /// Raised when a property of this device changes. See the threading remarks on <see cref="Device"/>:
        /// this is raised on a native callback thread and is not marshalled.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region Properties

        /// <summary>
        /// Gets the Id of the device.
        /// </summary>
        /// <remarks>
        /// On Apple this is the platform's peripheral identifier; on Android it is derived from the MAC
        /// address. It is stable for as long as the platform considers it stable, and is the key both
        /// device registries are keyed by, so it is set only by the library.
        /// </remarks>
        public Guid Id
        {
            get { return _id; }
            private set { _id = value; NotifyPropertyChanged(nameof(Id)); NotifyPropertyChanged(nameof(NameOrId)); }
        }

        /// <summary>
        /// Gets the name of the device, as advertised or as reported by the platform.
        /// </summary>
        public string Name
        {
            get { return _name; }
            protected set { _name = value; NotifyPropertyChanged(nameof(Name)); NotifyPropertyChanged(nameof(NameOrId)); }
        }

        /// <summary>
        /// Gets the Rssi (Received Signal Strength Indicator) value for the device.
        /// </summary>
        public int Rssi
        {
            get { return _rssi; }
            protected set { _rssi = value; NotifyPropertyChanged(nameof(Rssi)); }
        }

        /// <summary>
        /// Gets the current connection state, as the platform reports it.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The device has been disposed.</exception>
        public DeviceState State
        {
            get
            {
                ThrowIfDisposed();
                return GetState();
            }
        }

        /// <summary>
        /// Gets whether this instance has ever completed a connection.
        /// </summary>
        /// <remarks>
        /// Set by the adapter when a connection succeeds. It distinguishes a device that was discovered
        /// and never used from one whose native handles have been through a connect/disconnect cycle,
        /// which matters when deciding whether a reconnect can reuse cached state.
        /// </remarks>
        public bool HasBeenConnected { get; internal set; }

        /// <summary>
        /// Gets the advertisement records last seen for this device.
        /// </summary>
        public IReadOnlyList<AdvertisementRecord> AdvertisementRecords
        {
            get { return _advertisementRecords; }
            protected set { _advertisementRecords = value; NotifyPropertyChanged(nameof(AdvertisementRecords)); }
        }

        CancellationTokenSource ICancellationMaster.TokenSource { get; set; } = new CancellationTokenSource();

        /// <summary>
        /// Gets the name if set, or the Id if not.
        /// </summary>
        public string NameOrId => (string.IsNullOrWhiteSpace(Name)) ? Id.ToString() : Name;

        #endregion

        #region Constructors

        private Device(Adapter adapter)
        {
            Adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        }
        #endregion

        #region Methods

        /// <summary>
        /// Discovers, or returns the already discovered, GATT services of the device.
        /// </summary>
        /// <param name="cancellationToken">Cancels the discovery. The device's own cancellation source is
        /// combined with it, so <see cref="ClearServices"/> and disposal also tear down a discovery in flight.</param>
        public async Task<IReadOnlyList<Service>> GetServicesAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            lock (KnownServices)
            {
                if (KnownServices.Any())
                {
                    return KnownServices.ToArray();
                }
            }

            using (var source = this.GetCombinedSource(cancellationToken))
            {
                var services = await GetServicesNativeAsync(source.Token);

                lock (KnownServices)
                {
                    if (services != null)
                    {
                        KnownServices.AddRange(services);
                    }

                    return KnownServices.ToArray();
                }
            }
        }

        /// <summary>
        /// Returns the service with the given Id, or null when the device does not expose it.
        /// </summary>
        public async Task<Service> GetServiceAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var services = await GetServicesAsync(cancellationToken);

            return services.FirstOrDefault(x => x.Id == id);
        }

        /// <summary>
        /// Requests a larger ATT MTU. Returns the MTU the peripheral agreed to, or -1 where the platform
        /// does not support the request.
        /// </summary>
        public async Task<int> RequestMtuAsync(int requestValue, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            using (var source = this.GetCombinedSource(cancellationToken))
            {
                return await RequestMtuNativeAsync(requestValue, source.Token);
            }
        }

        /// <summary>
        /// Asks the platform for a different connection interval. Returns false where unsupported.
        /// </summary>
        public bool UpdateConnectionInterval(ConnectionInterval interval)
        {
            ThrowIfDisposed();
            return UpdateConnectionIntervalNative(interval);
        }

        /// <summary>
        /// Reads the current RSSI from a connected device and updates <see cref="Rssi"/>.
        /// </summary>
        public async Task<bool> UpdateRssiAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            using (var source = this.GetCombinedSource(cancellationToken))
            {
                return await UpdateRssiNativeAsync(source.Token);
            }
        }

        public override string ToString()
        {
            return NameOrId;
        }

        /// <summary>
        /// Cancels everything in flight against this device and drops the cached services.
        /// </summary>
        /// <remarks>
        /// Called on every disconnect, because a service or characteristic handle from a previous
        /// connection makes the next GATT operation on the same instance fail silently.
        /// </remarks>
        public void ClearServices()
        {
            this.CancelEverythingAndReInitialize();

            lock (KnownServices)
            {
                foreach (var service in KnownServices)
                {
                    try
                    {
                        service.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Trace.Message("Exception while cleanup of service: {0}", ex.Message);
                    }
                }

                KnownServices.Clear();
            }
        }

        public override bool Equals(object other)
        {
            if (other == null)
            {
                return false;
            }

            if (other.GetType() != GetType())
            {
                return false;
            }

            var otherDeviceBase = (Device)other;
            return Id == otherDeviceBase.Id;
        }

        public override int GetHashCode() => Id.GetHashCode();

        /// <summary>
        /// Releases the native handles and the cached services. Does not disconnect - see the lifetime
        /// remarks on <see cref="Device"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            if (!disposing)
            {
                return;
            }

            ClearServices();

            // Not the ReInitialize variant: nothing may be started against this device again.
            this.CancelEverything();

            // The adapter holds this instance in both of its registries, and those references outlive the
            // device unless it drops them here: a disposed device would otherwise stay reachable - and
            // returnable - through Adapter.DiscoveredDevices and Adapter.ConnectedDevices.
            Adapter.RemoveDeviceFromRegistries(this);

            DisposeNative();
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Raises <see cref="PropertyChanged"/> for <see cref="State"/>. The platform tells the adapter
        /// about connection changes, not the device, so the adapter has to drive the notification.
        /// </summary>
        internal void RaiseStateChanged() => NotifyPropertyChanged(nameof(State));

        #endregion

        #region Private Methods

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(Device), $"The device {NameOrId} has been disposed.");
            }
        }

        /// <summary>
        /// Implemented by each platform partial to release that platform's native handles.
        /// </summary>
        partial void DisposeNative();

        #endregion
    }
}
