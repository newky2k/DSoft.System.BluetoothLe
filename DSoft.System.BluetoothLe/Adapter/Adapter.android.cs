using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.OS;
using System.BluetoothLe.Extensions;
using Trace = System.BluetoothLe.Trace;

namespace System.BluetoothLe
{
    public partial class Adapter
    {
        private readonly BluetoothManager _bluetoothManager;
        private readonly BluetoothAdapter _bluetoothAdapter;
        private readonly Api21BleScanCallback _api21ScanCallback;

        // Internal to match the Apple partial. An adapter is only ever constructed by BluetoothLE, which is the
        // only thing that holds a BluetoothManager to hand it.
        internal Adapter(BluetoothManager bluetoothManager)
        {
            _bluetoothManager = bluetoothManager;
            _bluetoothAdapter = bluetoothManager?.Adapter;

            // TODO: bonding
            //var bondStatusBroadcastReceiver = new BondStatusBroadcastReceiver();
            //Application.Context.RegisterReceiver(bondStatusBroadcastReceiver,
            //    new IntentFilter(BluetoothDevice.ActionBondStateChanged));

            ////forward events from broadcast receiver
            //bondStatusBroadcastReceiver.BondStateChanged += (s, args) =>
            //{
            //    //DeviceBondStateChanged(this, args);
            //};

            _api21ScanCallback = new Api21BleScanCallback(this);
        }

        protected Task StartScanningForDevicesNativeAsync(Guid[] serviceUuids, bool allowDuplicatesKey, CancellationToken scanCancellationToken)
        {
            // allowDuplicatesKey has no equivalent on Android: the platform scanner reports every advertisement
            // it receives and there is no setting to coalesce them.

            var hasFilter = serviceUuids?.Any() ?? false;
            List<ScanFilter> scanFilters = null;

            if (hasFilter)
            {
                scanFilters = new List<ScanFilter>();
                foreach (var serviceUuid in serviceUuids)
                {
                    var sfb = new ScanFilter.Builder();
                    sfb.SetServiceUuid(ParcelUuid.FromString(serviceUuid.ToString()));
                    scanFilters.Add(sfb.Build());
                }
            }

            var ssb = new ScanSettings.Builder();
            ssb.SetScanMode(ScanMode.ToNative());

            if (OperatingSystem.IsAndroidVersionAtLeast(26))
            {
                // enable Bluetooth 5 Advertisement Extensions on Android 8.0 and above
                ssb.SetLegacy(false);
            }
            //ssb.SetCallbackType(ScanCallbackType.AllMatches);

            var scanner = _bluetoothAdapter?.BluetoothLeScanner;
            if (scanner == null)
            {
                // A null scanner means the radio is off or there is no BLE hardware. Previously this traced and
                // returned, so the caller waited out the full ScanTimeout and then reported an empty room.
                Trace.Message("Adapter: Scan failed, the Bluetooth adapter is unavailable or switched off.");
                HandleScanFailed(ScanFailureReason.AdapterOff);
                return Task.CompletedTask;
            }

            Trace.Message($"Adapter: Starting a scan for devices. ScanMode: {ScanMode}");
            if (hasFilter)
            {
                Trace.Message($"ScanFilters: {string.Join(", ", serviceUuids)}");
            }

            scanner.StartScan(scanFilters, ssb.Build(), _api21ScanCallback);

            return Task.CompletedTask;
        }

        protected void StopScanNative()
        {
            Trace.Message("Adapter: Stopping the scan for devices.");
            _bluetoothAdapter?.BluetoothLeScanner?.StopScan(_api21ScanCallback);
        }

        protected Task ConnectToDeviceNativeAsync(Device device, ConnectParameters connectParameters,
            CancellationToken cancellationToken)
        {
            device.Connect(connectParameters, cancellationToken);
            return Task.CompletedTask;
        }

        protected void DisconnectDeviceNative(Device device)
        {
            //make sure everything is disconnected
            device.Disconnect();
        }

        /// <summary>
        /// Produces a device for an identifier the scan has not seen, without connecting it. The shared layer
        /// owns the connection, so this must not connect.
        /// </summary>
        protected Task<Device> ConnectToKnownDeviceNativeAsync(Guid deviceGuid, ConnectParameters connectParameters, CancellationToken cancellationToken)
        {
            // The library encodes an Android MAC address in the last six bytes of the device Guid.
            var macBytes = deviceGuid.ToByteArray().Skip(10).Take(6).ToArray();

            BluetoothDevice nativeDevice;
            try
            {
                nativeDevice = _bluetoothAdapter?.GetRemoteDevice(macBytes);
            }
            catch (Java.Lang.IllegalArgumentException ex)
            {
                // Android rejects an address it cannot parse rather than returning null, and that exception
                // used to escape unhandled from a method documented as throwing DeviceNotFoundException.
                Trace.Message("Adapter: {0} is not a valid Android device address: {1}", deviceGuid, ex.Message);
                return Task.FromResult<Device>(null);
            }

            if (nativeDevice == null)
            {
                return Task.FromResult<Device>(null);
            }

            // Where the system already holds a BluetoothDevice for this address - bonded, or connected by
            // another profile - use that one. It carries the cached name and bond state, which a device
            // fabricated from raw address bytes does not.
            var systemDevice = FindSystemDevice(nativeDevice.Address);
            if (systemDevice != null)
            {
                return Task.FromResult(new Device(this, systemDevice, null, 0, []));
            }

            // DELIBERATE DEVIATION FROM THE PLAN, recorded here because it is a behaviour decision: an address
            // the system has no record of is NOT reported as not-found. Android connects happily to an address
            // it has never seen, and refusing here would break the common pattern of storing a device id and
            // reconnecting on the next launch without scanning first. On Android the only genuine "not found"
            // is a malformed address, handled above; a device that is not really there fails the connection
            // attempt instead, with a DeviceConnectionException that says so.
            Trace.Message("Adapter: {0} is not bonded and not connected; connecting to the address directly.", deviceGuid);

            return Task.FromResult(new Device(this, nativeDevice, null, 0, []));
        }

