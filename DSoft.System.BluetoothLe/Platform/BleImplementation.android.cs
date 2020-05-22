using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using System.BluetoothLe.Contracts;
using System.BluetoothLe.Utils;
using System.BluetoothLe.BroadcastReceivers;
using System.BluetoothLe.Extensions;
using Adapter = System.BluetoothLe.Adapter;
using IAdapter = System.BluetoothLe.Contracts.IAdapter;

namespace System.BluetoothLe
{
    public class BleImplementation : BleImplementationBase
    {
        private static volatile Handler _handler;

        /// <summary>
        /// Set this field to force are task builder execute() actions to be invoked on the main app tread one at a time (synchronous queue)
        /// </summary>
        public static bool ShouldQueueOnMainThread { get; set; } = true;

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

        private BluetoothManager _bluetoothManager;


        protected override void InitializeNative()
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

        protected override BluetoothState GetInitialStateNative()
            => _bluetoothManager?.Adapter.State.ToBluetoothState() ?? BluetoothState.Unavailable;

        protected override IAdapter CreateNativeAdapter()
            => new Adapter(_bluetoothManager);
    }
}