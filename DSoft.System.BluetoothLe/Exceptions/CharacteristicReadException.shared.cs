using System;

namespace System.BluetoothLe
{
    /// <summary>
    /// Thrown when reading a characteristic, or enabling notifications on one, fails.
    /// </summary>
    public class CharacteristicReadException : BleException, IGattStatusException
    {
        /// <summary>The characteristic the operation was attempted on, where it is known.</summary>
        public Guid CharacteristicId { get; }

        /// <summary>The service owning <see cref="CharacteristicId"/>, where it is known.</summary>
        public Guid ServiceId { get; }

        /// <summary>
        /// The raw platform status code, where the platform supplied one. This is a GATT status on Android and
        /// Windows; it is deliberately untyped because the three platforms do not share a status enumeration.
        /// </summary>
        public int? NativeStatus { get; }

        /// <inheritdoc/>
        /// <remarks>Separate from <see cref="NativeStatus"/>, which keeps its earlier meaning: on Apple platforms
        /// that is the <c>NSError</c> code whatever its domain, while this is set only for ATT errors.</remarks>
        public int? GattStatus { get; }

        /// <inheritdoc/>
        public GattFailure GattFailure => GattStatusCodes.Classify(GattStatus);

        /// <inheritdoc cref="CharacteristicReadException(string, Guid, Guid, int?)"/>
        public CharacteristicReadException(string message) : base(message)
        {
        }

        /// <summary>
        /// Creates the exception with the identifying detail a consumer needs to work out which of several
        /// concurrent reads failed.
        /// </summary>
        public CharacteristicReadException(string message, Guid characteristicId, Guid serviceId, int? nativeStatus = null)
            : base(message)
        {
            CharacteristicId = characteristicId;
            ServiceId = serviceId;
            NativeStatus = nativeStatus;
        }

        /// <summary>
        /// Creates the exception with the platform status and the GATT status attached.
        /// </summary>
        /// <param name="message">The message, unchanged from the earlier constructors.</param>
        /// <param name="characteristicId">The characteristic, as <see cref="CharacteristicId"/>.</param>
        /// <param name="serviceId">The owning service, as <see cref="ServiceId"/>.</param>
        /// <param name="nativeStatus">The raw platform status, as <see cref="NativeStatus"/>.</param>
        /// <param name="gattStatus">The GATT status, as <see cref="GattStatus"/>.</param>
        public CharacteristicReadException(string message, Guid characteristicId, Guid serviceId, int? nativeStatus, int? gattStatus)
            : this(message, characteristicId, serviceId, nativeStatus)
        {
            GattStatus = gattStatus;
        }
    }
}
