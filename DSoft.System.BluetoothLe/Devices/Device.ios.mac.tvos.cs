using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CoreBluetooth;
using Foundation;
using System.BluetoothLe.Utils;

namespace System.BluetoothLe
{
    public partial class Device
    {
        #region Fields
        private readonly IBleCentralManagerDelegate _bleCentralManagerDelegate;

        #endregion

        #region Properties
        internal CBPeripheral NativeDevice { get; private set; }

        #endregion

        #region Constructors
        internal Device(Adapter adapter, CBPeripheral nativeDevice, IBleCentralManagerDelegate bleCentralManagerDelegate)
            : this(adapter, nativeDevice, bleCentralManagerDelegate, nativeDevice.Name, nativeDevice.RSSI?.Int32Value ?? 0, new List<AdvertisementRecord>())
        {

        }

        internal Device(Adapter adapter, CBPeripheral nativeDevice, IBleCentralManagerDelegate bleCentralManagerDelegate, string name, int rssi, List<AdvertisementRecord> advertisementRecords) : this(adapter)
        {
            NativeDevice = nativeDevice;

            _bleCentralManagerDelegate = bleCentralManagerDelegate;

            Id = Guid.ParseExact(NativeDevice.Identifier.AsString(), "d");
            Name = name;

            Rssi = rssi;
            AdvertisementRecords = advertisementRecords;

            // CBPeripheral.UpdatedName is deliberately not subscribed. The peripheral's own name is cached
            // by the OS and can be stale or arrive out of order with the advertisement, and the
            // advertisement's DataLocalNameKey - which is what the adapter passes in above - is the
            // authoritative value. Upstream reached the same conclusion.
        }

        #endregion

        #region Methods

        partial void DisposeNative()
        {
            // Tolerant of a half-constructed or already-torn-down peripheral: disposal must never be the
            // thing that throws.
            var native = NativeDevice;
            NativeDevice = null;

            if (native == null)
            {
                return;
            }

            try
            {
                native.Delegate = null;
            }
            catch (Exception ex)
            {
                Trace.Message("Exception while releasing the native peripheral for {0}: {1}", NameOrId, ex.Message);
            }
        }

        private Task<IReadOnlyList<Service>> GetServicesNativeAsync(CancellationToken cancellationToken)
        {
            return GetServicesInternal(cancellationToken);
        }

        private Task<IReadOnlyList<Service>> GetServicesInternal(CancellationToken cancellationToken, CBUUID id = null)
        {
            var exception = new Exception($"Device {Name} disconnected while fetching services.");

            return TaskBuilder.FromEvent<IReadOnlyList<Service>, EventHandler<NSErrorEventArgs>, EventHandler<CBPeripheralErrorEventArgs>>(
                    execute: () =>
                    {
                        if (NativeDevice.State != CBPeripheralState.Connected)
                            throw exception;

                        if (id != null)
                        {
                            NativeDevice.DiscoverServices(new[] { id });
                        }
                        else
                        {
                            NativeDevice.DiscoverServices();
                        }

                        return Task.CompletedTask;
                    },
                    getCompleteHandler: (complete, reject) => (sender, args) =>
                    {
                        // If args.Error was not null then the Service might be null
                        if (args.Error != null)
                        {
                            reject(new Exception($"Error while discovering services {args.Error.LocalizedDescription}"));
                        }
                        else if (NativeDevice.Services == null)
                        {
                            // No service discovered. 
                            reject(new Exception($"Error while discovering services: returned list is null"));
                        }
                        else
                        {
                            var services = NativeDevice.Services
                                .Select(nativeService => new Service(nativeService, this, _bleCentralManagerDelegate))
                                .ToList<Service>();
                            complete(services);
                        }
                    },
                    subscribeComplete: handler => NativeDevice.DiscoveredService += handler,
                    unsubscribeComplete: handler => NativeDevice.DiscoveredService -= handler,
                    getRejectHandler: reject => ((sender, args) =>
                    {
                        if (args.Peripheral.Identifier == NativeDevice.Identifier)
                            reject(exception);
                    }),
                    subscribeReject: handler => _bleCentralManagerDelegate.DisconnectedPeripheral += handler,
                    unsubscribeReject: handler => _bleCentralManagerDelegate.DisconnectedPeripheral -= handler,
                    token: cancellationToken);
        }

        private Task<bool> UpdateRssiNativeAsync(CancellationToken cancellationToken)
        {
            return TaskBuilder.FromEvent<bool, EventHandler<CBRssiEventArgs>, EventHandler<CBPeripheralErrorEventArgs>>(
                execute: () => { NativeDevice.ReadRSSI(); return Task.CompletedTask; },
                getCompleteHandler: (complete, reject) => (sender, args) =>
                {
                    if (args.Error != null)
                    {
                        reject(new Exception($"Error while reading rssi services {args.Error.LocalizedDescription}"));
                    }
                    else
                    {
                        Rssi = args.Rssi?.Int32Value ?? 0;
                        complete(true);
                    }
                },
                subscribeComplete: handler => NativeDevice.RssiRead += handler,
                unsubscribeComplete: handler => NativeDevice.RssiRead -= handler,
                getRejectHandler: reject => ((sender, args) =>
                {
                    if (args.Peripheral.Identifier == NativeDevice.Identifier)
                        reject(new Exception($"Device {Name} disconnected while reading RSSI."));
                }),
                subscribeReject: handler => _bleCentralManagerDelegate.DisconnectedPeripheral += handler,
                unsubscribeReject: handler => _bleCentralManagerDelegate.DisconnectedPeripheral -= handler,
                token: cancellationToken);
        }

        private DeviceState GetState()
        {
            switch (NativeDevice.State)
            {
                case CBPeripheralState.Connected:
                    return DeviceState.Connected;
                case CBPeripheralState.Connecting:
                    return DeviceState.Connecting;
                case CBPeripheralState.Disconnected:
                    return DeviceState.Disconnected;
                case CBPeripheralState.Disconnecting:
                    return DeviceState.Disconnecting;
                default:
                    return DeviceState.Disconnected;
            }
        }

        private Task<int> RequestMtuNativeAsync(int requestValue, CancellationToken cancellationToken)
        {
            // Apple negotiates the ATT MTU itself; the best we can do is report what it settled on.
            Trace.Message("Request MTU is not supported on Apple platforms; reporting the negotiated write length instead.");
            return Task.FromResult((int)NativeDevice.GetMaximumWriteValueLength(CBCharacteristicWriteType.WithoutResponse));
        }

        private bool UpdateConnectionIntervalNative(ConnectionInterval interval)
        {
            Trace.Message("Cannot update the connection interval on Apple platforms.");
            return false;
        }

        internal void Update(CBPeripheral nativeDevice)
        {
            // The adapter can hand us a fresh peripheral object for the same identifier on a later scan,
            // so take the new one rather than keeping a stale handle.
            NativeDevice = nativeDevice;

            // Only overwrite the RSSI when the platform actually gave us one: outside a connection
            // CBPeripheral.RSSI is null, and treating that as 0 dBm reports a device as being closer
            // than anything real. Read once into a local so this remains a single deprecated call site.
            var nativeRssi = nativeDevice.RSSI;
            if (nativeRssi != null)
            {
                Rssi = nativeRssi.Int32Value;
            }

            // It is maybe not the best idea to update the name based on the CBPeripheral name, because this might be stale.
            //Name = nativeDevice.Name;
        }



        #endregion
    }
}
