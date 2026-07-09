using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CoreBluetooth;
using Foundation;
using System.BluetoothLe.EventArgs;
using System.BluetoothLe.Extensions;
using System.BluetoothLe.Utils;

namespace System.BluetoothLe
{
    public partial class Characteristic
    {
        #region Fields
        private readonly CBPeripheral _parentDevice;
        private readonly IBleCentralManagerDelegate _bleCentralManagerDelegate;

        #endregion

        #region Properties

        protected Guid NativeGuid => NativeCharacteristic.UUID.GuidFromUuid();

        protected string NativeUuid => NativeCharacteristic.UUID.ToString();

        protected byte[] NativeValue
        {
            get
            {
                var value = NativeCharacteristic.Value;
                if (value == null || value.Length == 0)
                {
                    return new byte[0];
                }

                return value.ToArray();
            }
        }

        protected CharacteristicPropertyType NativeProperties => (CharacteristicPropertyType)(int)NativeCharacteristic.Properties;

        protected CBCharacteristic NativeCharacteristic { get; private set; }

        protected string NativeName => KnownCharacteristics.Lookup(Id).Name;

        #endregion

        #region Constructors

        public Characteristic(CBCharacteristic nativeCharacteristic, CBPeripheral parentDevice, Service service, IBleCentralManagerDelegate bleCentralManagerDelegate)  : this(service)
        {
            NativeCharacteristic = nativeCharacteristic;

            _parentDevice = parentDevice;
            _bleCentralManagerDelegate = bleCentralManagerDelegate;
        }

        #endregion

        #region Methods

        protected Task<IReadOnlyList<Descriptor>> GetDescriptorsNativeAsync()
        {
            var exception = new Exception($"Device '{Service.Device.Id}' disconnected while fetching descriptors for characteristic with {Id}.");

            return TaskBuilder.FromEvent<IReadOnlyList<Descriptor>, EventHandler<CBCharacteristicEventArgs>, EventHandler<CBPeripheralErrorEventArgs>>(
                execute: () =>
                {
                    if (_parentDevice.State != CBPeripheralState.Connected)
                        throw exception;

                    _parentDevice.DiscoverDescriptors(NativeCharacteristic);
                },
                getCompleteHandler: (complete, reject) => (sender, args) =>
                {
                    if (args.Characteristic.UUID != NativeCharacteristic.UUID)
                        return;

                    if (args.Error != null)
                    {
                        reject(new Exception($"Discover descriptors error: {args.Error.Description}"));
                    }
                    else
                    {
                        complete(args.Characteristic.Descriptors.Select(descriptor => new Descriptor(descriptor, _parentDevice, this, _bleCentralManagerDelegate)).Cast<Descriptor>().ToList());
                    }
                },
                subscribeComplete: handler => _parentDevice.DiscoveredDescriptor += handler,
                unsubscribeComplete: handler => _parentDevice.DiscoveredDescriptor -= handler,
                getRejectHandler: reject => ((sender, args) =>
                {
                    if (args.Peripheral.Identifier == _parentDevice.Identifier)
                        reject(exception);
                }),
                subscribeReject: handler => _bleCentralManagerDelegate.DisconnectedPeripheral += handler,
                unsubscribeReject: handler => _bleCentralManagerDelegate.DisconnectedPeripheral -= handler);
        }

        protected Task<byte[]> ReadNativeAsync()
        {
            var exception = new Exception($"Device '{Service.Device.Id}' disconnected while reading characteristic with {Id}.");

            return TaskBuilder.FromEvent<byte[], EventHandler<CBCharacteristicEventArgs>, EventHandler<CBPeripheralErrorEventArgs>>(
                    execute: () =>
                    {
                        if (_parentDevice.State != CBPeripheralState.Connected)
                            throw exception;

                        _parentDevice.ReadValue(NativeCharacteristic);
                    },
                    getCompleteHandler: (complete, reject) => (sender, args) =>
                    {
                        if (args.Characteristic.UUID != NativeCharacteristic.UUID)
                            return;

                        if (args.Error != null)
                        {
                            reject(new CharacteristicReadException($"Read async error: {args.Error.Description}"));
                        }
                        else
                        {
                            Trace.Message($"Read characterteristic value: {Value?.ToHexString()}");
                            complete(Value);
                        }
                    },
                    subscribeComplete: handler => _parentDevice.UpdatedCharacterteristicValue += handler,
                    unsubscribeComplete: handler => _parentDevice.UpdatedCharacterteristicValue -= handler,
                    getRejectHandler: reject => ((sender, args) =>
                    {
                        if (args.Peripheral.Identifier == _parentDevice.Identifier)
                            reject(exception);
                    }),
                    subscribeReject: handler => _bleCentralManagerDelegate.DisconnectedPeripheral += handler,
                    unsubscribeReject: handler => _bleCentralManagerDelegate.DisconnectedPeripheral -= handler);
        }

