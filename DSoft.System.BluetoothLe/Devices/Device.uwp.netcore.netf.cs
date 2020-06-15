using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Windows.Devices.Bluetooth;
using System.BluetoothLe;
using System.BluetoothLe.Extensions;


namespace System.BluetoothLe
{
    public partial class Device
    {
        #region Properties
        internal ObservableBluetoothLEDevice NativeDevice { get; private set; }

        #endregion

        #region Constructors
        internal Device(Adapter adapter, BluetoothLEDevice nativeDevice, int rssi, Guid id, IReadOnlyList<AdvertisementRecord> advertisementRecords = null) : this(adapter)
        {
            NativeDevice = new ObservableBluetoothLEDevice(nativeDevice.DeviceInformation);

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

        protected async Task<IReadOnlyList<Service>> GetServicesNativeAsync()
        {
            var result = await NativeDevice.BluetoothLEDevice.GetGattServicesAsync(BluetoothLE.CacheModeGetServices);
            result.ThrowIfError();

            return result.Services?
                .Select(nativeService => new Service(nativeService, this))
                .Cast<Service>()
                .ToList();
        }

        protected async Task<Service> GetServiceNativeAsync(Guid id)
        {
            var result = await NativeDevice.BluetoothLEDevice.GetGattServicesForUuidAsync(id, BluetoothLE.CacheModeGetServices);
            result.ThrowIfError();

            var nativeService = result.Services?.FirstOrDefault();
            return nativeService != null ? new Service(nativeService, this) : null;
        }

        protected DeviceState GetState()
        {
            if (NativeDevice.IsConnected)
            {
                return DeviceState.Connected;
            }

            return NativeDevice.IsPaired ? DeviceState.Limited : DeviceState.Disconnected;
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
