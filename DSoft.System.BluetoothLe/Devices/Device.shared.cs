using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.BluetoothLe;

namespace System.BluetoothLe
{
    public partial class Device : IDevice, ICancellationMaster
    {
        #region Fields
        protected readonly Adapter Adapter;
        protected readonly Dictionary<Guid, Service> KnownServices = new Dictionary<Guid, Service>();
        #endregion

        #region Properties
        public Guid Id { get; protected set; }
        public string Name { get; protected set; }
        public int Rssi { get; protected set; }
        public DeviceState State => GetState();
        public IReadOnlyList<AdvertisementRecord> AdvertisementRecords { get; protected set; }
        CancellationTokenSource ICancellationMaster.TokenSource { get; set; } = new CancellationTokenSource();

        object IDevice.NativeDevice => NativeDevice;

        #endregion

        #region Constructors

        private Device(Adapter adapter)
        {
            Adapter = adapter;
            
        }
        #endregion

        #region Methods

        public async Task<IReadOnlyList<Service>> GetServicesAsync(CancellationToken cancellationToken = default)
        {
            using (var source = this.GetCombinedSource(cancellationToken))
            {
                foreach (var service in await GetServicesNativeAsync())
                {
                    KnownServices[service.Id] = service;
                }
            }

            return KnownServices.Values.ToList();
        }

        public async Task<Service> GetServiceAsync(Guid id, CancellationToken cancellationToken = default)
        {
            if (KnownServices.ContainsKey(id))
            {
                return KnownServices[id];
            }

            var service = await GetServiceNativeAsync(id);
            if (service == null)
            {
                return null;
            }

            return KnownServices[id] = service;
        }

        public async Task<int> RequestMtuAsync(int requestValue)
        {
            return await RequestMtuNativeAsync(requestValue);
        }

        public bool UpdateConnectionInterval(ConnectionInterval interval)
        {
            return UpdateConnectionIntervalNative(interval);
        }

        public override string ToString()
        {
            return Name;
        }

        public virtual void Dispose()
        {
            Adapter.DisconnectDeviceAsync(this);
        }

        public void DisposeServices()
        {
            this.CancelEverythingAndReInitialize();

            foreach (var service in KnownServices.Values)
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

            var otherDeviceBase = (IDevice)other;
            return Id == otherDeviceBase.Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        #endregion
    }
}
