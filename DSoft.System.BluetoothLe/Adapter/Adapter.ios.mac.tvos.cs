using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CoreBluetooth;
using Foundation;


namespace System.BluetoothLe
{
    public partial class Adapter
    {
        #region Fields

        private readonly CBCentralManager _centralManager;
        private readonly IBleCentralManagerDelegate _bleCentralManagerDelegate;

        /// <summary>
        /// Registry used to store device instances for pending operations : disconnect
        /// Helps to detect connection lost events.
        /// </summary>
        private readonly ConcurrentDictionary<Guid, Device> _deviceOperationRegistry = new ConcurrentDictionary<Guid, Device>();

        // The five delegate subscriptions are held in fields rather than written as inline lambdas, so that
        // DisposeNative can unsubscribe them. Until it could, the central manager delegate kept the adapter -
        // and through it every device it had ever seen - alive for the lifetime of the process.
        private readonly EventHandler<CBDiscoveredPeripheralEventArgs> _discoveredPeripheralHandler;
        private readonly EventHandler _updatedStateHandler;
        private readonly EventHandler<CBPeripheralEventArgs> _connectedPeripheralHandler;
        private readonly EventHandler<CBPeripheralErrorEventArgs> _disconnectedPeripheralHandler;
        private readonly EventHandler<CBPeripheralErrorEventArgs> _failedToConnectPeripheralHandler;

        // Waiters for a central manager state change. A TaskCompletionSource per waiter, rather than the
        // AutoResetEvent this used to poll on a thread-pool thread every two seconds: an AutoResetEvent
        // releases exactly one waiter per Set, so a second caller waiting for PoweredOn could sleep through
        // the transition it was waiting for and then wait a further two seconds to notice.
        private readonly object _stateWaitersLock = new object();
        private readonly List<TaskCompletionSource<CBManagerState>> _stateWaiters = new List<TaskCompletionSource<CBManagerState>>();

        #endregion

