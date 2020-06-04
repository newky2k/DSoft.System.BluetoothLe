using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.BluetoothLe.Contracts;
using System.BluetoothLe.EventArgs;


namespace System.BluetoothLe
{
    public partial class Characteristic
    {
        public Guid NativeGuid => throw new PlatformNotSupportedException();
        public string NativeUuid => throw new PlatformNotSupportedException();
        public byte[] NativeValue => throw new PlatformNotSupportedException();
        public CharacteristicPropertyType NativeProperties => throw new PlatformNotSupportedException();
        public object NativeCharacteristic => throw new PlatformNotSupportedException();

        public string NativeName => KnownCharacteristics.Lookup(Id).Name;

        protected Task<IReadOnlyList<IDescriptor>> GetDescriptorsNativeAsync() => throw new PlatformNotSupportedException();

        protected Task<byte[]> ReadNativeAsync() => throw new PlatformNotSupportedException();

        protected Task<bool> WriteNativeAsync(byte[] data, CharacteristicWriteType writeType) => throw new PlatformNotSupportedException();

        protected Task StartUpdatesNativeAsync() => throw new PlatformNotSupportedException();

        protected Task StopUpdatesNativeAsync() => throw new PlatformNotSupportedException();
    }
}
