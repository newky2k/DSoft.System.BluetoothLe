﻿using System;
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
using System.BluetoothLe.Exceptions;

namespace System.BluetoothLe
{
    public partial class Adapter
    {
        private BluetoothLEAdvertisementWatcher _bleWatcher;

        private Guid[] _serviceUuids;

        private bool HasFilter => _serviceUuids?.Any() ?? false;

        private List<Guid> _foundIds;

        protected Task StartScanningForDevicesNativeAsync(Guid[] serviceUuids, bool allowDuplicatesKey, CancellationToken scanCancellationToken)
        {

            _serviceUuids = serviceUuids;

            _bleWatcher = new BluetoothLEAdvertisementWatcher { ScanningMode = ScanMode.ToNative() };

            Trace.Message("Starting a scan for devices.");

            _foundIds = new List<Guid>();

            _bleWatcher.Received -= DeviceFoundAsync;
            _bleWatcher.Received += DeviceFoundAsync;

            _bleWatcher.Start();
            return Task.FromResult(true);
        }

        protected void StopScanNative()
        {
            if (_bleWatcher != null)
            {
                Trace.Message("Stopping the scan for devices");
                _bleWatcher.Stop();
                _bleWatcher = null;

                _foundIds = null;
            }
        }

        protected async Task ConnectToDeviceNativeAsync(Device device, ConnectParameters connectParameters, CancellationToken cancellationToken)
        {
            Trace.Message($"Connecting to device with ID:  {device.Id.ToString()}");

            if (!(device.NativeDevice is ObservableBluetoothLEDevice nativeDevice))
                return;

            nativeDevice.PropertyChanged -= Device_ConnectionStatusChanged;
            nativeDevice.PropertyChanged += Device_ConnectionStatusChanged;

            ConnectedDeviceRegistry[device.Id.ToString()] = device;

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

            var address = ParseDeviceId(nativeDevice.BluetoothLEDevice.BluetoothAddress).ToString();
            if (nativeDevice.IsConnected && ConnectedDeviceRegistry.TryGetValue(address, out var connectedDevice))
            {
                HandleConnectedDevice(connectedDevice);
                return;
            }

            if (!nativeDevice.IsConnected && ConnectedDeviceRegistry.TryRemove(address, out var disconnectedDevice))
            {
                HandleDisconnectedDevice(false, disconnectedDevice);
            }
        }

        protected void DisconnectDeviceNative(Device device)
        {
            // Windows doesn't support disconnecting, so currently just dispose of the device
            Trace.Message($"Disconnected from device with ID:  {device.Id.ToString()}");

            ((Device)device).ClearServices();
            if (device.NativeDevice is ObservableBluetoothLEDevice nativeDevice)
            {
                nativeDevice.BluetoothLEDevice.Dispose();
                ConnectedDeviceRegistry.TryRemove(device.Id.ToString(), out _);
            }

            HandleDisconnectedDevice(true, device);
        }

        public async Task<Device> ConnectToKnownDeviceAsync(Guid deviceGuid, ConnectParameters connectParameters = default, CancellationToken cancellationToken = default, bool dontThrowExceptionOnNotFound = false)
        {
            //convert GUID to string and take last 12 characters as MAC address
            var guidString = deviceGuid.ToString("N").Substring(20);
            var bluetoothAddress = Convert.ToUInt64(guidString, 16);
            var nativeDevice = await BluetoothLEDevice.FromBluetoothAddressAsync(bluetoothAddress);


            if (nativeDevice == null)
            {
                if (dontThrowExceptionOnNotFound == true)
                    return null;

                throw new DeviceNotFoundException(deviceGuid);
            }

            var knownDevice = new Device(this, nativeDevice, 0, deviceGuid);

            await ConnectToDeviceAsync(knownDevice, cancellationToken: cancellationToken);
            return knownDevice;
        }

        public IReadOnlyList<Device> GetSystemConnectedOrPairedDevices(Guid[] services = null)
        {
            //currently no way to retrieve paired and connected devices on windows without using an
            //async method. 
            Trace.Message("Returning devices connected by this app only");
            return ConnectedDevices;
        }

        /// <summary>
        /// Parses a given advertisement for various stored properties
        /// Currently only parses the manufacturer specific data
        /// </summary>
        /// <param name="adv">The advertisement to parse</param>
        /// <returns>List of generic advertisement records</returns>
        public static List<AdvertisementRecord> ParseAdvertisementData(BluetoothLEAdvertisement adv)
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
            var deviceId = ParseDeviceId(btAdv.BluetoothAddress);

            if (DiscoveredDevicesRegistry.TryGetValue(deviceId, out var device))
            {
                Trace.Message("AdvertisedPeripheral: {0} Id: {1}, Rssi: {2}", device.Name, device.Id, btAdv.RawSignalStrengthInDBm);
                (device as Device)?.Update(btAdv.RawSignalStrengthInDBm, ParseAdvertisementData(btAdv.Advertisement));
                this.HandleDiscoveredDevice(device);
            }
            else
            {
                var bluetoothLeDevice = await BluetoothLEDevice.FromBluetoothAddressAsync(btAdv.BluetoothAddress);
                if (bluetoothLeDevice != null) //make sure advertisement bluetooth address actually returns a device
                {

                    //if there is a filter on devices find the services for the device
                    if (HasFilter)
                    {
                        var services = await bluetoothLeDevice.GetGattServicesAsync();

                        if (services.Services.Any())
                        {
                           
                            //compare the list of services provided with the _serviceIds being listened for
                            var items = (from x in services.Services
                                         join y in _serviceUuids on x.Uuid equals y
                                         select x)
                                         .ToList();

                            //if no services then ignore
                            if (!items.Any())
                                return;
                        }
                        

                    }
                    

                    device = new Device(this, bluetoothLeDevice, btAdv.RawSignalStrengthInDBm, deviceId, ParseAdvertisementData(btAdv.Advertisement));

                    if (DiscoveredDevicesRegistry.ContainsKey(device.Id))
                    {
                        //try and merge advertising data
                        var existingDevice = DiscoveredDevicesRegistry[device.Id];

                        existingDevice.MergeOrUpdateAdvertising(device.AdvertisementRecords);

                        return;
                    }

                    Trace.Message("DiscoveredPeripheral: {0} Id: {1}, Rssi: {2}", device.Name, device.Id, btAdv.RawSignalStrengthInDBm);
                    this.HandleDiscoveredDevice(device);
                }
            }
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
