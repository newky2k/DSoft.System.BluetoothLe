using System;

namespace System.BluetoothLe
{
    /// <summary>
    /// Thrown when writing to a descriptor fails. Enabling or disabling notifications writes the client
    /// characteristic configuration descriptor, so this is the failure a consumer sees when a subscription
    /// cannot be established.
    /// </summary>
    public class DescriptorWriteException : BleException, IGattStatusException
    {
        /// <summary>The descriptor the write was attempted on, where it is known.</summary>
        public Guid DescriptorId { get; }

        /// <summary>The raw platform status code, where the platform supplied one.</summary>
        public int? NativeStatus { get; }

        /// <inheritdoc/>
        /// <remarks>Separate from <see cref="NativeStatus"/>, which keeps its earlier meaning: on Apple platforms
        /// that is the <c>NSError</c> code whatever its domain, while this is set only for ATT errors.</remarks>
        public int? GattStatus { get; }

        /// <inheritdoc/>
        public GattFailure GattFailure => GattStatusCodes.Classify(GattStatus);

        /// <inheritdoc cref="DescriptorWriteException(string, Guid, int?)"/>
        public DescriptorWriteException(string message) : base(message)
        {
        }

        /// <summary>Creates the exception with the descriptor and platform status attached.</summary>
        public DescriptorWriteException(string message, Guid descriptorId, int? nativeStatus = null) : base(message)
        {
            DescriptorId = descriptorId;
            NativeStatus = nativeStatus;
        }

        /// <summary>
        /// Creates the exception with the platform status and the GATT status attached.
        /// </summary>
        /// <param name="message">The message, unchanged from the earlier constructors.</param>
        /// <param name="descriptorId">The descriptor, as <see cref="DescriptorId"/>.</param>
        /// <param name="nativeStatus">The raw platform status, as <see cref="NativeStatus"/>.</param>
        /// <param name="gattStatus">The GATT status, as <see cref="GattStatus"/>.</param>
        public DescriptorWriteException(string message, Guid descriptorId, int? nativeStatus, int? gattStatus)
            : this(message, descriptorId, nativeStatus)
        {
            GattStatus = gattStatus;
        }
    }
}
