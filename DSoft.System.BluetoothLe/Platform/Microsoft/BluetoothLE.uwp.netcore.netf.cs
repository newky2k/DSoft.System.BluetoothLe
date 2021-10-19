using Windows.Devices.Bluetooth;

using System.BluetoothLe;
using System.Threading.Tasks;
using Windows.Devices.Radios;

namespace System.BluetoothLe
{
    public partial class BluetoothLE
    {
        #region Fields

        private BluetoothAdapter _bluetoothadapter;
        private Radio _radio;

        #endregion

        #region Properties
        internal static BluetoothCacheMode CacheModeCharacteristicRead { get; set; } = BluetoothCacheMode.Uncached;

        internal static BluetoothCacheMode CacheModeDescriptorRead { get; set; } = BluetoothCacheMode.Uncached;

        internal static BluetoothCacheMode CacheModeGetDescriptors { get; set; } = BluetoothCacheMode.Cached;

        internal static BluetoothCacheMode CacheModeGetCharacteristics { get; set; } = BluetoothCacheMode.Cached;

        internal static BluetoothCacheMode CacheModeGetServices { get; set; } = BluetoothCacheMode.Cached;

        private BluetoothAdapter NativeAdapter
        {
            get => _bluetoothadapter;
            set
            {
                _bluetoothadapter = value;

                if (_bluetoothadapter == null)
                {
                    State = BluetoothState.Unavailable;
                }

                State = BluetoothState.On;
            }
        }

        #endregion

        #region Methods


        internal Adapter CreateNativeAdapter()
        {
            return new Adapter();
        }



        internal BluetoothState GetInitialStateNative()
        {
            //The only way to get the state of bluetooth through windows is by
            //getting the radios for a device. This operation is asynchronous
            //and thus cannot be called in this method. Thus, we are just
            //returning "On" as long as the BluetoothLEHelper is initialized
            if (_bluetoothadapter == null)
                return BluetoothState.Unavailable;

            if (_radio == null)
                return BluetoothState.Unavailable;

            return BluetoothState.On;
        }


        internal void InitializeNative()
        {

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            InitAdapter();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

        }

        private async Task InitAdapter()
        {
            NativeAdapter = await BluetoothAdapter.GetDefaultAsync();

            _radio = await NativeAdapter.GetRadioAsync();

            if (_radio != null)
            {
                _radio.StateChanged += OnRadioStateChanged;
            }

        }

        private void OnRadioStateChanged(Radio sender, object args)
        {
            switch (sender.State)
            {
                case RadioState.Off:
                case RadioState.Disabled:
                    {
                        State = BluetoothState.Off;
                    }
                    break;
                case RadioState.On:
                    {
                        State = BluetoothState.On;
                    }
                    break;
                default:
                    {
                        State = BluetoothState.Unavailable;
                    }
                    break;
            }
        }

        #endregion
    }

}