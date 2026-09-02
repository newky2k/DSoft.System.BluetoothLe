using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Android.Bluetooth;
using System.BluetoothLe.EventArgs;
using System.BluetoothLe.Extensions;
using System.BluetoothLe.Utils;

namespace System.BluetoothLe
{
    public partial class Characteristic
    {
        //https://developer.android.com/samples/BluetoothLeGatt/src/com.example.android.bluetoothlegatt/SampleGattAttributes.html

        #region Fields

        /// <summary>
        /// The client characteristic configuration descriptor, 0x2902, whose value selects notifications or
        /// indications.
        /// </summary>
        private static readonly Guid ClientCharacteristicConfigurationDescriptorId = 0x2902.UuidFromPartial();

        private readonly BluetoothGatt _gatt;
        private readonly IGattCallback _gattCallback;

        #endregion


        #region Properties
        protected Guid NativeGuid => Guid.Parse(NativeCharacteristic.Uuid.ToString());
        protected string NativeUuid => NativeCharacteristic.Uuid.ToString();
        protected byte[] NativeValue => NativeCharacteristic.GetValue() ?? Array.Empty<byte>();
        protected CharacteristicPropertyType NativeProperties => (CharacteristicPropertyType)(int)NativeCharacteristic.Properties;
        protected BluetoothGattCharacteristic NativeCharacteristic { get; private set; }

        protected string NativeName => KnownCharacteristics.Lookup(Id).Name;

        #endregion

        #region Constructors
        internal Characteristic(BluetoothGattCharacteristic nativeCharacteristic, BluetoothGatt gatt, IGattCallback gattCallback, Service service) : this(service)
        {
            NativeCharacteristic = nativeCharacteristic;

            _gatt = gatt;
            _gattCallback = gattCallback;
        }
        #endregion

        #region Methods
        protected Task<IReadOnlyList<Descriptor>> GetDescriptorsNativeAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult<IReadOnlyList<Descriptor>>(
                NativeCharacteristic.Descriptors.Select(item => new Descriptor(item, _gatt, _gattCallback, this)).ToList());
        }

        protected async Task<byte[]> ReadNativeAsync(CancellationToken cancellationToken)
        {
            return await TaskBuilder.FromEvent<byte[], EventHandler<CharacteristicReadCallbackEventArgs>, EventHandler>(
                execute: () => { ReadInternal(); return Task.CompletedTask; },
                getCompleteHandler: (complete, reject) => ((sender, args) =>
                {
                    if (!IsSameCharacteristic(args.Characteristic))
                        return;

                    if (args.Status != GattStatus.Success)
                    {
                        reject(new CharacteristicReadException($"Read characteristic {Id} failed with GATT status {args.Status}.", Id, Service.Id, (int)args.Status));
                        return;
                    }

                    // args.Value was captured inside the callback. Re-reading NativeCharacteristic.GetValue()
                    // here would race with the next operation on the connection, which overwrites the same
                    // Java object, and could hand the caller a different operation's bytes.
                    complete(args.Value ?? Array.Empty<byte>());
                }),
                // Subscribing to CharacteristicValueRead rather than CharacteristicValueUpdated: before 4.0 both
                // OnCharacteristicRead and OnCharacteristicChanged raised the same event, so an unrelated
                // notification arriving mid-read completed the read with the notification's value.
                subscribeComplete: handler => _gattCallback.CharacteristicValueRead += handler,
                unsubscribeComplete: handler => _gattCallback.CharacteristicValueRead -= handler,
                getRejectHandler: reject => ((sender, args) =>
                {
                    reject(new CharacteristicReadException($"Device '{Service.Device.Id}' disconnected while reading characteristic with {Id}.", Id, Service.Id));
                }),
                subscribeReject: handler => _gattCallback.ConnectionInterrupted += handler,
                unsubscribeReject: handler => _gattCallback.ConnectionInterrupted -= handler,
                token: cancellationToken);
        }

