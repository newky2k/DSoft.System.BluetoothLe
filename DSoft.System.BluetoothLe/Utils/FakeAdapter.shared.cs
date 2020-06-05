using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.BluetoothLe.Contracts;
using System.BluetoothLe.EventArgs;

namespace System.BluetoothLe.Utils
{
    internal class FakeAdapter : IAdapter
    {
        public bool IsScanning { get; }
        public int ScanTimeout { get; set; }
        public ScanMode ScanMode { get; set; }
        public IReadOnlyList<IDevice> DiscoveredDevices { get; }
        public IReadOnlyList<IDevice> ConnectedDevices { get; }

        public event EventHandler<DeviceEventArgs> DeviceAdvertised;
        public event EventHandler<DeviceEventArgs> DeviceDiscovered;
        public event EventHandler<DeviceEventArgs> DeviceConnected;
        public event EventHandler<DeviceEventArgs> DeviceDisconnected;
        public event EventHandler<DeviceErrorEventArgs> DeviceConnectionLost;
        public event EventHandler ScanTimeoutElapsed;

        public Task<IDevice> ConnectToKnownDeviceAsync(Guid deviceGuid, ConnectParameters connectParameters, CancellationToken cancellationToken)
        {
            TraceUnavailability();
            return Task.FromResult<IDevice>(null);
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

        protected Task ConnectToDeviceNativeAsync(IDevice device, ConnectParameters connectParameters, CancellationToken cancellationToken)
        {
            TraceUnavailability();
            return Task.FromResult(0);
        }

        protected void DisconnectDeviceNative(IDevice device)
        {
            TraceUnavailability();
        }

        private static void TraceUnavailability()
        {
            Trace.Message("Bluetooth LE is not available on this device. Nothing will happen - ever!");
        }

        public IReadOnlyList<IDevice> GetSystemConnectedOrPairedDevices(Guid[] services = null)
        {
            TraceUnavailability();
            return new List<IDevice>();
        }

        public Task StartScanningForDevicesAsync(Guid[] serviceUuids = null, Func<IDevice, bool> deviceFilter = null, bool allowDuplicatesKey = false, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task StopScanningForDevicesAsync()
        {
            throw new NotImplementedException();
        }

        public Task ConnectToDeviceAsync(IDevice device, ConnectParameters connectParameters = default, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task DisconnectDeviceAsync(IDevice device)
        {
            throw new NotImplementedException();
        }
    }
}