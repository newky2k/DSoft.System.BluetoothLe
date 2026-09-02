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

        internal static BluetoothCacheMode CacheModeGetDescriptors { get; set; } = BluetoothCacheMode.Uncached;

        internal static BluetoothCacheMode CacheModeGetCharacteristics { get; set; } = BluetoothCacheMode.Uncached;

        internal static BluetoothCacheMode CacheModeGetServices { get; set; } = BluetoothCacheMode.Uncached;

        private BluetoothAdapter NativeAdapter
        {
            get => _bluetoothadapter;
            set
            {
                _bluetoothadapter = value;

                // The early return was missing, so a machine with no Bluetooth adapter set Unavailable and
                // then immediately overwrote it with On - reporting a working radio on hardware that has none.
                if (_bluetoothadapter == null)
                {
                    State = BluetoothState.Unavailable;
                    return;
                }

                State = BluetoothState.On;
            }
        }

        public Radio NativeRadio => _radio;

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
            // Fire and forget by necessity: the shared Initialize() is synchronous, while the only way Windows
            // exposes radio state is an asynchronous adapter query. What has changed is that the continuation
            // is now guarded. The previous signature was async void, so a machine with no Bluetooth radio - or
            // an app without the capability declared - raised an unhandled exception on the thread pool from
            // inside the BluetoothLE.Current property getter, taking the process down.
            _ = InitAdapterSafeAsync();
        }

        private async Task InitAdapterSafeAsync()
        {
            try
            {
                await InitAdapter();
            }
            catch (Exception ex)
            {
                Trace.Message("BluetoothLE: Windows adapter initialisation failed: {0}", ex.Message);
                State = BluetoothState.Unavailable;
            }
        }

        private async Task InitAdapter()
        {
            NativeAdapter = await BluetoothAdapter.GetDefaultAsync();

            // GetDefaultAsync returns null when the machine has no Bluetooth adapter at all. Calling
            // GetRadioAsync on that threw a NullReferenceException out of the async void above.
            if (NativeAdapter == null)
            {
                State = BluetoothState.Unavailable;
                return;
            }

            _radio = await NativeAdapter.GetRadioAsync();

            if (_radio != null)
            {
                _radio.StateChanged += OnRadioStateChanged;
            }

            State = GetInitialStateNative();

        }

        partial void DisposeNative()
        {
            if (_radio != null)
            {
                _radio.StateChanged -= OnRadioStateChanged;
                _radio = null;
            }

            _bluetoothadapter = null;
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