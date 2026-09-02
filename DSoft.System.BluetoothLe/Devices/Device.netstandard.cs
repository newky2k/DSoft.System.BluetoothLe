using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace System.BluetoothLe
{
    public partial class Device
    {
        // This partial backs netstandard2.0 and the platform-neutral net10.0 target. Neither has a
        // Bluetooth stack to talk to, so every native member fails loudly rather than pretending.
        private const string NotSupportedMessage =
            "Bluetooth LE is not available on this target framework. Reference a platform-specific build (android, ios, maccatalyst, macos, tvos or windows).";

        #region Properties
        internal object NativeDevice => throw new System.PlatformNotSupportedException(NotSupportedMessage);
        #endregion

        #region Methods

        partial void DisposeNative()
        {
            // Nothing native was ever acquired on this target.
        }

        private Task<bool> UpdateRssiNativeAsync(CancellationToken cancellationToken) => throw new System.PlatformNotSupportedException(NotSupportedMessage);

        private DeviceState GetState() => throw new System.PlatformNotSupportedException(NotSupportedMessage);

        private Task<IReadOnlyList<Service>> GetServicesNativeAsync(CancellationToken cancellationToken) => throw new System.PlatformNotSupportedException(NotSupportedMessage);

        private Task<int> RequestMtuNativeAsync(int requestValue, CancellationToken cancellationToken) => throw new System.PlatformNotSupportedException(NotSupportedMessage);

        private bool UpdateConnectionIntervalNative(ConnectionInterval interval) => throw new System.PlatformNotSupportedException(NotSupportedMessage);

        #endregion
    }
}
