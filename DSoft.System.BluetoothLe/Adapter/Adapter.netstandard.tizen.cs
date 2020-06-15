using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.BluetoothLe
{
    public partial class Adapter
    {
        protected Task StartScanningForDevicesNativeAsync(Guid[] serviceUuids, bool allowDuplicatesKey, CancellationToken scanCancellationToken) => throw new PlatformNotSupportedException();

        protected void StopScanNative() => throw new PlatformNotSupportedException();

        protected Task ConnectToDeviceNativeAsync(Device device, ConnectParameters connectParameters, CancellationToken cancellationToken) => throw new PlatformNotSupportedException();

        protected void DisconnectDeviceNative(Device device) => throw new PlatformNotSupportedException();

        public Task<Device> ConnectToKnownDeviceAsync(Guid deviceGuid, ConnectParameters connectParameters = default, CancellationToken cancellationToken = default) => throw new PlatformNotSupportedException();

        public IReadOnlyList<Device> GetSystemConnectedOrPairedDevices(Guid[] services = null) => throw new PlatformNotSupportedException();

    }
}
