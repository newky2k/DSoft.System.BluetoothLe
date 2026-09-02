using System;
using Android.Bluetooth;

namespace System.BluetoothLe.EventArgs
{
    /// <summary>
    /// Carries the outcome of a characteristic write up from the Android GATT callback.
    /// </summary>
    public class CharacteristicWriteCallbackEventArgs
    {
        /// <summary>The characteristic the callback was about.</summary>
        public BluetoothGattCharacteristic Characteristic { get; }

        /// <summary>Set when the platform reported a failing status, otherwise <see langword="null"/>.</summary>
        public Exception Exception { get; }

        /// <summary>
        /// The raw GATT status, carried so that the exception a caller sees can name the actual failure rather
        /// than only that one occurred.
        /// </summary>
        public GattStatus Status { get; }

        public CharacteristicWriteCallbackEventArgs(BluetoothGattCharacteristic characteristic, GattStatus status, Exception exception = null)
        {
            Characteristic = characteristic;
            Status = status;
            Exception = exception;
        }
    }
}
