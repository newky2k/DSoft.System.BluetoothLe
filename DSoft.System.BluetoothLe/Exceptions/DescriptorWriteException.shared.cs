using System;

namespace System.BluetoothLe
{
    /// <summary>
    /// Thrown when writing to a descriptor fails. Enabling or disabling notifications writes the client
    /// characteristic configuration descriptor, so this is the failure a consumer sees when a subscription
    /// cannot be established.
    /// </summary>
    public class DescriptorWriteException : BleException
    {
        /// <summary>The descriptor the write was attempted on, where it is known.</summary>
        public Guid DescriptorId { get; }

        /// <summary>The raw platform status code, where the platform supplied one.</summary>
        public int? NativeStatus { get; }

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
    }
}
