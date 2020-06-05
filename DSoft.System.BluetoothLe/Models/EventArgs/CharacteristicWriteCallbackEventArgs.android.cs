using System;
using Android.Bluetooth;

namespace System.BluetoothLe.CallbackEventArgs
{
    public class CharacteristicWriteCallbackEventArgs
    {
        public BluetoothGattCharacteristic Characteristic { get; }
        public Exception Exception { get; }

        public CharacteristicWriteCallbackEventArgs(BluetoothGattCharacteristic characteristic, Exception exception = null)
        {
            Characteristic = characteristic;
            Exception = exception;
        }
    }
}