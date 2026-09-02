using Android.Bluetooth;

namespace System.BluetoothLe.EventArgs
{
    /// <summary>
    /// Carries a characteristic read or notification up from the Android GATT callback.
    /// </summary>
    /// <remarks>
    /// <see cref="Value"/> and <see cref="Status"/> are captured inside the callback rather than read back
    /// from <see cref="Characteristic"/> by the handler. Android reuses the same
    /// <see cref="BluetoothGattCharacteristic"/> object for every operation on that attribute, so a handler
    /// that called <c>GetValue()</c> later could observe bytes belonging to whichever operation ran next -
    /// which is how a read could return another operation's data.
    /// </remarks>
    public class CharacteristicReadCallbackEventArgs
    {
        /// <summary>The characteristic the callback was about.</summary>
        public BluetoothGattCharacteristic Characteristic { get; }

        /// <summary>The bytes as they stood at the moment of the callback.</summary>
        public byte[] Value { get; }

        /// <summary>
        /// The GATT status the platform reported. <see cref="GattStatus.Success"/> for a notification, which
        /// carries no status of its own.
        /// </summary>
        public GattStatus Status { get; }

        public CharacteristicReadCallbackEventArgs(BluetoothGattCharacteristic characteristic, byte[] value, GattStatus status)
        {
            Characteristic = characteristic;
            Value = value;
            Status = status;
        }
    }
}
