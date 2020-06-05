using System;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using System.BluetoothLe;
using System.BluetoothLe.Contracts;
using Windows.Security.Cryptography;
using System.BluetoothLe.Extensions;

namespace System.BluetoothLe
{
    public partial class Descriptor
    {
        /// <summary>
        /// The locally stored value of a descriptor updated after a
        /// notification or a read
        /// </summary>
        private byte[] _value;

        protected Guid NativeGuid => NativeDescriptor.Uuid;

        protected byte[] NativeValue => _value ?? new byte[0];

        protected GattDescriptor NativeDescriptor { get; private set; }


        public Descriptor(GattDescriptor nativeDescriptor, ICharacteristic characteristic) : this(characteristic)
        {
            NativeDescriptor = nativeDescriptor;
        }

        protected async Task<byte[]> ReadNativeAsync()
        {
            var readResult = await NativeDescriptor.ReadValueAsync(BleImplementation.CacheModeDescriptorRead);
            return _value = readResult.GetValueOrThrowIfError();
        }

        protected async Task WriteNativeAsync(byte[] data)
        {
            var result = await NativeDescriptor.WriteValueWithResultAsync(CryptographicBuffer.CreateFromByteArray(data));
            result.ThrowIfError();
        }
    }
}
