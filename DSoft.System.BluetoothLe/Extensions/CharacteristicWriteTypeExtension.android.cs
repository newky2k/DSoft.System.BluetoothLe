using System;
using Android.Bluetooth;
using System.BluetoothLe;

namespace System.BluetoothLe.Extensions
{
    internal static class CharacteristicWriteTypeExtension
    {
        public static GattWriteType ToNative(this CharacteristicWriteType writeType)
        {
            switch (writeType)
            {
                case CharacteristicWriteType.WithResponse:
                    return GattWriteType.Default;
                case CharacteristicWriteType.WithoutResponse:
                    return GattWriteType.NoResponse;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}