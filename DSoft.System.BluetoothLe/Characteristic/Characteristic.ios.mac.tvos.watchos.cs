using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
                    return Array.Empty<byte>();
                }

                return value.ToArray();
            }
        }

        protected CharacteristicPropertyType NativeProperties => (CharacteristicPropertyType)(int)NativeCharacteristic.Properties;

        protected CBCharacteristic NativeCharacteristic { get; private set; }

        protected string NativeName => KnownCharacteristics.Lookup(Id).Name;

        /// <summary>
        /// Whether this OS version can tell us when the write-without-response buffer has drained.
        /// </summary>
        /// <remarks>
        /// macOS is included deliberately. Upstream's equivalent test lists only iOS, tvOS and Mac Catalyst,
        /// which evaluates false on a macOS build and skips flow control entirely there. Every Apple target
        /// framework in this library already has a minimum OS well above these versions, so in practice the
        /// test is always true; it is kept because the analyser reads it as the availability guard for
        /// <see cref="CBPeripheral.CanSendWriteWithoutResponse"/>.
        /// </remarks>
        private static bool SupportsWriteWithoutResponseFlowControl =>
            OperatingSystem.IsIOSVersionAtLeast(11)
            || OperatingSystem.IsTvOSVersionAtLeast(11)
            || OperatingSystem.IsMacCatalystVersionAtLeast(11)
            || OperatingSystem.IsMacOSVersionAtLeast(10, 13);

        #endregion

        #region Constructors

        internal Characteristic(CBCharacteristic nativeCharacteristic, CBPeripheral parentDevice, Service service, IBleCentralManagerDelegate bleCentralManagerDelegate) : this(service)
        {
            NativeCharacteristic = nativeCharacteristic;

            _parentDevice = parentDevice;
            _bleCentralManagerDelegate = bleCentralManagerDelegate;
        }

        #endregion

        #region Methods

        protected Task<IReadOnlyList<Descriptor>> GetDescriptorsNativeAsync(CancellationToken cancellationToken)
        {
            var exception = new CharacteristicReadException($"Device '{Service.Device.Id}' disconnected while fetching descriptors for characteristic with {Id}.", Id, Service.Id);

            return TaskBuilder.FromEvent<IReadOnlyList<Descriptor>, EventHandler<CBCharacteristicEventArgs>, EventHandler<CBPeripheralErrorEventArgs>>(
                execute: () =>
                {
                    if (_parentDevice.State != CBPeripheralState.Connected)
                        throw exception;

                    _parentDevice.DiscoverDescriptors(NativeCharacteristic);

                    return Task.CompletedTask;
                },
                getCompleteHandler: (complete, reject) => (sender, args) =>
                {
                    if (!IsSameCharacteristic(args.Characteristic))
                        return;

                    if (args.Error != null)
                    {
                        reject(new CharacteristicReadException($"Discover descriptors error: {args.Error.Description}", Id, Service.Id, (int)args.Error.Code));
                    }
                    else
                    {
                        complete(args.Characteristic.Descriptors?
                            .Select(descriptor => new Descriptor(descriptor, _parentDevice, this, _bleCentralManagerDelegate))
                            .ToList() ?? (IReadOnlyList<Descriptor>)Array.Empty<Descriptor>());
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
                unsubscribeReject: handler => _bleCentralManagerDelegate.DisconnectedPeripheral -= handler,
                token: cancellationToken);
        }

        protected Task<byte[]> ReadNativeAsync(CancellationToken cancellationToken)
        {
            var exception = new CharacteristicReadException($"Device '{Service.Device.Id}' disconnected while reading characteristic with {Id}.", Id, Service.Id);

            return TaskBuilder.FromEvent<byte[], EventHandler<CBCharacteristicEventArgs>, EventHandler<CBPeripheralErrorEventArgs>>(
                    execute: () =>
                    {
                        if (_parentDevice.State != CBPeripheralState.Connected)
                            throw exception;

                        _parentDevice.ReadValue(NativeCharacteristic);

                        return Task.CompletedTask;
                    },
                    getCompleteHandler: (complete, reject) => (sender, args) =>
                    {
                        if (!IsSameCharacteristic(args.Characteristic))
                            return;

                        if (args.Error != null)
                        {
                            reject(new CharacteristicReadException($"Read async error: {args.Error.Description}", Id, Service.Id, (int)args.Error.Code));
                        }
                        else
                        {
                            // Taken from the callback's own characteristic rather than from the Value property,
                            // which reads whatever CoreBluetooth last wrote into the shared object and can have
                            // moved on by the time the continuation runs.
                            var value = args.Characteristic.Value?.ToArray() ?? Array.Empty<byte>();
                            Trace.Message($"Read characteristic value: {value.ToHexString()}");
                            complete(value);
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
                    unsubscribeReject: handler => _bleCentralManagerDelegate.DisconnectedPeripheral -= handler,
                    token: cancellationToken);
        }

        protected async Task WriteNativeAsync(byte[] data, CharacteristicWriteType writeType, CancellationToken cancellationToken)
        {
            var exception = new CharacteristicWriteException($"Device {Service.Device.Id} disconnected while writing characteristic with {Id}.", Id, Service.Id);
            var nativeWriteType = writeType.ToNative();

            if (nativeWriteType == CBCharacteristicWriteType.WithResponse)
            {
                await TaskBuilder.FromEvent<bool, EventHandler<CBCharacteristicEventArgs>, EventHandler<CBPeripheralErrorEventArgs>>(
                    execute: () =>
                    {
                        if (_parentDevice.State != CBPeripheralState.Connected)
                            throw exception;

                        // The write now happens inside the delegate. Before 4.0 it was issued after the task was
                        // constructed and outside it, so the connected-state precheck above guarded nothing and
                        // a write was posted to a disconnected peripheral anyway.
                        _parentDevice.WriteValue(NSData.FromArray(data), NativeCharacteristic, nativeWriteType);

                        return Task.CompletedTask;
                    },
                    getCompleteHandler: (complete, reject) => (sender, args) =>
                    {
                        if (!IsSameCharacteristic(args.Characteristic))
                            return;

                        if (args.Error != null)
                            reject(new CharacteristicWriteException($"Write async error: {args.Error.Description}", Id, Service.Id, (int)args.Error.Code));
                        else
                            complete(true);
                    },
                    subscribeComplete: handler => _parentDevice.WroteCharacteristicValue += handler,
                    unsubscribeComplete: handler => _parentDevice.WroteCharacteristicValue -= handler,
                    getRejectHandler: reject => ((sender, args) =>
                    {
                        if (args.Peripheral.Identifier == _parentDevice.Identifier)
                            reject(exception);
                    }),
                    subscribeReject: handler => _bleCentralManagerDelegate.DisconnectedPeripheral += handler,
                    unsubscribeReject: handler => _bleCentralManagerDelegate.DisconnectedPeripheral -= handler,
                    token: cancellationToken);

                return;
            }

            // A write without response is never acknowledged, so there is no completion event to wait on. What
            // there is instead is flow control: CoreBluetooth silently drops the write when its buffer is full,
            // which is how a burst of writes loses packets with no error anywhere. Waiting for the buffer to
            // drain is the only way to write reliably.
            if (SupportsWriteWithoutResponseFlowControl && !_parentDevice.CanSendWriteWithoutResponse)
            {
                await WaitUntilReadyToSendWriteWithoutResponseAsync(exception, cancellationToken);
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (_parentDevice.State != CBPeripheralState.Connected)
                throw exception;

            _parentDevice.WriteValue(NSData.FromArray(data), NativeCharacteristic, nativeWriteType);
        }

        /// <summary>
        /// Completes once CoreBluetooth says the peripheral can accept another unacknowledged write.
        /// </summary>
        private async Task WaitUntilReadyToSendWriteWithoutResponseAsync(Exception disconnectedException, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            void OnReady(object sender, System.EventArgs e) => tcs.TrySetResult(true);
            void OnDisconnected(object sender, CBPeripheralErrorEventArgs e)
            {
                if (e.Peripheral.Identifier == _parentDevice.Identifier)
                    tcs.TrySetException(disconnectedException);
            }

            _parentDevice.IsReadyToSendWriteWithoutResponse += OnReady;
            _bleCentralManagerDelegate.DisconnectedPeripheral += OnDisconnected;

            try
            {
                // Re-checked after subscribing. The buffer can drain between the caller's test and the handler
                // being attached, and CoreBluetooth raises the event once and only on the transition, so a wait
                // started after that transition would never complete.
                if (_parentDevice.CanSendWriteWithoutResponse)
                    return;

                using (cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken), false))
                {
                    await tcs.Task;
                }
            }
            finally
            {
                _parentDevice.IsReadyToSendWriteWithoutResponse -= OnReady;
                _bleCentralManagerDelegate.DisconnectedPeripheral -= OnDisconnected;
            }
        }

        protected Task StartUpdatesNativeAsync(CharacteristicUpdateMode mode, CancellationToken cancellationToken)
        {
            var exception = new CharacteristicReadException($"Device {Service.Device.Id} disconnected while starting updates for characteristic with {Id}.", Id, Service.Id);

            // CoreBluetooth has no equivalent of writing the configuration descriptor by hand: setNotifyValue
            // chooses between notification and indication from the characteristic's own properties. The
            // requested mode is therefore advisory here, and is traced rather than silently discarded.
            if (mode == CharacteristicUpdateMode.Indicate && Properties.HasFlag(CharacteristicPropertyType.Notify))
            {
                Trace.Message("Characteristic {0}: indications were requested, but CoreBluetooth selects the mechanism itself and will use notifications for a characteristic that advertises both.", Id);
            }

            _parentDevice.UpdatedCharacterteristicValue -= UpdatedNotify;
            _parentDevice.UpdatedCharacterteristicValue += UpdatedNotify;

            //https://developer.apple.com/reference/corebluetooth/cbperipheral/1518949-setnotifyvalue
            return TaskBuilder.FromEvent<bool, EventHandler<CBCharacteristicEventArgs>, EventHandler<CBPeripheralErrorEventArgs>>(
                  execute: () =>
                  {
                      if (_parentDevice.State != CBPeripheralState.Connected)
                          throw exception;

                      _parentDevice.SetNotifyValue(true, NativeCharacteristic);

                      return Task.CompletedTask;
                  },
                  getCompleteHandler: (complete, reject) => (sender, args) =>
                  {
                      if (!IsSameCharacteristic(args.Characteristic))
                          return;

                      if (args.Error != null)
                      {
                          reject(new CharacteristicReadException($"Start Notifications: Error {args.Error.Description}", Id, Service.Id, (int)args.Error.Code));
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
                          reject(exception);
                  }),
                  subscribeReject: handler => _bleCentralManagerDelegate.DisconnectedPeripheral += handler,
                  unsubscribeReject: handler => _bleCentralManagerDelegate.DisconnectedPeripheral -= handler,
                  token: cancellationToken);
        }

        protected Task StopUpdatesNativeAsync(CancellationToken cancellationToken)
        {
            var exception = new CharacteristicReadException($"Device {Service.Device.Id} disconnected while stopping updates for characteristic with {Id}.", Id, Service.Id);

            _parentDevice.UpdatedCharacterteristicValue -= UpdatedNotify;

            return TaskBuilder.FromEvent<bool, EventHandler<CBCharacteristicEventArgs>, EventHandler<CBPeripheralErrorEventArgs>>(
                execute: () =>
                {
                    if (_parentDevice.State != CBPeripheralState.Connected)
                        throw exception;

                    _parentDevice.SetNotifyValue(false, NativeCharacteristic);

                    return Task.CompletedTask;
                },
                getCompleteHandler: (complete, reject) => (sender, args) =>
                {
                    if (!IsSameCharacteristic(args.Characteristic))
                        return;

                    if (args.Error != null)
                    {
                        reject(new CharacteristicReadException($"Stop Notifications: Error {args.Error.Description}", Id, Service.Id, (int)args.Error.Code));
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
                unsubscribeReject: handler => _bleCentralManagerDelegate.DisconnectedPeripheral -= handler,
                token: cancellationToken);
        }

        partial void DetachNotificationsNative()
        {
            _parentDevice.UpdatedCharacterteristicValue -= UpdatedNotify;
        }

        private void UpdatedNotify(object sender, CBCharacteristicEventArgs e)
        {
            if (IsSameCharacteristic(e.Characteristic))
            {
                ValueUpdated?.Invoke(this, new CharacteristicUpdatedEventArgs(this));
            }
        }

        /// <summary>
        /// Decides whether a delegate callback is about this characteristic and not merely one sharing its UUID.
        /// </summary>
        /// <remarks>
        /// A UUID is not unique within a peripheral, so matching on it alone let one operation's callback
        /// complete another's with the wrong bytes. CoreBluetooth hands back the very object it holds for the
        /// attribute, so comparing the native handle identifies it exactly.
        /// </remarks>
        private bool IsSameCharacteristic(CBCharacteristic other)
            => other != null && NativeCharacteristic != null && other.Handle == NativeCharacteristic.Handle;

        #endregion

    }
}
