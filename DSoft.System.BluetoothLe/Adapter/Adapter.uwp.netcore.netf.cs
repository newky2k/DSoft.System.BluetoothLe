using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;

using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;

using System.BluetoothLe;
using System.BluetoothLe.Extensions;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace System.BluetoothLe
{
    public partial class Adapter
    {
        private BluetoothLEAdvertisementWatcher _bleWatcher;

        private Guid[] _serviceUuids;

        private bool HasFilter => _serviceUuids?.Any() ?? false;

        // Windows needs no native manager handed to it, but the shared parameterless constructor was removed
        // in 4.0 because on every other platform it produced an adapter that could not work. This one is
        // internal for the same reason: only BluetoothLE may create it.
        internal Adapter()
        {
        }

        protected Task StartScanningForDevicesNativeAsync(Guid[] serviceUuids, bool allowDuplicatesKey, CancellationToken scanCancellationToken)
        {
            // allowDuplicatesKey has no equivalent on Windows: the watcher reports every advertisement it
            // receives, and DeviceAdvertised is raised for each of them.

            _serviceUuids = serviceUuids;

            _bleWatcher = new BluetoothLEAdvertisementWatcher { ScanningMode = ScanMode.ToNative() };

            Trace.Message("Starting a scan for devices.");

            _bleWatcher.Received -= DeviceFoundAsync;
            _bleWatcher.Received += DeviceFoundAsync;

            _bleWatcher.Start();

            return Task.CompletedTask;
        }

        protected void StopScanNative()
        {
            var watcher = _bleWatcher;

            if (watcher != null)
            {
                Trace.Message("Stopping the scan for devices");

                // Unsubscribed before Stop, not after. The watcher delivers advertisements it has already
                // queued for a short while after Stop returns, and each of those kept a reference to this
                // adapter alive through the handler for as long as the watcher lived.
                watcher.Received -= DeviceFoundAsync;
                watcher.Stop();

                _bleWatcher = null;
            }

            _serviceUuids = null;
        }

        protected async Task ConnectToDeviceNativeAsync(Device device, ConnectParameters connectParameters, CancellationToken cancellationToken)
        {
            Trace.Message($"Connecting to device with ID:  {device.Id}");

            if (!(device.NativeDevice is ObservableBluetoothLEDevice nativeDevice))
                return;

            nativeDevice.PropertyChanged -= Device_ConnectionStatusChanged;
            nativeDevice.PropertyChanged += Device_ConnectionStatusChanged;

            RegisterConnectedDevice(device);

            await nativeDevice.ConnectAsync();
        }

        private void Device_ConnectionStatusChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            if (!(sender is ObservableBluetoothLEDevice nativeDevice) || nativeDevice.BluetoothLEDevice == null)
            {
                return;
            }

            if (propertyChangedEventArgs.PropertyName != nameof(nativeDevice.IsConnected))
            {
                return;
            }

            var address = ParseDeviceId(nativeDevice.BluetoothLEDevice.BluetoothAddress);

            if (nativeDevice.IsConnected && TryGetConnectedDevice(address, out var connectedDevice))
            {
                HandleConnectedDevice(connectedDevice);
                return;
            }

            // Only the unsolicited case reaches here: a disconnect the caller asked for is raised by
            // DisconnectDeviceNative, which unsubscribes this handler before it does so.
            if (!nativeDevice.IsConnected && TryRemoveConnectedDevice(address, out var disconnectedDevice))
            {
                HandleDisconnectedDevice(false, disconnectedDevice);
            }
        }

        protected void DisconnectDeviceNative(Device device)
        {
            // Windows has no explicit disconnect: the link drops when the last reference to the
            // BluetoothLEDevice is released.
            Trace.Message($"Disconnecting from device with ID:  {device.Id}");

            device.ClearServices();

            if (device.NativeDevice is ObservableBluetoothLEDevice nativeDevice)
            {
                // Unsubscribed first, so that disposing the native device below cannot re-enter
                // Device_ConnectionStatusChanged and report this as a connection that was lost.
                nativeDevice.PropertyChanged -= Device_ConnectionStatusChanged;
                nativeDevice.BluetoothLEDevice?.Dispose();
            }

            HandleDisconnectedDevice(true, device);
        }

        /// <summary>
        /// Produces a device for an identifier the scan has not seen, without connecting it.
        /// </summary>
        protected async Task<Device> ConnectToKnownDeviceNativeAsync(Guid deviceGuid, ConnectParameters connectParameters, CancellationToken cancellationToken)
        {
            //convert GUID to string and take last 12 characters as MAC address
            var guidString = deviceGuid.ToString("N").Substring(20);
            var bluetoothAddress = Convert.ToUInt64(guidString, 16);
            var nativeDevice = await BluetoothLEDevice.FromBluetoothAddressAsync(bluetoothAddress).AsTask(cancellationToken);

            if (nativeDevice == null)
            {
                return null;
            }

            return new Device(this, nativeDevice, 0, deviceGuid);
        }

        /// <summary>
        /// Windows offers no synchronous way to enumerate paired or system-connected devices, so this returns
        /// only the devices this application has connected.
        /// </summary>
        /// <param name="services">Ignored on Windows.</param>
        public IReadOnlyList<Device> GetSystemConnectedOrPairedDevices(Guid[] services = null)
        {
            //currently no way to retrieve paired and connected devices on windows without using an
            //async method.
            Trace.Message("Returning devices connected by this app only");
            return ConnectedDevices;
        }

        partial void DisposeNative()
        {
            var watcher = _bleWatcher;

            if (watcher != null)
            {
                watcher.Received -= DeviceFoundAsync;

                try
                {
                    watcher.Stop();
                }
                catch (Exception ex)
                {
                    Trace.Message("Adapter: Stopping the advertisement watcher during disposal failed: {0}", ex.Message);
                }

                _bleWatcher = null;
            }

            _serviceUuids = null;
        }

        /// <summary>
        /// Parses a given advertisement for various stored properties
        /// Currently only parses the manufacturer specific data
        /// </summary>
        /// <param name="adv">The advertisement to parse</param>
        /// <returns>List of generic advertisement records</returns>
        internal static List<AdvertisementRecord> ParseAdvertisementData(BluetoothLEAdvertisement adv)
        {
            var advList = adv.DataSections;

            return advList.Select(data => new AdvertisementRecord((AdvertisementRecordType)data.DataType, data.Data?.ToArray())).ToList();
        }

        /// <summary>
        /// Handler for devices found when duplicates are not allowed
        /// </summary>
        /// <param name="watcher">The bluetooth advertisement watcher currently being used</param>
        /// <param name="btAdv">The advertisement recieved by the watcher</param>
        private async void DeviceFoundAsync(BluetoothLEAdvertisementWatcher watcher, BluetoothLEAdvertisementReceivedEventArgs btAdv)
        {
            // This is an async void handler on a system callback: nothing observes the task, so an exception
            // escaping it is an unhandled exception on a thread-pool thread and terminates the process.
            try
            {
                var deviceId = ParseDeviceId(btAdv.BluetoothAddress);

                // The service filter is applied to the advertisement, before anything is resolved from the
                // address. Previously it ran only for newly seen devices and it queried the device's GATT
                // services to do it, which forms a connection - during a scan, for every unknown device in
                // range. It also let a device advertising no services at all through a non-empty filter.
                if (!MatchesServiceFilter(btAdv))
                {
                    return;
                }

                if (DiscoveredDevicesRegistry.TryGetValue(deviceId, out var device))
                {
                    Trace.Message("AdvertisedPeripheral: {0} Id: {1}, Rssi: {2}", device.Name, device.Id, btAdv.RawSignalStrengthInDBm);
                    device.Update(btAdv.RawSignalStrengthInDBm, ParseAdvertisementData(btAdv.Advertisement));
                    HandleDiscoveredDevice(device);
                    return;
                }

                var bluetoothLeDevice = await BluetoothLEDevice.FromBluetoothAddressAsync(btAdv.BluetoothAddress);
                if (bluetoothLeDevice == null)
                {
                    //make sure advertisement bluetooth address actually returns a device
                    return;
                }

                device = new Device(this, bluetoothLeDevice, btAdv.RawSignalStrengthInDBm, deviceId, ParseAdvertisementData(btAdv.Advertisement));

                if (DiscoveredDevicesRegistry.TryGetValue(device.Id, out var existingDevice))
                {
                    // Another advertisement for the same device resolved while this one was awaiting.
                    existingDevice.MergeOrUpdateAdvertising(device.AdvertisementRecords);
                    return;
                }

                Trace.Message("DiscoveredPeripheral: {0} Id: {1}, Rssi: {2}", device.Name, device.Id, btAdv.RawSignalStrengthInDBm);
                HandleDiscoveredDevice(device);
            }
            catch (Exception ex)
            {
                Trace.Message("Adapter: Failed to handle a received advertisement: {0}", ex);
            }
        }

        private bool MatchesServiceFilter(BluetoothLEAdvertisementReceivedEventArgs btAdv)
        {
            if (!HasFilter)
            {
                return true;
            }

            var advertised = btAdv.Advertisement?.ServiceUuids;

            // A device advertising no services fails a non-empty filter. The previous code took the opposite
            // view and let it through, so a filtered scan reported devices that could not possibly match.
            if (advertised == null || advertised.Count == 0)
            {
                return false;
            }

            return advertised.Any(uuid => _serviceUuids.Contains(uuid));
        }

        /// <summary>
        /// Method to parse the bluetooth address as a hex string to a UUID
        /// </summary>
        /// <param name="bluetoothAddress">BluetoothLEDevice native device address</param>
        /// <returns>a GUID that is padded left with 0 and the last 6 bytes are the bluetooth address</returns>
        private static Guid ParseDeviceId(ulong bluetoothAddress)
        {
            var macWithoutColons = bluetoothAddress.ToString("x");
            macWithoutColons = macWithoutColons.PadLeft(12, '0'); //ensure valid length
            var deviceGuid = new byte[16];
            Array.Clear(deviceGuid, 0, 16);
            var macBytes = Enumerable.Range(0, macWithoutColons.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(macWithoutColons.Substring(x, 2), 16))
                .ToArray();
            macBytes.CopyTo(deviceGuid, 10);
            return new Guid(deviceGuid);
        }
    }
}
