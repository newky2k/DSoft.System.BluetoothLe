﻿using System;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using System.BluetoothLe;
using System.BluetoothLe.Contracts;
using Windows.Security.Cryptography;
using System.BluetoothLe.Extensions;

namespace System.BluetoothLe
{
    public class Descriptor : DescriptorBase<GattDescriptor>
    {
        /// <summary>
        /// The locally stored value of a descriptor updated after a
        /// notification or a read
        /// </summary>
        private byte[] _value;
        public override Guid Id => NativeDescriptor.Uuid;
        public override byte[] Value => _value ?? new byte[0];

        public Descriptor(GattDescriptor nativeDescriptor, ICharacteristic characteristic) 
            : base(characteristic, nativeDescriptor)
        {
        }

        protected override async Task<byte[]> ReadNativeAsync()
        {
            var readResult = await NativeDescriptor.ReadValueAsync(BleImplementation.CacheModeDescriptorRead);
            return _value = readResult.GetValueOrThrowIfError();
        }

        protected override async Task WriteNativeAsync(byte[] data)
        {
            var result = await NativeDescriptor.WriteValueWithResultAsync(CryptographicBuffer.CreateFromByteArray(data));
            result.ThrowIfError();
        }
    }
}
