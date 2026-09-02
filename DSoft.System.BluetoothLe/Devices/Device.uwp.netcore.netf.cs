using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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

            // Never null: MergeOrUpdateAdvertising enumerates this on the very next advertisement.
            AdvertisementRecords = advertisementRecords ?? Array.Empty<AdvertisementRecord>();

            NativeDevice.OnNameChanged += OnNativeNameChanged;
        }

        #endregion

        #region Methods

        partial void DisposeNative()
        {
            var native = NativeDevice;
            NativeDevice = null;

            if (native == null)
            {
                return;
            }

            // A closure would have made this impossible to detach, leaving the device alive for as long as
            // the native wrapper raises name changes.
            native.OnNameChanged -= OnNativeNameChanged;
        }

        private void OnNativeNameChanged(object sender, string name)
        {
            Name = name;
        }

        internal void Update(short btAdvRawSignalStrengthInDBm, IReadOnlyList<AdvertisementRecord> advertisementData)
        {
            this.Rssi = btAdvRawSignalStrengthInDBm;

            MergeOrUpdateAdvertising(advertisementData);
        }

        private Task<bool> UpdateRssiNativeAsync(CancellationToken cancellationToken)
        {
            //No current method to update the Rssi of a device
            //In future implementations, maybe listen for device's advertisements

            Trace.Message("Request RSSI not supported in UWP");

            return Task.FromResult(true);
        }

        private async Task<IReadOnlyList<Service>> GetServicesNativeAsync(CancellationToken cancellationToken)
        {
            var result = await NativeDevice.BluetoothLEDevice
                .GetGattServicesAsync(BluetoothLE.CacheModeGetServices)
                .AsTask(cancellationToken);

            result.ThrowIfError();

            if (result.Services == null)
            {
                return Array.Empty<Service>();
            }

            return result.Services
                .Select(nativeService => new Service(nativeService, this))
                .ToList<Service>();
        }

        private DeviceState GetState()
        {
            if (NativeDevice.IsConnected)
            {
                return DeviceState.Connected;
            }

            return NativeDevice.IsPaired ? DeviceState.Limited : DeviceState.Disconnected;
        }

        private Task<int> RequestMtuNativeAsync(int requestValue, CancellationToken cancellationToken)
        {
            Trace.Message("Request MTU not supported in UWP");
            return Task.FromResult(-1);
        }

        private bool UpdateConnectionIntervalNative(ConnectionInterval interval)
        {
            Trace.Message("Update Connection Interval not supported in UWP");
            return false;
        }

        internal void MergeOrUpdateAdvertising(IReadOnlyList<AdvertisementRecord> advertisementRecords)
        {
            var adverts = this.AdvertisementRecords.ToList();

            foreach (var adv in advertisementRecords)
            {
                var matching = adverts.FirstOrDefault(x => x.Type.Equals(adv.Type));

                if (matching != null)
                    adverts.Remove(matching);

                adverts.Add(adv);
            }

            this.AdvertisementRecords = adverts;
        }

        #endregion
    }
}
