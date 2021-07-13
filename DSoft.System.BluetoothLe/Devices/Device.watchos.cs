﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CoreBluetooth;
using Foundation;
using System.BluetoothLe;
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
            : this(adapter, nativeDevice, bleCentralManagerDelegate, nativeDevice.Name, 0,
                new List<AdvertisementRecord>())
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

            // TODO figure out if this is in any way required,
            // https://github.com/xabre/xamarin-bluetooth-le/issues/81
            NativeDevice.UpdatedName += OnNameUpdated;
        }

        #endregion

        #region Methods

        public virtual void Dispose()
        {
            Adapter?.DisconnectDeviceAsync(this);

            NativeDevice.UpdatedName -= OnNameUpdated;
            NativeDevice.Delegate = null;
            NativeDevice = null;
        }

        private void OnNameUpdated(object sender, System.EventArgs e)
        {
            Name = ((CBPeripheral)sender).Name;
            Trace.Message("Device changed name: {0}", Name);
        }

        private Task<IReadOnlyList<Service>> GetServicesNativeAsync()
        {
            return GetServicesInternal();
        }

        private async Task<Service> GetServiceNativeAsync(Guid id)
        {
            var cbuuid = CBUUID.FromString(id.ToString());
            var nativeService = NativeDevice.Services?.FirstOrDefault(service => service.UUID.Equals(cbuuid));
            if (nativeService != null)
            {
                return new Service(nativeService, this, _bleCentralManagerDelegate);
            }

            var services = await GetServicesInternal(cbuuid);
            return services?.FirstOrDefault();
        }

        private Task<IReadOnlyList<Service>> GetServicesInternal(CBUUID id = null)
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
                                .Cast<Service>().ToList();
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
                    unsubscribeReject: handler => _bleCentralManagerDelegate.DisconnectedPeripheral -= handler);
        }

        private Task<bool> UpdateRssiNativeAsync()
        {
            return TaskBuilder.FromEvent<bool, EventHandler<CBRssiEventArgs>, EventHandler<CBPeripheralErrorEventArgs>>(
                execute: () => NativeDevice.ReadRSSI(),
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
                unsubscribeReject: handler => _bleCentralManagerDelegate.DisconnectedPeripheral -= handler);
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
                    return DeviceState.Disconnected;
                default:
                    return DeviceState.Disconnected;
            }
        }

        internal void Update(CBPeripheral nativeDevice)
        {
            nativeDevice.ReadRSSI();
            //Rssi = nativeDevice.RSSI?.Int32Value ?? 0;
            //It's maybe not the best idea to updated the name based on CBPeripherial name because this might be stale.
            //Name = nativeDevice.Name; 
        }

        private async Task<int> RequestMtuNativeAsync(int requestValue)
        {
            Trace.Message($"Request MTU is not supported on iOS.");
            return await Task.FromResult((int)NativeDevice.GetMaximumWriteValueLength(CBCharacteristicWriteType.WithoutResponse));
        }

        private bool UpdateConnectionIntervalNative(ConnectionInterval interval)
        {
            Trace.Message("Cannot update connection inteval on iOS.");
            return false;
        }

        #endregion
    }
}
