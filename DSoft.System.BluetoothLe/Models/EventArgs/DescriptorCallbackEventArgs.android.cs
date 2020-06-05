using System;
using Android.Bluetooth;

namespace System.BluetoothLe.EventArgs
{
    public class DescriptorCallbackEventArgs
    {
        public BluetoothGattDescriptor Descriptor { get; }
        public Exception Exception { get; }

        public DescriptorCallbackEventArgs(BluetoothGattDescriptor descriptor, Exception exception = null)
        {
            Descriptor = descriptor;
            Exception = exception;
        }
    }
}
