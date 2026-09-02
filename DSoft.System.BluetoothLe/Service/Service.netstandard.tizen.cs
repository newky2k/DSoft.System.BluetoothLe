using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.BluetoothLe
{
    // See the note on Characteristic.netstandard.tizen.cs: this target framework has no Bluetooth stack, and
    // every member throws the BCL's PlatformNotSupportedException rather than the fork's deleted namesake.
    public partial class Service
    {
        private const string NotSupported = "Bluetooth LE is not available on this target framework. Reference a platform build of DSoft.System.BluetoothLe (Android, iOS, macOS, Mac Catalyst, tvOS or Windows) from the head project.";

        internal Guid NativeGuid => throw new System.PlatformNotSupportedException(NotSupported);

        internal bool NativeIsPrimary => throw new System.PlatformNotSupportedException(NotSupported);

        internal Task<IList<Characteristic>> GetCharacteristicsNativeAsync(CancellationToken cancellationToken) => throw new System.PlatformNotSupportedException(NotSupported);

        internal object NativeService => throw new System.PlatformNotSupportedException(NotSupported);
    }
}