        void ReadInternal()
        {
            if (!_gatt.ReadCharacteristic(NativeCharacteristic))
            {
                throw new CharacteristicReadException("BluetoothGattCharacteristic.readCharacteristic returned FALSE", Id, Service.Id);
            }
        }

        protected async Task WriteNativeAsync(byte[] data, CharacteristicWriteType writeType, CancellationToken cancellationToken)
        {
            NativeCharacteristic.WriteType = writeType.ToNative();

            await TaskBuilder.FromEvent<bool, EventHandler<CharacteristicWriteCallbackEventArgs>, EventHandler>(
                execute: () => { InternalWrite(data); return Task.CompletedTask; },
                getCompleteHandler: (complete, reject) => ((sender, args) =>
                   {
                       if (!IsSameCharacteristic(args.Characteristic))
                           return;

                       if (args.Exception != null)
                       {
                           reject(new CharacteristicWriteException($"Write characteristic {Id} failed with GATT status {args.Status}.", Id, Service.Id, (int)args.Status));
                           return;
                       }

                       complete(true);
                   }),
               subscribeComplete: handler => _gattCallback.CharacteristicValueWritten += handler,
               unsubscribeComplete: handler => _gattCallback.CharacteristicValueWritten -= handler,
               getRejectHandler: reject => ((sender, args) =>
               {
                   reject(new CharacteristicWriteException($"Device '{Service.Device.Id}' disconnected while writing characteristic with {Id}.", Id, Service.Id));
               }),
               subscribeReject: handler => _gattCallback.ConnectionInterrupted += handler,
               unsubscribeReject: handler => _gattCallback.ConnectionInterrupted -= handler,
               token: cancellationToken);
        }

        private void InternalWrite(byte[] data)
        {
            if (!NativeCharacteristic.SetValue(data))
            {
                throw new CharacteristicWriteException("Gatt characteristic set value FAILED.", Id, Service.Id);
            }

            Trace.Message("Write {0}", Id);

            if (!_gatt.WriteCharacteristic(NativeCharacteristic))
            {
                throw new CharacteristicWriteException("Gatt write characteristic FAILED.", Id, Service.Id);
            }
        }

        protected async Task StartUpdatesNativeAsync(CharacteristicUpdateMode mode, CancellationToken cancellationToken)
        {
            // wire up the characteristic value updating on the gattcallback for event forwarding
            _gattCallback.CharacteristicValueUpdated -= OnCharacteristicValueChanged;
            _gattCallback.CharacteristicValueUpdated += OnCharacteristicValueChanged;

            await TaskBuilder.EnqueueOnMainThreadAsync(() =>
            {
                if (!_gatt.SetCharacteristicNotification(NativeCharacteristic, true))
                    throw new CharacteristicReadException("Gatt SetCharacteristicNotification FAILED.", Id, Service.Id);

                return Task.CompletedTask;
            }, cancellationToken);

            // In order to subscribe to notifications on a given characteristic, you must first set the Notifications Enabled bit
            // in its Client Characteristic Configuration Descriptor. See https://developer.bluetooth.org/gatt/descriptors/Pages/DescriptorsHomePage.aspx and
            // https://developer.bluetooth.org/gatt/descriptors/Pages/DescriptorViewer.aspx?u=org.bluetooth.descriptor.gatt.client_characteristic_configuration.xml
            // for details.

            var descriptor = await GetClientConfigurationDescriptorAsync(cancellationToken);
            if (descriptor == null)
            {
                return;
            }

            // Exactly one write. Before 4.0 a characteristic advertising both flags was written twice, indicate
            // then notify, on a connection that can only carry one operation at a time; the second overwrote
            // the first, so the indicate write was never anything but wasted traffic.
            var value = mode == CharacteristicUpdateMode.Indicate
                ? BluetoothGattDescriptor.EnableIndicationValue
                : BluetoothGattDescriptor.EnableNotificationValue;

            await descriptor.WriteAsync(value.ToArray(), cancellationToken);
            Trace.Message("Descriptor set value: {0}", mode);

            Trace.Message("Characteristic.StartUpdates, successful!");
        }

