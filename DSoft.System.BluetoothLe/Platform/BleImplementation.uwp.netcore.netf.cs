using Windows.Devices.Bluetooth;

using System.BluetoothLe;
using System.BluetoothLe.Contracts;
using System.Threading.Tasks;

namespace System.BluetoothLe
{
    public partial class BleImplementation
    {
        public static BluetoothCacheMode CacheModeCharacteristicRead { get; set; } = BluetoothCacheMode.Uncached;
        public static BluetoothCacheMode CacheModeDescriptorRead { get; set; } = BluetoothCacheMode.Uncached;
        public static BluetoothCacheMode CacheModeGetDescriptors { get; set; } = BluetoothCacheMode.Cached;
        public static BluetoothCacheMode CacheModeGetCharacteristics { get; set; } = BluetoothCacheMode.Cached;
        public static BluetoothCacheMode CacheModeGetServices { get; set; } = BluetoothCacheMode.Cached;

        private BluetoothAdapter _bluetoothadapter;

        protected IAdapter CreateNativeAdapter()
        {
            return new Adapter();
        }

        protected BluetoothState GetInitialStateNative()
        {
            //The only way to get the state of bluetooth through windows is by
            //getting the radios for a device. This operation is asynchronous
            //and thus cannot be called in this method. Thus, we are just
            //returning "On" as long as the BluetoothLEHelper is initialized
            if (_bluetoothadapter == null)
            {
                return BluetoothState.Unavailable;
            }
            return BluetoothState.On;
        }


        protected void InitializeNative()
        {

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            InitAdapter();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

        }

        public async Task InitAdapter()
        {
            _bluetoothadapter = await BluetoothAdapter.GetDefaultAsync();
        }
    }

}