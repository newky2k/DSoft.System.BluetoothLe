using System;
using System.Threading;
using System.Threading.Tasks;
using System.BluetoothLe;

namespace System.BluetoothLe
{
    // See the note on Characteristic.netstandard.tizen.cs: this target framework has no Bluetooth stack, and
    // every member throws the BCL's PlatformNotSupportedException rather than the fork's deleted namesake.
    public partial class Descriptor
    {
        private const string NotSupported = "Bluetooth LE is not available on this target framework. Reference a platform build of DSoft.System.BluetoothLe (Android, iOS, macOS, Mac Catalyst, tvOS or Windows) from the head project.";

        protected object NativeDescriptor => throw new System.PlatformNotSupportedException(NotSupported);

        protected byte[] NativeValue => throw new System.PlatformNotSupportedException(NotSupported);

        protected Guid NativeGuid => throw new System.PlatformNotSupportedException(NotSupported);

        protected Task<byte[]> ReadNativeAsync(CancellationToken cancellationToken) => throw new System.PlatformNotSupportedException(NotSupported);

        protected Task WriteNativeAsync(byte[] data, CancellationToken cancellationToken) => throw new System.PlatformNotSupportedException(NotSupported);
    }
}
