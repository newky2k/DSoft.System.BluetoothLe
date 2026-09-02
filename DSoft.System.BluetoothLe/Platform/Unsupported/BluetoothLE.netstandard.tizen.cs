using System;

namespace System.BluetoothLe
{
    public partial class BluetoothLE
    {
        // The message names the target framework so a developer who has accidentally resolved the
        // platform-neutral build - which is what happens when a package reference is made from a non-platform
        // project, or when a platform TFM version is more specific than any lib folder in the package - is
        // told what to do about it rather than just told "not supported".
        private const string NotSupportedMessage =
            "Bluetooth Low Energy is not implemented for this target framework. Reference DSoft.System.BluetoothLe "
            + "from a platform-specific target framework (net10.0-android, net10.0-ios, net10.0-maccatalyst, "
            + "net10.0-macos, net10.0-tvos, net10.0-windows or net481).";

        internal Adapter CreateNativeAdapter() => throw new System.PlatformNotSupportedException(NotSupportedMessage);

        // Reporting Unavailable rather than throwing is what lets BluetoothLE.Current, State, IsAvailable and
        // IsOn all work on this target. A consumer with shared code that checks availability before doing
        // anything Bluetooth-shaped then behaves correctly instead of having to guard the check itself; only
        // reaching for the Adapter throws.
        internal BluetoothState GetInitialStateNative() => BluetoothState.Unavailable;

        internal void InitializeNative()
        {
            // Nothing to bring up: there is no radio binding on this target.
        }
    }
}
