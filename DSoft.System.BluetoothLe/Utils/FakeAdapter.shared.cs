using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.BluetoothLe.EventArgs;

namespace System.BluetoothLe.Utils
{
    internal class FakeAdapter : IAdapter
    {
        public bool IsScanning { get; }
        public int ScanTimeout { get; set; }
        public ScanMode ScanMode { get; set; }
        public IReadOnlyList<Device> DiscoveredDevices { get; }
        public IReadOnlyList<Device> ConnectedDevices { get; }

        public event EventHandler<DeviceEventArgs> DeviceAdvertised;
        public event EventHandler<DeviceEventArgs> DeviceDiscovered;
        public event EventHandler<DeviceEventArgs> DeviceConnected;
        public event EventHandler<DeviceEventArgs> DeviceDisconnected;
        public event EventHandler<DeviceErrorEventArgs> DeviceConnectionLost;
        public event EventHandler ScanTimeoutElapsed;

        public Task<Device> ConnectToKnownDeviceAsync(Guid deviceGuid, ConnectParameters connectParameters, CancellationToken cancellationToken)
        {
            TraceUnavailability();
            return Task.FromResult<Device>(null);
        }

        protected Task StartScanningForDevicesNativeAsync(Guid[] serviceUuids, bool allowDuplicatesKey, CancellationToken scanCancellationToken)
        {
            TraceUnavailability();
            return Task.FromResult(0);
        }

        protected void StopScanNative()
        {
            TraceUnavailability();
        }

        protected Task ConnectToDeviceNativeAsync(Device device, ConnectParameters connectParameters, CancellationToken cancellationToken)
        {
            TraceUnavailability();
            return Task.FromResult(0);
        }

        protected void DisconnectDeviceNative(Device device)
        {
            TraceUnavailability();
        }

        private static void TraceUnavailability()
        {
            Trace.Message("Bluetooth LE is not available on this device. Nothing will happen - ever!");
        }

        public IReadOnlyList<Device> GetSystemConnectedOrPairedDevices(Guid[] services = null)
        {
            TraceUnavailability();
            return new List<Device>();
        }

        public Task StartScanningForDevicesAsync(Guid[] serviceUuids = null, Func<Device, bool> deviceFilter = null, bool allowDuplicatesKey = false, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task StopScanningForDevicesAsync()
        {
            throw new NotImplementedException();
        }

        public Task ConnectToDeviceAsync(Device device, ConnectParameters connectParameters = default, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task DisconnectDeviceAsync(Device device)
        {
            throw new NotImplementedException();
        }
    }
}