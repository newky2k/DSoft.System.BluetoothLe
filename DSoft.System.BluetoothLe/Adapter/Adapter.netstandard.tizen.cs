using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace System.BluetoothLe
{
    public partial class Adapter
    {
        // The message names what to do about it. A developer reaching this build has almost always referenced
        // the package from a project whose target framework has no platform, or from a platform TFM more
        // specific than any lib folder in the package, and "not supported" on its own does not say that.
        private const string NotSupportedMessage =
            "Bluetooth Low Energy is not implemented for this target framework. Reference DSoft.System.BluetoothLe "
            + "from a platform-specific target framework (net10.0-android, net10.0-ios, net10.0-maccatalyst, "
            + "net10.0-macos, net10.0-tvos, net10.0-windows or net481).";

        internal Adapter()
        {
        }

        protected Task StartScanningForDevicesNativeAsync(Guid[] serviceUuids, bool allowDuplicatesKey, CancellationToken scanCancellationToken) => throw new System.PlatformNotSupportedException(NotSupportedMessage);

        protected void StopScanNative() => throw new System.PlatformNotSupportedException(NotSupportedMessage);

        protected Task ConnectToDeviceNativeAsync(Device device, ConnectParameters connectParameters, CancellationToken cancellationToken) => throw new System.PlatformNotSupportedException(NotSupportedMessage);

        protected void DisconnectDeviceNative(Device device) => throw new System.PlatformNotSupportedException(NotSupportedMessage);

        /// <summary>
        /// Retrieves a device the platform already knows about, without connecting it.
        /// </summary>
        /// <exception cref="System.PlatformNotSupportedException">Always, on this target framework.</exception>
        protected Task<Device> ConnectToKnownDeviceNativeAsync(Guid deviceGuid, ConnectParameters connectParameters, CancellationToken cancellationToken) => throw new System.PlatformNotSupportedException(NotSupportedMessage);

        /// <inheritdoc cref="System.PlatformNotSupportedException"/>
        public IReadOnlyList<Device> GetSystemConnectedOrPairedDevices(Guid[] services = null) => throw new System.PlatformNotSupportedException(NotSupportedMessage);
    }
}
