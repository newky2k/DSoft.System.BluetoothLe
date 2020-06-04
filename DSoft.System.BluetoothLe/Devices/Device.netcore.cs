using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Windows.Devices.Bluetooth;
using System.BluetoothLe.Contracts;
using System.BluetoothLe.Extensions;


namespace System.BluetoothLe
{
    public partial class Device
    {
        #region Properties
        internal BluetoothLEDevice NativeDevice { get; private set; }

        private bool IsConnected
        {
            get
            {
                return NativeDevice.ConnectionStatus == BluetoothConnectionStatus.Connected;
            }
        }

        private bool IsPaired
        {
            get
            {
                return NativeDevice.DeviceInformation.Pairing.IsPaired;
            }
        }

        #endregion

        #region Constructors
        internal Device(Adapter adapter, BluetoothLEDevice nativeDevice, int rssi, Guid id, IReadOnlyList<AdvertisementRecord> advertisementRecords = null) : this(adapter)
        {
            NativeDevice = nativeDevice;

            Rssi = rssi;
            Id = id;
            Name = nativeDevice.Name;
            AdvertisementRecords = advertisementRecords;
        }
        #endregion

        #region Methods

        internal void Update(short btAdvRawSignalStrengthInDBm, IReadOnlyList<AdvertisementRecord> advertisementData)
        {
            this.Rssi = btAdvRawSignalStrengthInDBm;
            this.AdvertisementRecords = advertisementData;
        }

        public Task<bool> UpdateRssiAsync()
        {
            //No current method to update the Rssi of a device
            //In future implementations, maybe listen for device's advertisements

            Trace.Message("Request RSSI not supported in UWP");

            return Task.FromResult(true);
        }

        protected async Task<IReadOnlyList<IService>> GetServicesNativeAsync()
        {
            var result = await NativeDevice.GetGattServicesAsync(BleImplementation.CacheModeGetServices);
            result.ThrowIfError();

            return result.Services?
                .Select(nativeService => new Service(nativeService, this))
                .Cast<IService>()
                .ToList();
        }

        protected async Task<IService> GetServiceNativeAsync(Guid id)
        {
            var result = await NativeDevice.GetGattServicesForUuidAsync(id, BleImplementation.CacheModeGetServices);
            result.ThrowIfError();

            var nativeService = result.Services?.FirstOrDefault();
            return nativeService != null ? new Service(nativeService, this) : null;
        }

        protected DeviceState GetState()
        {
            if (IsConnected)
            {
                return DeviceState.Connected;
            }

            return IsPaired ? DeviceState.Limited : DeviceState.Disconnected;
        }

        protected Task<int> RequestMtuNativeAsync(int requestValue)
        {
            Trace.Message("Request MTU not supported in UWP");
            return Task.FromResult(-1);
        }

        protected bool UpdateConnectionIntervalNative(ConnectionInterval interval)
        {
            Trace.Message("Update Connection Interval not supported in UWP");
            return false;
        }

        #endregion
    }
}
