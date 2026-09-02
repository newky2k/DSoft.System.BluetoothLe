using System;

namespace System.BluetoothLe
{
    /// <summary>
    /// Thrown when a scan finishes without discovering the device that was being looked for.
    /// </summary>
    public class DeviceDiscoverException : BleException
    {
        /// <summary>The device that was being looked for, when the caller named one.</summary>
        public Guid? DeviceId { get; }

        /// <inheritdoc cref="DeviceDiscoverException(Guid)"/>
        public DeviceDiscoverException() : base("Could not find the specific device.")
        {
        }

        /// <summary>Creates the exception naming the device that was not found.</summary>
        public DeviceDiscoverException(Guid deviceId) : base($"Could not find device {deviceId}.")
        {
            DeviceId = deviceId;
        }
    }
}
