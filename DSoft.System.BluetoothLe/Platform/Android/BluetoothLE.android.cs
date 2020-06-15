using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using System.BluetoothLe;
using System.BluetoothLe.Utils;
using System.BluetoothLe.BroadcastReceivers;
using System.BluetoothLe.Extensions;
using Android.Graphics;

namespace System.BluetoothLe
{
    public partial class BluetoothLE
    {
        #region Fields

        private static volatile Handler _handler;
        private BluetoothManager _bluetoothManager;

        #endregion

        #region Properties

        /// <summary>
        /// Set this field to force are task builder execute() actions to be invoked on the main app tread one at a time (synchronous queue)
        /// </summary>
        internal static bool ShouldQueueOnMainThread { get; set; } = true;

        private static bool IsMainThread
        {
            get
            {
                if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
                {
                    return Looper.MainLooper.IsCurrentThread;
                }

                return Looper.MyLooper() == Looper.MainLooper;
            }
        }

        #endregion

        #region Methods

        internal void InitializeNative()
        {
            var ctx = Application.Context;
            if (!ctx.PackageManager.HasSystemFeature(PackageManager.FeatureBluetoothLe))
                return;

            var statusChangeReceiver = new BluetoothStatusBroadcastReceiver(state => State = state);
            ctx.RegisterReceiver(statusChangeReceiver, new IntentFilter(BluetoothAdapter.ActionStateChanged));

            _bluetoothManager = (BluetoothManager)ctx.GetSystemService(Context.BluetoothService);

            if (ShouldQueueOnMainThread)
            {
                TaskBuilder.MainThreadInvoker = action =>
                {

                    if (IsMainThread)
                    {
                        action();
                    }
                    else
                    {
                        if (_handler == null)
                        {
                            _handler = new Handler(Looper.MainLooper);
                        }

                        _handler.Post(action);
                    }
                };
            }
        }

        internal BluetoothState GetInitialStateNative() => _bluetoothManager?.Adapter.State.ToBluetoothState() ?? BluetoothState.Unavailable;

        internal Adapter CreateNativeAdapter()  => new Adapter(_bluetoothManager);

        #endregion
    }
}