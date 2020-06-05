using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Security.Cryptography;
using System.BluetoothLe.Contracts;
using System.BluetoothLe.EventArgs;
using System.BluetoothLe.Extensions;

namespace System.BluetoothLe
{
    public partial class Characteristic
    {
        #region Fields
        /// <summary>
        /// Value of the characteristic to be stored locally after
        /// update notification or read
        /// </summary>
        private byte[] _value;

        #endregion

        #region Properties
        protected Guid NativeGuid => NativeCharacteristic.Uuid;

        protected string NativeUuid => NativeCharacteristic.Uuid.ToString();

        protected byte[] NativeValue => _value ?? new byte[0]; // return empty array if value is equal to null

        protected string NativeName => string.IsNullOrEmpty(NativeCharacteristic.UserDescription) ? KnownCharacteristics.Lookup(Id).Name : NativeCharacteristic.UserDescription;

        protected CharacteristicPropertyType NativeProperties => (CharacteristicPropertyType)(int)NativeCharacteristic.CharacteristicProperties;

        protected GattCharacteristic NativeCharacteristic { get; private set; }

        #endregion

        #region Constructors

        public Characteristic(GattCharacteristic nativeCharacteristic, IService service) : this(service)
        {
            NativeCharacteristic = nativeCharacteristic;
        }

        #endregion

        #region Methods

        protected async Task<IReadOnlyList<IDescriptor>> GetDescriptorsNativeAsync()
        {
            var descriptorsResult = await NativeCharacteristic.GetDescriptorsAsync(BleImplementation.CacheModeGetDescriptors);
            descriptorsResult.ThrowIfError();

            return descriptorsResult.Descriptors?
                .Select(nativeDescriptor => new Descriptor(nativeDescriptor, this))
                .Cast<IDescriptor>()
                .ToList();
        }

        protected async Task<byte[]> ReadNativeAsync()
        {
            var readResult = await NativeCharacteristic.ReadValueAsync(BleImplementation.CacheModeCharacteristicRead);
            return _value = readResult.GetValueOrThrowIfError();
        }

        protected async Task StartUpdatesNativeAsync()
        {
            NativeCharacteristic.ValueChanged -= OnCharacteristicValueChanged;
            NativeCharacteristic.ValueChanged += OnCharacteristicValueChanged;

            var result = await NativeCharacteristic.WriteClientCharacteristicConfigurationDescriptorWithResultAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
            result.ThrowIfError();
        }

        protected async Task StopUpdatesNativeAsync()
        {
            NativeCharacteristic.ValueChanged -= OnCharacteristicValueChanged;

            var result = await NativeCharacteristic.WriteClientCharacteristicConfigurationDescriptorWithResultAsync(GattClientCharacteristicConfigurationDescriptorValue.None);
            result.ThrowIfError();
        }

        protected async Task<bool> WriteNativeAsync(byte[] data, CharacteristicWriteType writeType)
        {
            var result = await NativeCharacteristic.WriteValueWithResultAsync(
                CryptographicBuffer.CreateFromByteArray(data),
                writeType == CharacteristicWriteType.WithResponse ? GattWriteOption.WriteWithResponse : GattWriteOption.WriteWithoutResponse);

            result.ThrowIfError();
            return true;
        }

        /// <summary>
        /// Handler for when the characteristic value is changed. Updates the
        /// stored value
        /// </summary>
        private void OnCharacteristicValueChanged(object sender, GattValueChangedEventArgs e)
        {
            _value = e.CharacteristicValue?.ToArray(); //add value to array
            ValueUpdated?.Invoke(this, new CharacteristicUpdatedEventArgs(this));
        }

        #endregion

    }
}
