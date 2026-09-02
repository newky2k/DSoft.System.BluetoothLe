using System;

namespace System.BluetoothLe
{
    /// <summary>
    /// Thrown when reading a descriptor fails.
    /// </summary>
    public class DescriptorReadException : BleException
    {
        /// <summary>The descriptor the read was attempted on, where it is known.</summary>
        public Guid DescriptorId { get; }

        /// <summary>The raw platform status code, where the platform supplied one.</summary>
        public int? NativeStatus { get; }

        /// <inheritdoc cref="DescriptorReadException(string, Guid, int?)"/>
        public DescriptorReadException(string message) : base(message)
        {
        }

        /// <summary>Creates the exception with the descriptor and platform status attached.</summary>
        public DescriptorReadException(string message, Guid descriptorId, int? nativeStatus = null) : base(message)
        {
            DescriptorId = descriptorId;
            NativeStatus = nativeStatus;
        }
    }
}
