using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace System.BluetoothLe
{
    public partial class Device
    {

        public Task<bool> UpdateRssiAsync() => throw new PlatformNotSupportedException();

        protected DeviceState GetState() => throw new PlatformNotSupportedException();
        protected Task<IReadOnlyList<IService>> GetServicesNativeAsync() => throw new PlatformNotSupportedException();
        protected Task<IService> GetServiceNativeAsync(Guid id) => throw new PlatformNotSupportedException();
        protected Task<int> RequestMtuNativeAsync(int requestValue) => throw new PlatformNotSupportedException();
        protected bool UpdateConnectionIntervalNative(ConnectionInterval interval) => throw new PlatformNotSupportedException();
        public object NativeDevice => throw new PlatformNotSupportedException();

    }
}
