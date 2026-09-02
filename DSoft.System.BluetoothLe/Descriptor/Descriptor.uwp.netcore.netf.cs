using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using System.BluetoothLe;
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

        protected byte[] NativeValue => _value ?? Array.Empty<byte>();

        protected GattDescriptor NativeDescriptor { get; private set; }


        internal Descriptor(GattDescriptor nativeDescriptor, Characteristic characteristic) : this(characteristic)
        {
            NativeDescriptor = nativeDescriptor;
        }

        protected async Task<byte[]> ReadNativeAsync(CancellationToken cancellationToken)
        {
            var readResult = await NativeDescriptor.ReadValueAsync(BluetoothLE.CacheModeDescriptorRead).AsTask(cancellationToken);

            if (readResult.Status != GattCommunicationStatus.Success)
            {
                throw new DescriptorReadException(
                    $"Read descriptor {Id} failed with status {readResult.Status}{ProtocolErrorDetail(readResult.ProtocolError)}.",
                    Id,
                    readResult.ProtocolError);
            }

            return _value = readResult.Value?.ToArray() ?? Array.Empty<byte>();
        }

        protected async Task WriteNativeAsync(byte[] data, CancellationToken cancellationToken)
        {
            var result = await NativeDescriptor
                .WriteValueWithResultAsync(CryptographicBuffer.CreateFromByteArray(data))
                .AsTask(cancellationToken);

            if (result.Status != GattCommunicationStatus.Success)
            {
                throw new DescriptorWriteException(
                    $"Write descriptor {Id} failed with status {result.Status}{ProtocolErrorDetail(result.ProtocolError)}.",
                    Id,
                    result.ProtocolError);
            }
        }

        private static string ProtocolErrorDetail(byte? protocolError)
            => protocolError.HasValue ? $" and protocol error {protocolError.GetErrorString()}" : string.Empty;
    }
}
