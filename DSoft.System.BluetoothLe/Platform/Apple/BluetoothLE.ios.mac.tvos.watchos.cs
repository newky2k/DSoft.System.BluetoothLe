using CoreBluetooth;
using CoreFoundation;

using System.BluetoothLe;
using System.BluetoothLe.Extensions;

namespace System.BluetoothLe
{
    public partial class BluetoothLE
    {
        #region Fields
        private static string _restorationIdentifier;
        private static bool _showPowerAlert = true;
        private CBCentralManager _centralManager;
        private IBleCentralManagerDelegate _bleCentralManagerDelegate;

        // Held in a field rather than written as an inline lambda so that DisposeNative can unsubscribe it.
        // The delegate outlives this object otherwise, and keeps it reachable through the event.
        private EventHandler _updatedStateHandler;

        #endregion

        #region Methods
        internal static void UseRestorationIdentifier(string restorationIdentifier)
        {
            _restorationIdentifier = restorationIdentifier;
        }

        internal static void ShowPowerAlert(bool showPowerAlert)
        {
            _showPowerAlert = showPowerAlert;
        }

        internal void InitializeNative()
        {
            var cmDelegate = new BleCentralManagerDelegate();
            _bleCentralManagerDelegate = cmDelegate;

            var options = CreateInitOptions();

            _centralManager = new CBCentralManager(cmDelegate, DispatchQueue.CurrentQueue, options);

            _updatedStateHandler = (s, e) => State = GetState();
            _bleCentralManagerDelegate.UpdatedState += _updatedStateHandler;
        }

        partial void DisposeNative()
        {
            if (_bleCentralManagerDelegate != null && _updatedStateHandler != null)
            {
                _bleCentralManagerDelegate.UpdatedState -= _updatedStateHandler;
            }

            _updatedStateHandler = null;

            // The native objects are released by dropping the last managed reference rather than by calling
            // Dispose on them. CoreBluetooth may still hold the delegate and deliver one more callback after
            // this point, and disposing an NSObject it is about to message crashes the process; letting the
            // runtime collect them once nothing is subscribed is both sufficient and safe.
            _centralManager = null;
            _bleCentralManagerDelegate = null;
        }

        internal BluetoothState GetInitialStateNative()
        {
            return GetState();
        }

        internal Adapter CreateNativeAdapter()
        {
            return new Adapter(_centralManager, _bleCentralManagerDelegate);
        }

        private BluetoothState GetState()
        {
            return _centralManager?.State.ToBluetoothState() ?? BluetoothState.Unavailable;
        }

        private CBCentralInitOptions CreateInitOptions()
        {
            return new CBCentralInitOptions
            {
#if __IOS__
                RestoreIdentifier = _restorationIdentifier,
#endif
                ShowPowerAlert = _showPowerAlert
            };
        }

        #endregion
    }
}