        protected async Task StopUpdatesNativeAsync(CancellationToken cancellationToken)
        {
            _gattCallback.CharacteristicValueUpdated -= OnCharacteristicValueChanged;

            await TaskBuilder.EnqueueOnMainThreadAsync(() =>
            {
                if (!_gatt.SetCharacteristicNotification(NativeCharacteristic, false))
                    throw new CharacteristicReadException("GATT: SetCharacteristicNotification to false, FAILED.", Id, Service.Id);

                return Task.CompletedTask;
            }, cancellationToken);

            var descriptor = await GetClientConfigurationDescriptorAsync(cancellationToken);
            if (descriptor == null)
            {
                return;
            }

            await descriptor.WriteAsync(BluetoothGattDescriptor.DisableNotificationValue.ToArray(), cancellationToken);
            Trace.Message("Descriptor set value: DISABLE_NOTIFY");
        }

        /// <summary>
        /// Finds the client characteristic configuration descriptor, or traces and returns
        /// <see langword="null"/> when the peripheral does not expose one.
        /// </summary>
        /// <remarks>
        /// Before 4.0 this fell back to whatever descriptor happened to be first "just in case manufacturer
        /// forgot", which writes a notification bitmask into an unrelated descriptor - a silent corruption of
        /// peripheral state that is worse than a subscription which visibly does not start.
        /// </remarks>
        private async Task<Descriptor> GetClientConfigurationDescriptorAsync(CancellationToken cancellationToken)
        {
            if (NativeCharacteristic.Descriptors.Count == 0)
            {
                Trace.Message("Characteristic {0}: cannot change notification state, the characteristic exposes no descriptors at all.", Id);
                return null;
            }

            var descriptors = await GetDescriptorsAsync(cancellationToken);
            var descriptor = descriptors.FirstOrDefault(d => d.Id.Equals(ClientCharacteristicConfigurationDescriptorId));

            if (descriptor == null)
            {
                Trace.Message("Characteristic {0}: cannot change notification state, no client characteristic configuration descriptor (0x2902) was found among its {1} descriptors.", Id, descriptors.Count);
            }

            return descriptor;
        }

        partial void DetachNotificationsNative()
        {
            _gattCallback.CharacteristicValueUpdated -= OnCharacteristicValueChanged;
        }

        private void OnCharacteristicValueChanged(object sender, CharacteristicReadCallbackEventArgs e)
        {
            if (IsSameCharacteristic(e.Characteristic))
            {
                ValueUpdated?.Invoke(this, new CharacteristicUpdatedEventArgs(this));
            }
        }

        /// <summary>
        /// Decides whether a callback is about this characteristic and not merely one sharing its UUID.
        /// </summary>
        /// <remarks>
        /// A UUID is not unique within a peripheral: the same characteristic UUID may appear in more than one
        /// service, and a service may expose it more than once. Matching on UUID alone let one operation's
        /// callback complete an unrelated operation with the wrong bytes. Reference equality is not usable
        /// instead, because .NET for Android does not guarantee a stable managed wrapper for a Java peer, so
        /// the attribute's own instance id and its owning service's UUID are compared as well.
        /// </remarks>
        private bool IsSameCharacteristic(BluetoothGattCharacteristic other)
        {
            if (other == null || NativeCharacteristic == null)
                return false;

            return SameUuid(other.Uuid, NativeCharacteristic.Uuid)
                && other.InstanceId == NativeCharacteristic.InstanceId
                && SameUuid(other.Service?.Uuid, NativeCharacteristic.Service?.Uuid);
        }

        // Java.Util.UUID does not overload ==, so the operator compares managed wrapper references and only
        // happens to work while the runtime hands back the same peer for the same Java object. Equals crosses
        // to Java's own equals and compares the value, which is what is actually meant here.
        private static bool SameUuid(Java.Util.UUID left, Java.Util.UUID right)
            => left != null && right != null && left.Equals(right);

        #endregion
    }
}
