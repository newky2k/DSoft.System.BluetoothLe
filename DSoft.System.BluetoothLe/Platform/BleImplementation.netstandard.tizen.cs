using System.BluetoothLe;
using System.BluetoothLe.Contracts;
using System;
using System.Collections.Generic;
using System.Text;

namespace System.BluetoothLe
{
    public class BleImplementation : BleImplementationBase
    {
        protected override IAdapter CreateNativeAdapter()
        {
            throw new NotImplementedException();
        }

        protected override BluetoothState GetInitialStateNative()
        {
            throw new NotImplementedException();
        }

        protected override void InitializeNative()
        {
            throw new NotImplementedException();
        }
    }
}
