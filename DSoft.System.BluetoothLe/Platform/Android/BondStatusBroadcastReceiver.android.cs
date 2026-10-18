using System;
using Android.Bluetooth;
using Android.Content;
using System.BluetoothLe;
using System.BluetoothLe.EventArgs;
using Trace = System.BluetoothLe.Trace;

namespace System.BluetoothLe.BroadcastReceivers
{
    /// <summary>
    /// Receives the system's <see cref="BluetoothDevice.ActionBondStateChanged"/> broadcast and reports it as a
    /// <see cref="DeviceBondStateChangedEventArgs"/>.
    /// </summary>
    /// <remarks>
    /// The <see cref="Adapter"/> registers one of these for its lifetime and raises
    /// <see cref="Adapter.DeviceBondStateChanged"/> from it, so an app does not need to register its own.
    /// </remarks>
    public class BondStatusBroadcastReceiver : BroadcastReceiver
    {
        private readonly Adapter _adapter;

        /// <summary>
        /// Raised for every bond state change the system broadcasts, on the main thread.
        /// </summary>
        public event EventHandler<DeviceBondStateChangedEventArgs> BondStateChanged;

        /// <summary>
        /// Creates a receiver that resolves devices through <see cref="BluetoothLE.Current"/>'s adapter.
        /// </summary>
        public BondStatusBroadcastReceiver()
        {
        }

        /// <summary>
        /// Creates a receiver that resolves devices through <paramref name="adapter"/> and reports each change to it.
        /// </summary>
        internal BondStatusBroadcastReceiver(Adapter adapter)
        {
            _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        }

        /// <inheritdoc/>
        public override void OnReceive(Context context, Intent intent)
        {
            // OnReceive is called from the main looper: an exception escaping it terminates the process.
            try
            {
                if (intent?.Action != BluetoothDevice.ActionBondStateChanged)
                {
                    return;
                }

#pragma warning disable CA1422 // The typed overload is API 33+; the untyped one is the only one available below that.
                var nativeDevice = OperatingSystem.IsAndroidVersionAtLeast(33)
                    ? (BluetoothDevice)intent.GetParcelableExtra(BluetoothDevice.ExtraDevice, Java.Lang.Class.FromType(typeof(BluetoothDevice)))
                    : (BluetoothDevice)intent.GetParcelableExtra(BluetoothDevice.ExtraDevice);
#pragma warning restore CA1422

                if (nativeDevice == null)
                {
                    return;
                }

                var bond = (Bond)intent.GetIntExtra(BluetoothDevice.ExtraBondState, (int)Bond.None);
                var state = Device.ToBondState(bond);

                var adapter = _adapter ?? BluetoothLE.Current.Adapter;
                if (adapter == null)
                {
                    Trace.Message("BondStatusBroadcastReceiver: No adapter is available to report the bond state change for {0}.", nativeDevice.Address);
                    return;
                }

                var device = adapter.ResolveDevice(nativeDevice);

                if (_adapter != null)
                {
                    _adapter.HandleBondStateChanged(device, state);
                }

                BondStateChanged?.Invoke(this, new DeviceBondStateChangedEventArgs(device, state));
            }
            catch (Exception ex)
            {
                Trace.Message("BondStatusBroadcastReceiver: Handling a bond state change failed: {0}", ex);
            }
        }
    }
}