        private BluetoothDevice FindSystemDevice(string address)
        {
            if (string.IsNullOrEmpty(address))
            {
                return null;
            }

            try
            {
                var connected = _bluetoothManager?.GetConnectedDevices(ProfileType.Gatt)
                    ?.FirstOrDefault(d => string.Equals(d.Address, address, StringComparison.OrdinalIgnoreCase));

                if (connected != null)
                {
                    return connected;
                }

                return _bluetoothAdapter?.BondedDevices
                    ?.FirstOrDefault(d => string.Equals(d.Address, address, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                // Enumerating bonded devices needs BLUETOOTH_CONNECT on Android 12 and later, and throws a
                // SecurityException without it. That is not a reason to fail the connection attempt.
                Trace.Message("Adapter: Could not enumerate system devices: {0}", ex.Message);
                return null;
            }
        }

        /// <summary>
        /// The devices the system knows about without this adapter having scanned for them: on Android, those
        /// bonded to the system plus those another profile currently has connected.
        /// </summary>
        /// <param name="services">Ignored on Android; the platform offers no way to filter by service here.</param>
        public IReadOnlyList<Device> GetSystemConnectedOrPairedDevices(Guid[] services = null)
        {
            if (services != null)
            {
                Trace.Message("Caution: GetSystemConnectedDevices does not take into account the 'services' parameter on Android.");
            }

            //add dualMode type too as they are BLE too ;)
            var connectedDevices = _bluetoothManager.GetConnectedDevices(ProfileType.Gatt).Where(d => d.Type == BluetoothDeviceType.Le || d.Type == BluetoothDeviceType.Dual);

            var bondedDevices = _bluetoothAdapter.BondedDevices.Where(d => d.Type == BluetoothDeviceType.Le || d.Type == BluetoothDeviceType.Dual);

            return connectedDevices.Union(bondedDevices, new DeviceComparer()).Select(d => new Device(this, d, null, 0)).ToList();
        }

        partial void DisposeNative()
        {
            try
            {
                _bluetoothAdapter?.BluetoothLeScanner?.StopScan(_api21ScanCallback);
            }
            catch (Exception ex)
            {
                Trace.Message("Adapter: Stopping the scanner during disposal failed: {0}", ex.Message);
            }

            _api21ScanCallback?.Dispose();
        }

        private class DeviceComparer : IEqualityComparer<BluetoothDevice>
        {
            public bool Equals(BluetoothDevice x, BluetoothDevice y)
            {
                return x.Address == y.Address;
            }

            public int GetHashCode(BluetoothDevice obj)
            {
                return obj.GetHashCode();
            }
        }

        public class Api21BleScanCallback : ScanCallback
        {
            private readonly Adapter _adapter;

            public Api21BleScanCallback(Adapter adapter)
            {
                _adapter = adapter;
            }

            public override void OnScanFailed(ScanFailure errorCode)
            {
                Trace.Message("Adapter: Scan failed with code {0}", errorCode);

                base.OnScanFailed(errorCode);

                _adapter.HandleScanFailed(Translate(errorCode));
            }

            public override void OnScanResult(ScanCallbackType callbackType, ScanResult result)
            {
                base.OnScanResult(callbackType, result);

                // This runs on a binder thread inside the Android BLE stack. An exception escaping here does not
                // reach any consumer code - it kills the process from a native frame - so everything the
                // adapter does with an advertisement is contained.
                try
                {
                    var scanRecord = result?.ScanRecord?.GetBytes();
                    if (result?.Device == null)
                    {
                        return;
                    }

                    var device = new Device(_adapter, result.Device, null, result.Rssi, scanRecord);

                    _adapter.HandleDiscoveredDevice(device);
                }
                catch (Exception ex)
                {
                    Trace.Message("Adapter: Failed to handle a scan result: {0}", ex);
                }
            }

            /// <summary>
            /// Maps Android's scan failure codes onto the reasons a consumer can actually act on.
            /// </summary>
            private static ScanFailureReason Translate(ScanFailure errorCode) => errorCode switch
            {
                ScanFailure.FeatureUnsupported => ScanFailureReason.NotSupported,

                // Android reports a missing BLUETOOTH_SCAN permission, and a scan client the system refuses to
                // register, through the same code. Both are fixed by the user granting a permission.
                ScanFailure.ApplicationRegistrationFailed => ScanFailureReason.PermissionDenied,

                _ => ScanFailureReason.InternalError,
            };
        }
    }
}
