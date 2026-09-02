using System;
using System.Threading;
using System.Threading.Tasks;

namespace System.BluetoothLe
{
    /// <summary>
    /// A single GATT descriptor on a characteristic.
    /// </summary>
    /// <remarks>
    /// Instances are cached by their owning <see cref="Characteristic"/>. Do not construct one directly.
    /// </remarks>
    public partial class Descriptor
    {
        private string _name;

        /// <summary>
        /// A human-readable name from the Bluetooth SIG's assigned-numbers table, or "Unknown descriptor".
        /// </summary>
        public string Name => _name ?? (_name = KnownDescriptors.Lookup(Id).Name);

        /// <summary>The most recently read value.</summary>
        public byte[] Value => NativeValue;

        /// <summary>The descriptor's UUID.</summary>
        public Guid Id => NativeGuid;

        /// <summary>The characteristic this descriptor belongs to.</summary>
        public Characteristic Characteristic { get; }

        #region Constructors

        private Descriptor(Characteristic characteristic)
        {
            Characteristic = characteristic ?? throw new ArgumentNullException(nameof(characteristic));
        }

        #endregion

        /// <summary>
        /// Reads the descriptor's value from the peripheral.
        /// </summary>
        /// <exception cref="DescriptorReadException">The peripheral rejected the read, or the link dropped.</exception>
        public async Task<byte[]> ReadAsync(CancellationToken cancellationToken = default)
        {
            using (var source = CreateOperationSource(cancellationToken))
            {
                return await ReadNativeAsync(source.Token);
            }
        }

        /// <summary>
        /// Writes a value to the descriptor.
        /// </summary>
        /// <exception cref="DescriptorWriteException">The peripheral rejected the write, or the link dropped.</exception>
        public async Task WriteAsync(byte[] data, CancellationToken cancellationToken = default)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            using (var source = CreateOperationSource(cancellationToken))
            {
                await WriteNativeAsync(data, source.Token);
            }
        }

        /// <summary>
        /// Links the caller's token to the owning device's, so that <see cref="Device.ClearServices"/> and
        /// disposal cancel GATT work that is already in flight rather than leaving it to time out.
        /// </summary>
        private CancellationTokenSource CreateOperationSource(CancellationToken cancellationToken)
            => Characteristic.Service.Device.GetCombinedSource(cancellationToken);
    }
}
