using System;
using System.Collections.Generic;
using System.Linq;
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
        private static readonly Guid ClientCharacteristicConfigurationDescriptorId = Guid.Parse("00002902-0000-1000-8000-00805f9b34fb");

        private readonly BluetoothGatt _gatt;
        private readonly IGattCallback _gattCallback;

        #endregion


        #region Properties
        protected Guid NativeGuid => Guid.Parse(NativeCharacteristic.Uuid.ToString());
        protected string NativeUuid => NativeCharacteristic.Uuid.ToString();
        protected byte[] NativeValue => NativeCharacteristic.GetValue() ?? new byte[0];
        protected CharacteristicPropertyType NativeProperties => (CharacteristicPropertyType)(int)NativeCharacteristic.Properties;
        protected BluetoothGattCharacteristic NativeCharacteristic { get; private set; }

        protected string NativeName => KnownCharacteristics.Lookup(Id).Name;

        #endregion

        #region Constructors
        public Characteristic(BluetoothGattCharacteristic nativeCharacteristic, BluetoothGatt gatt, IGattCallback gattCallback, Service service) : this(service)
        {
            NativeCharacteristic = nativeCharacteristic;

            _gatt = gatt;
            _gattCallback = gattCallback;
        }
        #endregion

        #region Methods
        protected Task<IReadOnlyList<Descriptor>> GetDescriptorsNativeAsync()
        {
            return Task.FromResult<IReadOnlyList<Descriptor>>(NativeCharacteristic.Descriptors.Select(item => new Descriptor(item, _gatt, _gattCallback, this)).Cast<Descriptor>().ToList());
        }

        protected async Task<byte[]> ReadNativeAsync()
        {
            return await TaskBuilder.FromEvent<byte[], EventHandler<CharacteristicReadCallbackEventArgs>, EventHandler>(
                execute: ReadInternal,
                getCompleteHandler: (complete, reject) => ((sender, args) =>
                {
                    if (args.Characteristic.Uuid == NativeCharacteristic.Uuid)
                    {
                        complete(args.Characteristic.GetValue());
                    }
                }),
                subscribeComplete: handler => _gattCallback.CharacteristicValueUpdated += handler,
                unsubscribeComplete: handler => _gattCallback.CharacteristicValueUpdated -= handler,
                getRejectHandler: reject => ((sender, args) =>
                {
                    reject(new Exception($"Device '{Service.Device.Id}' disconnected while reading characteristic with {Id}."));
                }),
                subscribeReject: handler => _gattCallback.ConnectionInterrupted += handler,
                unsubscribeReject: handler => _gattCallback.ConnectionInterrupted -= handler);
        }

        void ReadInternal()
        {
            if (!_gatt.ReadCharacteristic(NativeCharacteristic))
            {
                throw new CharacteristicReadException("BluetoothGattCharacteristic.readCharacteristic returned FALSE");
            }
        }

        protected async Task<bool> WriteNativeAsync(byte[] data, CharacteristicWriteType writeType)
        {
            NativeCharacteristic.WriteType = writeType.ToNative();

            return await TaskBuilder.FromEvent<bool, EventHandler<CharacteristicWriteCallbackEventArgs>, EventHandler>(
                execute: () => InternalWrite(data),
                getCompleteHandler: (complete, reject) => ((sender, args) =>
                   {
                       if (args.Characteristic.Uuid == NativeCharacteristic.Uuid)
                       {
                           complete(args.Exception == null);
                       }
                   }),
               subscribeComplete: handler => _gattCallback.CharacteristicValueWritten += handler,
               unsubscribeComplete: handler => _gattCallback.CharacteristicValueWritten -= handler,
               getRejectHandler: reject => ((sender, args) =>
               {
                   reject(new Exception($"Device '{Service.Device.Id}' disconnected while writing characteristic with {Id}."));
               }),
               subscribeReject: handler => _gattCallback.ConnectionInterrupted += handler,
               unsubscribeReject: handler => _gattCallback.ConnectionInterrupted -= handler);
        }

        private void InternalWrite(byte[] data)
        {
            if (!NativeCharacteristic.SetValue(data))
            {
                throw new CharacteristicReadException("Gatt characteristic set value FAILED.");
            }

            Trace.Message("Write {0}", Id);

            if (!_gatt.WriteCharacteristic(NativeCharacteristic))
            {
                throw new CharacteristicReadException("Gatt write characteristic FAILED.");
            }
        }

        protected async Task StartUpdatesNativeAsync()
        {
            // wire up the characteristic value updating on the gattcallback for event forwarding
            _gattCallback.CharacteristicValueUpdated -= OnCharacteristicValueChanged;
            _gattCallback.CharacteristicValueUpdated += OnCharacteristicValueChanged;

            await TaskBuilder.EnqueueOnMainThreadAsync(() =>
            {
                if (!_gatt.SetCharacteristicNotification(NativeCharacteristic, true))
                    throw new CharacteristicReadException("Gatt SetCharacteristicNotification FAILED.");
            });

            // In order to subscribe to notifications on a given characteristic, you must first set the Notifications Enabled bit
            // in its Client Characteristic Configuration Descriptor. See https://developer.bluetooth.org/gatt/descriptors/Pages/DescriptorsHomePage.aspx and
            // https://developer.bluetooth.org/gatt/descriptors/Pages/DescriptorViewer.aspx?u=org.bluetooth.descriptor.gatt.client_characteristic_configuration.xml
            // for details.

            if (NativeCharacteristic.Descriptors.Count > 0)
            {
                var descriptors = await GetDescriptorsAsync();
                var descriptor = descriptors.FirstOrDefault(d => d.Id.Equals(ClientCharacteristicConfigurationDescriptorId)) ??
                                            descriptors.FirstOrDefault(); // fallback just in case manufacturer forgot

                // has to have one of these (either indicate or notify)
                if (descriptor != null && Properties.HasFlag(CharacteristicPropertyType.Indicate))
                {
                    await descriptor.WriteAsync(BluetoothGattDescriptor.EnableIndicationValue.ToArray());
                    Trace.Message("Descriptor set value: INDICATE");
                }

                if (descriptor != null && Properties.HasFlag(CharacteristicPropertyType.Notify))
                {
                    await descriptor.WriteAsync(BluetoothGattDescriptor.EnableNotificationValue.ToArray());
                    Trace.Message("Descriptor set value: NOTIFY");
                }
            }
            else
            {
                Trace.Message("Descriptor set value FAILED: _nativeCharacteristic.Descriptors was empty");
            }

            Trace.Message("Characteristic.StartUpdates, successful!");
        }

        protected async Task StopUpdatesNativeAsync()
        {
            _gattCallback.CharacteristicValueUpdated -= OnCharacteristicValueChanged;

            await TaskBuilder.EnqueueOnMainThreadAsync(() =>
            {
                if (!_gatt.SetCharacteristicNotification(NativeCharacteristic, false))
                    throw new CharacteristicReadException("GATT: SetCharacteristicNotification to false, FAILED.");
            });

            if (NativeCharacteristic.Descriptors.Count > 0)
            {
                var descriptors = await GetDescriptorsAsync();
                var descriptor = descriptors.FirstOrDefault(d => d.Id.Equals(ClientCharacteristicConfigurationDescriptorId)) ??
                                            descriptors.FirstOrDefault(); // fallback just in case manufacturer forgot

                if (descriptor != null && (Properties.HasFlag(CharacteristicPropertyType.Notify) || Properties.HasFlag(CharacteristicPropertyType.Indicate)))
                {
                    await descriptor.WriteAsync(BluetoothGattDescriptor.DisableNotificationValue.ToArray());
                    Trace.Message("Descriptor set value: DISABLE_NOTIFY");
                }
            }
            else
            {
                Trace.Message("StopUpdatesNativeAsync descriptor set value FAILED: _nativeCharacteristic.Descriptors was empty");
            }
        }

        private void OnCharacteristicValueChanged(object sender, CharacteristicReadCallbackEventArgs e)
        {
            if (e.Characteristic.Uuid == NativeCharacteristic.Uuid)
            {
                ValueUpdated?.Invoke(this, new CharacteristicUpdatedEventArgs(this));
            }
        }

        #endregion
    }
}