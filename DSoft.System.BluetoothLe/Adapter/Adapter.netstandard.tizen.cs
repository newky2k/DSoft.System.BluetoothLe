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

        /// <summary>
        /// Connects to a known device asynchronously.
        /// </summary>
        /// <param name="deviceGuid">The device unique identifier.</param>
        /// <param name="connectParameters">The connection parameters.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="dontThrowExceptionOnNotFound">if set to <c>true</c> [dont throw exception on not found].</param>
        /// <returns></returns>
        /// <exception cref="PlatformNotSupportedException"></exception>
        public Task<Device> ConnectToKnownDeviceAsync(Guid deviceGuid, ConnectParameters connectParameters = default, CancellationToken cancellationToken = default(CancellationToken), bool dontThrowExceptionOnNotFound = false) => throw new PlatformNotSupportedException();

        public IReadOnlyList<Device> GetSystemConnectedOrPairedDevices(Guid[] services = null) => throw new PlatformNotSupportedException();

    }
}
