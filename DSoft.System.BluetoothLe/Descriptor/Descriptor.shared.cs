﻿using System;
using System.Threading;
using System.Threading.Tasks;
using System.BluetoothLe.Contracts;

namespace System.BluetoothLe
{
    public partial class Descriptor : IDescriptor
    {
        private string _name;

        public string Name => _name ?? (_name = KnownDescriptors.Lookup(Id).Name);

        public byte[] Value => NativeValue;

        public Guid Id => NativeGuid;

        public ICharacteristic Characteristic { get; }

        protected Descriptor(ICharacteristic characteristic)
        {
            Characteristic = characteristic;
        }

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