        #region Constructors
        internal Adapter(CBCentralManager centralManager, IBleCentralManagerDelegate bleCentralManagerDelegate)
        {
            _centralManager = centralManager;
            _bleCentralManagerDelegate = bleCentralManagerDelegate;

            // Every handler below runs on the CoreBluetooth delegate queue. An exception escaping one unwinds
            // into an Objective-C frame, which terminates the process; each is therefore contained.

            _discoveredPeripheralHandler = (sender, e) =>
            {
                try
                {
                    Trace.Message("DiscoveredPeripheral: {0}, Id: {1}", e.Peripheral.Name, e.Peripheral.Identifier);
                    var name = e.Peripheral.Name;
                    if (e.AdvertisementData.ContainsKey(CBAdvertisement.DataLocalNameKey))
                    {
                        // iOS caches the peripheral name, so it can become stale (if changing)
                        // keep track of the local name key manually
                        name = ((NSString)e.AdvertisementData.ValueForKey(CBAdvertisement.DataLocalNameKey)).ToString();
                    }

                    var device = new Device(this, e.Peripheral, _bleCentralManagerDelegate, name, e.RSSI.Int32Value,
                        ParseAdvertisementData(e.AdvertisementData));
                    HandleDiscoveredDevice(device);
                }
                catch (Exception ex)
                {
                    Trace.Message("Adapter: Failed to handle a discovered peripheral: {0}", ex);
                }
            };

            _updatedStateHandler = (sender, e) =>
            {
                try
                {
                    var state = _centralManager.State;
                    Trace.Message("UpdatedState: {0}", state);

                    ReleaseStateWaiters(state);

                    //handle PoweredOff state
                    //notify subscribers about disconnection
                    if (state == CBManagerState.PoweredOff)
                    {
                        foreach (var device in ConnectedDevices)
                        {
                            TryRemoveConnectedDevice(device.Id, out _);
                            device.ClearServices();
                            HandleDisconnectedDevice(false, device);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Trace.Message("Adapter: Failed to handle a state update: {0}", ex);
                }
            };

            _connectedPeripheralHandler = (sender, e) =>
            {
                try
                {
                    Trace.Message("ConnectedPeripheral: {0}", e.Peripheral.Name);

                    // when a peripheral gets connected, add that peripheral to our running list of connected peripherals
                    var guid = ParseDeviceGuid(e.Peripheral);

                    if (_deviceOperationRegistry.TryRemove(guid, out var device))
                    {
                        device.Update(e.Peripheral);
                    }
                    else
                    {
                        Trace.Message("Device not found in operation registry. Creating a new one.");
                        device = new Device(this, e.Peripheral, _bleCentralManagerDelegate);
                    }

                    RegisterConnectedDevice(device);
                    HandleConnectedDevice(device);
                }
                catch (Exception ex)
                {
                    Trace.Message("Adapter: Failed to handle a connected peripheral: {0}", ex);
                }
            };

            _disconnectedPeripheralHandler = (sender, e) =>
            {
                try
                {
                    if (e.Error != null)
                    {
                        Trace.Message("Disconnect error {0} {1} {2}", e.Error.Code, e.Error.Description, e.Error.Domain);
                    }

                    // when a peripheral disconnects, remove it from our running list.
                    var id = ParseDeviceGuid(e.Peripheral);

                    // normal disconnect (requested by user)
                    var isNormalDisconnect = _deviceOperationRegistry.TryRemove(id, out var foundDevice);

                    // check if it is a peripheral disconnection, which would be treated as normal
                    if (e.Error != null && e.Error.Code == 7 && e.Error.Domain == "CBErrorDomain")
                    {
                        isNormalDisconnect = true;
                    }

                    // remove from connected devices
                    if (!TryRemoveConnectedDevice(id, out var connectedDevice))
                    {
                        Trace.Message($"Device with id '{id}' was not found in the connected device registry. Nothing to remove.");
                    }

                    foundDevice = foundDevice ?? connectedDevice ?? new Device(this, e.Peripheral, _bleCentralManagerDelegate);

                    //make sure all cached services are cleared this will also clear characteristics and descriptors implicitly
                    foundDevice.ClearServices();

                    HandleDisconnectedDevice(isNormalDisconnect, foundDevice);
                }
                catch (Exception ex)
                {
                    Trace.Message("Adapter: Failed to handle a disconnected peripheral: {0}", ex);
                }
            };

            _failedToConnectPeripheralHandler = (sender, e) =>
            {
                try
                {
                    var id = ParseDeviceGuid(e.Peripheral);

                    // remove instance from registry
                    _deviceOperationRegistry.TryRemove(id, out var foundDevice);

                    foundDevice = foundDevice ?? new Device(this, e.Peripheral, _bleCentralManagerDelegate);

                    HandleConnectionFail(foundDevice, e.Error?.Description ?? "The connection attempt failed.");
                }
                catch (Exception ex)
                {
                    Trace.Message("Adapter: Failed to handle a failed connection: {0}", ex);
                }
            };

            _bleCentralManagerDelegate.DiscoveredPeripheral += _discoveredPeripheralHandler;
            _bleCentralManagerDelegate.UpdatedState += _updatedStateHandler;
            _bleCentralManagerDelegate.ConnectedPeripheral += _connectedPeripheralHandler;
            _bleCentralManagerDelegate.DisconnectedPeripheral += _disconnectedPeripheralHandler;
            _bleCentralManagerDelegate.FailedToConnectPeripheral += _failedToConnectPeripheralHandler;
        }

        #endregion

        #region Native Functions
        protected async Task StartScanningForDevicesNativeAsync(Guid[] serviceUuids, bool allowDuplicatesKey, CancellationToken scanCancellationToken)
        {
            // Wait for the PoweredOn state. A central manager that has only just been created reports Unknown
            // for a short while even when Bluetooth is on and working, so scanning without this gate fails on
            // the first attempt after launch.
            await WaitForState(CBManagerState.PoweredOn, scanCancellationToken).ConfigureAwait(false);

            Trace.Message("Adapter: Starting a scan for devices.");

            CBUUID[] serviceCbuuids = null;
            if (serviceUuids != null && serviceUuids.Any())
            {
                serviceCbuuids = serviceUuids.Select(u => CBUUID.FromString(u.ToString())).ToArray();
                Trace.Message("Adapter: Scanning for " + serviceCbuuids.First());
            }

            _centralManager.ScanForPeripherals(serviceCbuuids, new PeripheralScanningOptions { AllowDuplicatesKey = allowDuplicatesKey });
        }

        protected void DisconnectDeviceNative(Device device)
        {
            _deviceOperationRegistry[device.Id] = device;
            _centralManager.CancelPeripheralConnection(device.NativeDevice);
        }

        protected void StopScanNative()
        {
            _centralManager.StopScan();
        }

        protected Task ConnectToDeviceNativeAsync(Device device, ConnectParameters connectParameters, CancellationToken cancellationToken)
        {
            if (connectParameters.AutoConnect)
            {
                Trace.Message("Warning: Autoconnect is not supported in iOS");
            }

            _deviceOperationRegistry[device.Id] = device;

            _centralManager.ConnectPeripheral(device.NativeDevice, new PeripheralConnectionOptions());

            cancellationToken.Register(() =>
            {
                Trace.Message("Canceling the connect attempt");

                // The pending-operation entry has to go with it. An abandoned entry made the next unsolicited
                // disconnection for this peripheral look like one the user had asked for, so the consumer was
                // told the link had been closed cleanly when in fact it had been lost.
                _deviceOperationRegistry.TryRemove(device.Id, out _);

                _centralManager.CancelPeripheralConnection(device.NativeDevice);
            });

            return Task.CompletedTask;
        }

        /// <summary>
        /// Retrieves a peripheral CoreBluetooth already knows about, without connecting it.
        /// </summary>
        /// <remarks>
        /// See Apple's guidance on reconnecting to a known peripheral:
        /// https://developer.apple.com/library/archive/documentation/NetworkingInternetWeb/Conceptual/CoreBluetooth_concepts/BestPracticesForInteractingWithARemotePeripheralDevice/BestPracticesForInteractingWithARemotePeripheralDevice.html
        /// The state gate is kept here: retrieving peripherals from a central manager that has not settled
        /// returns nothing, which would look exactly like the device being gone.
        /// </remarks>
        protected async Task<Device> ConnectToKnownDeviceNativeAsync(Guid deviceGuid, ConnectParameters connectParameters, CancellationToken cancellationToken)
        {
            // Wait for the PoweredOn state
            await WaitForState(CBManagerState.PoweredOn, cancellationToken).ConfigureAwait(false);

            //FYI attempted to use tobyte array insetead of string but there was a problem with byte ordering Guid->NSUui
            var uuid = new NSUuid(deviceGuid.ToString());

            Trace.Message($"[Adapter] Attempting connection to {uuid}");

            var peripherals = _centralManager.RetrievePeripheralsWithIdentifiers(uuid);

            // FirstOrDefault rather than SingleOrDefault: CoreBluetooth returning more than one peripheral for
            // an identifier is not something the caller can do anything about, and throwing there turned an
            // odd platform response into an exception with nothing to do with Bluetooth.
            var peripheral = peripherals.FirstOrDefault();

            if (peripheral == null)
            {
                var systemPeripherals = _centralManager.RetrieveConnectedPeripherals([]);

                peripheral = systemPeripherals.FirstOrDefault(p => p.Identifier.Equals(uuid));
            }

            if (peripheral == null)
            {
                return null;
            }

            // Rssi is reported as zero rather than read from CBPeripheral.RSSI, which Apple deprecated because
            // it holds whatever the last ReadRSSI returned - for a peripheral retrieved by identifier that is
            // nothing at all. Android and Windows report zero here for the same reason. Call
            // Device.UpdateRssiAsync once connected for a real reading.
            return new Device(this, peripheral, _bleCentralManagerDelegate, peripheral.Name, 0, new List<AdvertisementRecord>());
        }

        /// <summary>
        /// The peripherals the system currently has connected, whether or not this application connected them.
        /// </summary>
        /// <param name="services">Restricts the result to peripherals offering these services. Null returns all of them.</param>
        public IReadOnlyList<Device> GetSystemConnectedOrPairedDevices(Guid[] services = null)
        {
            CBUUID[] serviceUuids = null;
            if (services != null)
            {
                serviceUuids = services.Select(guid => CBUUID.FromString(guid.ToString())).ToArray();
            }

            var nativeDevices = _centralManager.RetrieveConnectedPeripherals(serviceUuids);

            return nativeDevices.Select(d => new Device(this, d, _bleCentralManagerDelegate)).ToList();
        }

        partial void DisposeNative()
        {
            if (_bleCentralManagerDelegate != null)
            {
                _bleCentralManagerDelegate.DiscoveredPeripheral -= _discoveredPeripheralHandler;
                _bleCentralManagerDelegate.UpdatedState -= _updatedStateHandler;
                _bleCentralManagerDelegate.ConnectedPeripheral -= _connectedPeripheralHandler;
                _bleCentralManagerDelegate.DisconnectedPeripheral -= _disconnectedPeripheralHandler;
                _bleCentralManagerDelegate.FailedToConnectPeripheral -= _failedToConnectPeripheralHandler;
            }

            // Anything still waiting for PoweredOn will never be told now, so cancel it rather than leave it
            // hanging for the lifetime of the process.
            CancelStateWaiters();

            _deviceOperationRegistry.Clear();
        }

        #endregion

        /// <summary>
        /// Completes when the central manager reaches <paramref name="state"/>.
        /// </summary>
        private async Task WaitForState(CBManagerState state, CancellationToken cancellationToken)
        {
            Trace.Message("Adapter: Waiting for state: " + state);

            while (_centralManager.State != state)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var waiter = new TaskCompletionSource<CBManagerState>(TaskCreationOptions.RunContinuationsAsynchronously);

                lock (_stateWaitersLock)
                {
                    _stateWaiters.Add(waiter);
                }

                // Re-checked after registering. Without this the state could change in the window between the
                // loop's test and the registration above, and the waiter would sleep until the next change -
                // which, for an adapter that has just settled on PoweredOn, may never come.
                if (_centralManager.State == state)
                {
                    RemoveStateWaiter(waiter);
                    return;
                }

                try
                {
                    using (cancellationToken.Register(() => waiter.TrySetCanceled(cancellationToken)))
                    {
                        await waiter.Task.ConfigureAwait(false);
                    }
                }
                finally
                {
                    RemoveStateWaiter(waiter);
                }
            }
        }

        private void ReleaseStateWaiters(CBManagerState state)
        {
            TaskCompletionSource<CBManagerState>[] waiters;

            lock (_stateWaitersLock)
            {
                if (_stateWaiters.Count == 0)
                {
                    return;
                }

                waiters = _stateWaiters.ToArray();
                _stateWaiters.Clear();
            }

            foreach (var waiter in waiters)
            {
                waiter.TrySetResult(state);
            }
        }

        private void CancelStateWaiters()
        {
            TaskCompletionSource<CBManagerState>[] waiters;

            lock (_stateWaitersLock)
            {
                waiters = _stateWaiters.ToArray();
                _stateWaiters.Clear();
            }

            foreach (var waiter in waiters)
            {
                waiter.TrySetCanceled();
            }
        }

        private void RemoveStateWaiter(TaskCompletionSource<CBManagerState> waiter)
        {
            lock (_stateWaitersLock)
            {
                _stateWaiters.Remove(waiter);
            }
        }

        private static Guid ParseDeviceGuid(CBPeripheral peripherial)
        {
            return Guid.ParseExact(peripherial.Identifier.AsString(), "d");
        }

        internal static List<AdvertisementRecord> ParseAdvertisementData(NSDictionary advertisementData)
        {
            var records = new List<AdvertisementRecord>();

            foreach (var o in advertisementData.Keys)
            {
                var key = (NSString)o;

                // Guarded per key. A single malformed or unexpected value - a service data dictionary whose
                // values are not NSData, say - used to abandon the whole advertisement, and because this runs
                // on the CoreBluetooth delegate queue the resulting exception killed the process rather than
                // costing one advertisement.
                try
                {
                    ParseAdvertisementRecord(advertisementData, key, records);
                }
                catch (Exception ex)
                {
                    Trace.Message("Parsing Advertisement: Ignoring entry for key {0}, it could not be parsed: {1}", key, ex.Message);
                }
            }

            return records;
        }

        private static void ParseAdvertisementRecord(NSDictionary advertisementData, NSString key, List<AdvertisementRecord> records)
        {
            if (key == CBAdvertisement.DataLocalNameKey)
            {
                records.Add(new AdvertisementRecord(AdvertisementRecordType.CompleteLocalName,
                    NSData.FromString(advertisementData.ObjectForKey(key) as NSString).ToArray()));
            }
            else if (key == CBAdvertisement.DataManufacturerDataKey)
            {
                var arr = ((NSData)advertisementData.ObjectForKey(key)).ToArray();
                records.Add(new AdvertisementRecord(AdvertisementRecordType.ManufacturerSpecificData, arr));
            }
            else if (key == CBAdvertisement.DataServiceUUIDsKey || key == CBAdvertisement.DataOverflowServiceUUIDsKey)
            {
                var array = (NSArray)advertisementData.ObjectForKey(key);

                for (nuint i = 0; i < array.Count; i++)
                {
                    var cbuuid = array.GetItem<CBUUID>(i);

                    switch (cbuuid.Data.Length)
                    {
                        case 16:
                            // 128-bit UUID
                            records.Add(new AdvertisementRecord(AdvertisementRecordType.UuidsComplete128Bit, cbuuid.Data.ToArray()));
                            break;
                        case 8:
                            // 32-bit UUID
                            records.Add(new AdvertisementRecord(AdvertisementRecordType.UuidsComplete32Bit, cbuuid.Data.ToArray()));
                            break;
                        case 2:
                            // 16-bit UUID
                            records.Add(new AdvertisementRecord(AdvertisementRecordType.UuidsComplete16Bit, cbuuid.Data.ToArray()));
                            break;
                        default:
                            // Invalid data length for UUID
                            break;
                    }
                }
            }
            else if (key == CBAdvertisement.DataTxPowerLevelKey)
            {
                //iOS stores TxPower as NSNumber. Get int value of number and convert it into a signed Byte
                //TxPower has a range from -100 to 20 which can fit into a single signed byte (-128 to 127)
                sbyte byteValue = Convert.ToSByte(((NSNumber)advertisementData.ObjectForKey(key)).Int32Value);
                //add our signed byte to a new byte array and return it (same parsed value as android returns)
                byte[] arr = { (byte)byteValue };
                records.Add(new AdvertisementRecord(AdvertisementRecordType.TxPowerLevel, arr));
            }
            else if (key == CBAdvertisement.DataServiceDataKey)
            {
                //Service data from CoreBluetooth is returned as a key/value dictionary with the key being
                //the service uuid (CBUUID) and the value being the NSData (bytes) of the service
                //This is where you'll find eddystone and other service specific data
                NSDictionary serviceDict = (NSDictionary)advertisementData.ObjectForKey(key);
                //There can be multiple services returned in the dictionary, so loop through them
                foreach (CBUUID dKey in serviceDict.Keys)
                {
                    //Get the service key in bytes (from NSData)
                    byte[] keyAsData = dKey.Data.ToArray();

                    //Service UUID's are read backwards (little endian) according to specs,
                    //CoreBluetooth returns the service UUIDs as Big Endian
                    //but to match the raw service data returned from Android we need to reverse it back
                    //Note haven't tested it yet on 128bit service UUID's, but should work
                    Array.Reverse(keyAsData);

                    //The service data under this key can just be turned into an arra
                    var data = (NSData)serviceDict.ObjectForKey(dKey);
                    byte[] valueAsData = data.Length > 0 ? data.ToArray() : [];

                    //Now we append the key and value data and return that so that our parsing matches the raw
                    //byte value returned from the Android library (which matches the raw bytes from the device)
                    byte[] arr = new byte[keyAsData.Length + valueAsData.Length];
                    Buffer.BlockCopy(keyAsData, 0, arr, 0, keyAsData.Length);
                    Buffer.BlockCopy(valueAsData, 0, arr, keyAsData.Length, valueAsData.Length);

                    records.Add(new AdvertisementRecord(AdvertisementRecordType.ServiceData, arr));
                }
            }
            else if (key == CBAdvertisement.IsConnectable)
            {
                // A Boolean value that indicates whether the advertising event type is connectable.
                // The value for this key is an NSNumber object. You can use this value to determine whether a peripheral is connectable at a particular moment.
                records.Add(new AdvertisementRecord(AdvertisementRecordType.IsConnectable,
                                                    [((NSNumber)advertisementData.ObjectForKey(key)).ByteValue]));
            }
            else
            {
                Trace.Message($"Parsing Advertisement: Ignoring Advertisement entry for key {key}, since we don't know how to parse it yet. Maybe you can open a Pull Request and implement it ;)");
            }
        }
    }
}