        protected Task<bool> WriteNativeAsync(byte[] data, CharacteristicWriteType writeType)
        {
            var exception = new Exception($"Device {Service.Device.Id} disconnected while writing characteristic with {Id}.");

            Task<bool> task;
            if (writeType.ToNative() == CBCharacteristicWriteType.WithResponse)
            {
                task = TaskBuilder.FromEvent<bool, EventHandler<CBCharacteristicEventArgs>, EventHandler<CBPeripheralErrorEventArgs>>(
                    execute: () => 
                    {
                        if (_parentDevice.State != CBPeripheralState.Connected)
                            throw exception;
                    },
                    getCompleteHandler: (complete, reject) => (sender, args) =>
                    {
                        if (args.Characteristic.UUID != NativeCharacteristic.UUID)
                            return;

                        complete(args.Error == null);
                    },
                    subscribeComplete: handler => _parentDevice.WroteCharacteristicValue += handler,
                    unsubscribeComplete: handler => _parentDevice.WroteCharacteristicValue -= handler,
                    getRejectHandler: reject => ((sender, args) =>
                    {
                        if (args.Peripheral.Identifier == _parentDevice.Identifier)
                            reject(exception);
                    }),
                    subscribeReject: handler => _bleCentralManagerDelegate.DisconnectedPeripheral += handler,
                    unsubscribeReject: handler => _bleCentralManagerDelegate.DisconnectedPeripheral -= handler);
            }
            else
            {
                task = Task.FromResult(true);
            }

            var nsdata = NSData.FromArray(data);
            _parentDevice.WriteValue(nsdata, NativeCharacteristic, writeType.ToNative());

            return task;
        }

        protected Task StartUpdatesNativeAsync()
        {
            var exception = new Exception($"Device {Service.Device.Id} disconnected while starting updates for characteristic with {Id}.");

            _parentDevice.UpdatedCharacterteristicValue -= UpdatedNotify;
            _parentDevice.UpdatedCharacterteristicValue += UpdatedNotify;

            //https://developer.apple.com/reference/corebluetooth/cbperipheral/1518949-setnotifyvalue
            return TaskBuilder.FromEvent<bool, EventHandler<CBCharacteristicEventArgs>, EventHandler<CBPeripheralErrorEventArgs>>(
                  execute: () =>
                  {
                      if (_parentDevice.State != CBPeripheralState.Connected)
                          throw exception;

                      _parentDevice.SetNotifyValue(true, NativeCharacteristic);
                  },
                  getCompleteHandler: (complete, reject) => (sender, args) =>
                  {
                      if (args.Characteristic.UUID != NativeCharacteristic.UUID)
                          return;

                      if (args.Error != null)
                      {
                          reject(new Exception($"Start Notifications: Error {args.Error.Description}"));
                      }
                      else
                      {
                          Trace.Message($"StartUpdates IsNotifying: {args.Characteristic.IsNotifying}");
                          complete(args.Characteristic.IsNotifying);
                      }
                  },
                  subscribeComplete: handler => _parentDevice.UpdatedNotificationState += handler,
                  unsubscribeComplete: handler => _parentDevice.UpdatedNotificationState -= handler,
                  getRejectHandler: reject => ((sender, args) =>
                  {
                      if (args.Peripheral.Identifier == _parentDevice.Identifier)
                          reject(new Exception($"Device {Service.Device.Id} disconnected while starting updates for characteristic with {Id}."));
                  }),
                  subscribeReject: handler => _bleCentralManagerDelegate.DisconnectedPeripheral += handler,
                  unsubscribeReject: handler => _bleCentralManagerDelegate.DisconnectedPeripheral -= handler);
        }

        protected Task StopUpdatesNativeAsync()
        {
            var exception = new Exception($"Device {Service.Device.Id} disconnected while stopping updates for characteristic with {Id}.");

            _parentDevice.UpdatedCharacterteristicValue -= UpdatedNotify;

            return TaskBuilder.FromEvent<bool, EventHandler<CBCharacteristicEventArgs>, EventHandler<CBPeripheralErrorEventArgs>>(
                execute: () =>
                {
                    if (_parentDevice.State != CBPeripheralState.Connected)
                        throw exception;

                    _parentDevice.SetNotifyValue(false, NativeCharacteristic);
                },
                getCompleteHandler: (complete, reject) => (sender, args) =>
                {
                    if (args.Characteristic.UUID != NativeCharacteristic.UUID)
                        return;

                    if (args.Error != null)
                    {
                        reject(new Exception($"Stop Notifications: Error {args.Error.Description}"));
                    }
                    else
                    {
                        Trace.Message($"StopUpdates IsNotifying: {args.Characteristic.IsNotifying}");
                        complete(args.Characteristic.IsNotifying);
                    }
                },
                subscribeComplete: handler => _parentDevice.UpdatedNotificationState += handler,
                unsubscribeComplete: handler => _parentDevice.UpdatedNotificationState -= handler,
                getRejectHandler: reject => ((sender, args) =>
                {
                    if (args.Peripheral.Identifier == _parentDevice.Identifier)
                        reject(exception);
                }),
                subscribeReject: handler => _bleCentralManagerDelegate.DisconnectedPeripheral += handler,
                unsubscribeReject: handler => _bleCentralManagerDelegate.DisconnectedPeripheral -= handler);
        }

        private void UpdatedNotify(object sender, CBCharacteristicEventArgs e)
        {
            if (e.Characteristic.UUID == NativeCharacteristic.UUID)
            {
                ValueUpdated?.Invoke(this, new CharacteristicUpdatedEventArgs(this));
            }
        }

        #endregion

    }
}