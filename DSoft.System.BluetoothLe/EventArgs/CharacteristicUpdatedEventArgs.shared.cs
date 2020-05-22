using Plugin.BLE.Contracts;

namespace Plugin.BLE.EventArgs
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