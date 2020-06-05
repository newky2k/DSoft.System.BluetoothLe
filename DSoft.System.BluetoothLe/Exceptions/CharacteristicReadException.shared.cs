using System;

namespace System.BluetoothLe.Exceptions
{
    public class CharacteristicReadException : Exception
    {
        public CharacteristicReadException(string message) : base(message)
        {
        }
    }
}