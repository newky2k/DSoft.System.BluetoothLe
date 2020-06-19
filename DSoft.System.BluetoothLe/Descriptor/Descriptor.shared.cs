using System;
using System.Threading;
using System.Threading.Tasks;

namespace System.BluetoothLe
{
    public partial class Descriptor
    {
        private string _name;

        public string Name => _name ?? (_name = KnownDescriptors.Lookup(Id).Name);

        public byte[] Value => NativeValue;

        public Guid Id => NativeGuid;

        public Characteristic Characteristic { get; }

        #region Constructors

        protected Descriptor()
        {

        }

        protected Descriptor(Characteristic characteristic)
        {
            Characteristic = characteristic;
        }

        #endregion

        public Task<byte[]> ReadAsync(CancellationToken cancellationToken = default)
        {
            return ReadNativeAsync();
        }



        public Task WriteAsync(byte[] data, CancellationToken cancellationToken = default)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            return WriteNativeAsync(data);
        }



    }
}
