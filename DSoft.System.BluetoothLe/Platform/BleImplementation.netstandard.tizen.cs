using System.BluetoothLe;
using System.BluetoothLe.Contracts;
using System;
using System.Collections.Generic;
using System.Text;

namespace System.BluetoothLe
{
    public partial class BleImplementation
    {
        protected IAdapter CreateNativeAdapter() => throw new PlatformNotSupportedException();

        protected BluetoothState GetInitialStateNative() => throw new PlatformNotSupportedException();

        protected void InitializeNative() => throw new PlatformNotSupportedException();
    }
}
