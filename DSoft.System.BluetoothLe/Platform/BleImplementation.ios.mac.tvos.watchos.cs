using CoreBluetooth;
using CoreFoundation;

using System.BluetoothLe;
using System.BluetoothLe.Extensions;

namespace System.BluetoothLe
{
    public partial class BleImplementation
    {
        private static string _restorationIdentifier;
        private static bool _showPowerAlert = true;

        private CBCentralManager _centralManager;
        private IBleCentralManagerDelegate _bleCentralManagerDelegate;

        public static void UseRestorationIdentifier(string restorationIdentifier)
        {
            _restorationIdentifier = restorationIdentifier;
        }

        public static void ShowPowerAlert(bool showPowerAlert)
        {
            _showPowerAlert = showPowerAlert;
        }

        protected void InitializeNative()
        {
            var cmDelegate = new BleCentralManagerDelegate();
            _bleCentralManagerDelegate = cmDelegate;

            var options = CreateInitOptions();

            _centralManager = new CBCentralManager(cmDelegate, DispatchQueue.CurrentQueue, options);
            _bleCentralManagerDelegate.UpdatedState += (s, e) => State = GetState();
        }

        protected BluetoothState GetInitialStateNative()
        {
            return GetState();
        }

        protected IAdapter CreateNativeAdapter()
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
    }
}