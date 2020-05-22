using Android.Bluetooth;

namespace Plugin.BLE.CallbackEventArgs
{
    public class CharacteristicReadCallbackEventArgs
    {
        public BluetoothGattCharacteristic Characteristic { get; }

        public CharacteristicReadCallbackEventArgs(BluetoothGattCharacteristic characteristic)
        {
            Characteristic = characteristic;
        }
    }
}