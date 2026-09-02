using System;

namespace System.BluetoothLe
{
    /// <summary>
    /// Thrown when writing to a characteristic fails.
    /// </summary>
    /// <remarks>
    /// Before 4.0 a failed write reported itself inconsistently: Apple and Windows returned <c>false</c> from
    /// <c>WriteAsync</c> with no reason attached, while Android threw <see cref="CharacteristicReadException"/>
    /// from the write path. Both are now this type on every platform.
    /// </remarks>
    public class CharacteristicWriteException : BleException
    {
        /// <summary>The characteristic the write was attempted on, where it is known.</summary>
        public Guid CharacteristicId { get; }

        /// <summary>The service owning <see cref="CharacteristicId"/>, where it is known.</summary>
        public Guid ServiceId { get; }

        /// <summary>
        /// The raw platform status code, where the platform supplied one. This is a GATT status on Android and
        /// Windows; it is deliberately untyped because the three platforms do not share a status enumeration.
        /// </summary>
        public int? NativeStatus { get; }

        /// <inheritdoc cref="CharacteristicWriteException(string, Guid, Guid, int?)"/>
        public CharacteristicWriteException(string message) : base(message)
        {
        }

        /// <summary>
        /// Creates the exception with the identifying detail a consumer needs to work out which of several
        /// concurrent writes failed.
        /// </summary>
        public CharacteristicWriteException(string message, Guid characteristicId, Guid serviceId, int? nativeStatus = null)
            : base(message)
        {
            CharacteristicId = characteristicId;
            ServiceId = serviceId;
            NativeStatus = nativeStatus;
        }
    }
}
