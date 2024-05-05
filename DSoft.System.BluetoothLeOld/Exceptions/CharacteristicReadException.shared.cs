using System;

namespace System.BluetoothLe
{
    public class CharacteristicReadException : Exception
    {
        public CharacteristicReadException(string message) : base(message)
        {
        }
    }
}