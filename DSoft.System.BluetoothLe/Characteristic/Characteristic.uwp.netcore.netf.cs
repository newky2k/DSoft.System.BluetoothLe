using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Security.Cryptography;
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

        protected byte[] NativeValue => _value ?? Array.Empty<byte>(); // return empty array if value is equal to null

        protected string NativeName => string.IsNullOrEmpty(NativeCharacteristic.UserDescription) ? KnownCharacteristics.Lookup(Id).Name : NativeCharacteristic.UserDescription;

        protected CharacteristicPropertyType NativeProperties => (CharacteristicPropertyType)(int)NativeCharacteristic.CharacteristicProperties;

        protected GattCharacteristic NativeCharacteristic { get; private set; }

        #endregion

        #region Constructors

        internal Characteristic(GattCharacteristic nativeCharacteristic, Service service) : this(service)
        {
            NativeCharacteristic = nativeCharacteristic;
        }

        #endregion

        #region Methods

        protected async Task<IReadOnlyList<Descriptor>> GetDescriptorsNativeAsync(CancellationToken cancellationToken)
        {
            var descriptorsResult = await NativeCharacteristic.GetDescriptorsAsync(BluetoothLE.CacheModeGetDescriptors).AsTask(cancellationToken);
            descriptorsResult.ThrowIfError();

            // An empty list rather than null: a characteristic with no descriptors is ordinary, and returning
            // null made the shared cache rediscover on every call and forced null checks on every caller.
            return descriptorsResult.Descriptors?
                .Select(nativeDescriptor => new Descriptor(nativeDescriptor, this))
                .ToList() ?? (IReadOnlyList<Descriptor>)Array.Empty<Descriptor>();
        }

        protected async Task<byte[]> ReadNativeAsync(CancellationToken cancellationToken)
        {
            var readResult = await NativeCharacteristic.ReadValueAsync(BluetoothLE.CacheModeCharacteristicRead).AsTask(cancellationToken);
            return _value = readResult.GetValueOrThrowIfError();
        }

        protected async Task StartUpdatesNativeAsync(CharacteristicUpdateMode mode, CancellationToken cancellationToken)
        {
            NativeCharacteristic.ValueChanged -= OnCharacteristicValueChanged;
            NativeCharacteristic.ValueChanged += OnCharacteristicValueChanged;

            // Before 4.0 this always wrote Notify, so subscribing to an indicate-only characteristic failed on
            // Windows while succeeding on Android and Apple.
            var descriptorValue = mode == CharacteristicUpdateMode.Indicate
                ? GattClientCharacteristicConfigurationDescriptorValue.Indicate
                : GattClientCharacteristicConfigurationDescriptorValue.Notify;

            var result = await NativeCharacteristic
                .WriteClientCharacteristicConfigurationDescriptorWithResultAsync(descriptorValue)
                .AsTask(cancellationToken);

            ThrowIfWriteFailed(result);
        }

        protected async Task StopUpdatesNativeAsync(CancellationToken cancellationToken)
        {
            NativeCharacteristic.ValueChanged -= OnCharacteristicValueChanged;

            var result = await NativeCharacteristic
                .WriteClientCharacteristicConfigurationDescriptorWithResultAsync(GattClientCharacteristicConfigurationDescriptorValue.None)
                .AsTask(cancellationToken);

            ThrowIfWriteFailed(result);
        }

        protected async Task WriteNativeAsync(byte[] data, CharacteristicWriteType writeType, CancellationToken cancellationToken)
        {
            var result = await NativeCharacteristic.WriteValueWithResultAsync(
                CryptographicBuffer.CreateFromByteArray(data),
                writeType == CharacteristicWriteType.WithResponse ? GattWriteOption.WriteWithResponse : GattWriteOption.WriteWithoutResponse)
                .AsTask(cancellationToken);

            ThrowIfWriteFailed(result);
        }

        /// <summary>
        /// Turns a failed GATT write into the library's own exception, carrying the protocol error so that a
        /// caller can tell an authentication failure from an unreachable device.
        /// </summary>
        private void ThrowIfWriteFailed(GattWriteResult result)
        {
            if (result.Status == GattCommunicationStatus.Success)
                return;

            var detail = result.ProtocolError.HasValue
                ? $" and protocol error {result.ProtocolError.GetErrorString()}"
                : string.Empty;

            throw new CharacteristicWriteException(
                $"Write characteristic {Id} failed with status {result.Status}{detail}.",
                Id,
                Service.Id,
                result.ProtocolError);
        }

        partial void DetachNotificationsNative()
        {
            NativeCharacteristic.ValueChanged -= OnCharacteristicValueChanged;
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
