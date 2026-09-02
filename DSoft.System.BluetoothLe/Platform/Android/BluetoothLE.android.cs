using Android;
using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using System.BluetoothLe;
using System.BluetoothLe.Utils;
using System.BluetoothLe.BroadcastReceivers;
using System.BluetoothLe.Extensions;

namespace System.BluetoothLe
{
    public partial class BluetoothLE
    {
        #region Fields

        private static volatile Handler _handler;
        private BluetoothManager _bluetoothManager;

        // Held so DisposeNative can unregister it. Previously the receiver was created as a local, which left
        // it registered against the application context for the life of the process with no way to release it.
        private BluetoothStatusBroadcastReceiver _statusChangeReceiver;

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
                // OperatingSystem.IsAndroidVersionAtLeast is what the platform analyser understands, so the
                // API-23-and-later branch no longer reports CA1416 against the project's minimum of 21.
                if (OperatingSystem.IsAndroidVersionAtLeast(23))
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

            _statusChangeReceiver = new BluetoothStatusBroadcastReceiver(state => State = ApplyPermissionState(state));
            ctx.RegisterReceiver(_statusChangeReceiver, new IntentFilter(BluetoothAdapter.ActionStateChanged));

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

        internal BluetoothState GetInitialStateNative()
            => ApplyPermissionState(_bluetoothManager?.Adapter.State.ToBluetoothState() ?? BluetoothState.Unavailable);

        internal Adapter CreateNativeAdapter() => new Adapter(_bluetoothManager);

        /// <summary>
        /// Reports a radio that is switched on but unusable to this application as Unauthorized.
        /// </summary>
        /// <remarks>
        /// From Android 12 the runtime permissions BLUETOOTH_SCAN and BLUETOOTH_CONNECT gate every useful
        /// operation, but the adapter still reports itself as on when they are denied. Without this a consumer
        /// sees State.On, starts a scan, and gets a SecurityException or a silent empty result with nothing to
        /// tell the user. The check lives here rather than in BluetoothStateExtension so that the extension
        /// stays a pure enum mapping with no dependency on a Context.
        /// </remarks>
        private static BluetoothState ApplyPermissionState(BluetoothState state)
        {
            if (state != BluetoothState.On)
                return state;

            if (!OperatingSystem.IsAndroidVersionAtLeast(31))
                return state;

            var ctx = Application.Context;
            if (ctx.CheckSelfPermission(Manifest.Permission.BluetoothScan) != Permission.Granted
                || ctx.CheckSelfPermission(Manifest.Permission.BluetoothConnect) != Permission.Granted)
            {
                return BluetoothState.Unauthorized;
            }

            return state;
        }

        partial void DisposeNative()
        {
            if (_statusChangeReceiver != null)
            {
                try
                {
                    Application.Context.UnregisterReceiver(_statusChangeReceiver);
                }
                catch (Java.Lang.IllegalArgumentException ex)
                {
                    // Android throws rather than no-ops when the receiver was never registered, or was already
                    // unregistered by the framework as the application shut down. Neither is a failure here.
                    Trace.Message("BluetoothLE.DisposeNative: receiver was not registered: {0}", ex.Message);
                }

                _statusChangeReceiver.Dispose();
                _statusChangeReceiver = null;
            }

            // The invoker and handler capture the main looper, so leaving them set keeps a torn-down stack
            // posting work to a thread the consumer may no longer expect to be used.
            TaskBuilder.MainThreadInvoker = null;
            _handler = null;
            _bluetoothManager = null;
        }

        #endregion
    }
}
