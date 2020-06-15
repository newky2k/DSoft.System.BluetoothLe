using System.BluetoothLe;
using System;
using System.Collections.Generic;
using System.Text;

namespace System.BluetoothLe
{
    public partial class BluetoothLE
    {
        internal Adapter CreateNativeAdapter() => throw new PlatformNotSupportedException();

        internal BluetoothState GetInitialStateNative() => throw new PlatformNotSupportedException();

        internal void InitializeNative() => throw new PlatformNotSupportedException();
    }
}
