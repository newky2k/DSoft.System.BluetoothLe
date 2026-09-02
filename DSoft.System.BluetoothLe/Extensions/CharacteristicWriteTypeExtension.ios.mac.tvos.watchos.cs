using System;
using CoreBluetooth;
using System.BluetoothLe;

namespace System.BluetoothLe.Extensions
{
    internal static class CharacteristicWriteTypeExtension
    {
        public static CBCharacteristicWriteType ToNative(this CharacteristicWriteType writeType)
        {
            switch (writeType)
            {
                case CharacteristicWriteType.WithResponse:
                    return CBCharacteristicWriteType.WithResponse;
                case CharacteristicWriteType.WithoutResponse:
                    return CBCharacteristicWriteType.WithoutResponse;
                default:
                    // Reached only if a caller sets WriteType to Default and bypasses Characteristic.WriteAsync,
                    // which resolves Default from the characteristic's properties before getting here.
                    throw new ArgumentOutOfRangeException(nameof(writeType), writeType, "There is no native write type for this value.");
            }
        }
    }

}