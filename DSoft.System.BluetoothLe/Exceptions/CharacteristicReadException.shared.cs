using System;

namespace Plugin.BLE.Exceptions
{
    public class CharacteristicReadException : Exception
    {
        public CharacteristicReadException(string message) : base(message)
        {
        }
    }
}