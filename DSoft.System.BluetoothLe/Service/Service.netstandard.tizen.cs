using System;
using System.BluetoothLe.Contracts;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace System.BluetoothLe
{
    public partial class Service
    {
        public Guid NativeGuid => throw new PlatformNotSupportedException();

        public bool NativeIsPrimary => throw new PlatformNotSupportedException();

        protected Task<IList<ICharacteristic>> GetCharacteristicsNativeAsync() => throw new PlatformNotSupportedException();

        protected object NativeService => throw new PlatformNotSupportedException();

        public virtual void Dispose()
        {

        }
    }
}
