using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace System.BluetoothLe
{
    public partial class Device
    {
        #region Properties
        internal object NativeDevice => throw new PlatformNotSupportedException();
        #endregion

        #region Methods

        public virtual void Dispose()
        {

            Adapter?.DisconnectDeviceAsync(this);
        }


        private Task<bool> UpdateRssiNativeAsync() => throw new PlatformNotSupportedException();

        private DeviceState GetState() => throw new PlatformNotSupportedException();

        private Task<IReadOnlyList<Service>> GetServicesNativeAsync() => throw new PlatformNotSupportedException();

        private Task<Service> GetServiceNativeAsync(Guid id) => throw new PlatformNotSupportedException();

        private Task<int> RequestMtuNativeAsync(int requestValue) => throw new PlatformNotSupportedException();

        private bool UpdateConnectionIntervalNative(ConnectionInterval interval) => throw new PlatformNotSupportedException();

        #endregion
    }
}
