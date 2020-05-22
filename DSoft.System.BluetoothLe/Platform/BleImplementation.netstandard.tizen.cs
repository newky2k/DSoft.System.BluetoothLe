using Plugin.BLE;
using Plugin.BLE.Contracts;
using System;
using System.Collections.Generic;
using System.Text;

namespace Plugin.BLE
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
