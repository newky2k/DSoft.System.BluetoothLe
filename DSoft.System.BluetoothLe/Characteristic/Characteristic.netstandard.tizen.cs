using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace System.BluetoothLe
{
    // There is no Bluetooth stack behind this target framework at all; it exists so that a shared library can
    // reference the package without multi-targeting. Every member throws the BCL's PlatformNotSupportedException
    // rather than the fork's now-deleted namesake, which a consumer's catch of the BCL type never caught.
    public partial class Characteristic
    {
        protected Guid NativeGuid => throw new System.PlatformNotSupportedException(NotSupported);
        protected string NativeUuid => throw new System.PlatformNotSupportedException(NotSupported);
        protected byte[] NativeValue => throw new System.PlatformNotSupportedException(NotSupported);
        protected CharacteristicPropertyType NativeProperties => throw new System.PlatformNotSupportedException(NotSupported);
        protected object NativeCharacteristic => throw new System.PlatformNotSupportedException(NotSupported);

        protected string NativeName => throw new System.PlatformNotSupportedException(NotSupported);

        private const string NotSupported = "Bluetooth LE is not available on this target framework. Reference a platform build of DSoft.System.BluetoothLe (Android, iOS, macOS, Mac Catalyst, tvOS or Windows) from the head project.";

        protected Task<IReadOnlyList<Descriptor>> GetDescriptorsNativeAsync(CancellationToken cancellationToken) => throw new System.PlatformNotSupportedException(NotSupported);

        protected Task<byte[]> ReadNativeAsync(CancellationToken cancellationToken) => throw new System.PlatformNotSupportedException(NotSupported);

        protected Task WriteNativeAsync(byte[] data, CharacteristicWriteType writeType, CancellationToken cancellationToken) => throw new System.PlatformNotSupportedException(NotSupported);

        protected Task StartUpdatesNativeAsync(CharacteristicUpdateMode mode, CancellationToken cancellationToken) => throw new System.PlatformNotSupportedException(NotSupported);

        protected Task StopUpdatesNativeAsync(CancellationToken cancellationToken) => throw new System.PlatformNotSupportedException(NotSupported);
    }
}
