using System.BluetoothLe.Contracts;

namespace System.BluetoothLe.EventArgs
{
    public class CharacteristicUpdatedEventArgs : System.EventArgs
    {
        public ICharacteristic Characteristic { get; set; }

        public CharacteristicUpdatedEventArgs(ICharacteristic characteristic)
        {
            Characteristic = characteristic;
        }
    }
}