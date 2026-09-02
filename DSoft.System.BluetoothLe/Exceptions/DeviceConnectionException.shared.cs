using System;

namespace System.BluetoothLe
{
    /// <summary>
    /// Thrown when a connection attempt fails or an established connection is lost unexpectedly.
    /// </summary>
    public class DeviceConnectionException : BleException
    {
        /// <summary>The device the connection attempt concerned.</summary>
        public Guid DeviceId { get; }

        /// <summary>The device's advertised name at the time of the failure, where it was known.</summary>
        public string DeviceName { get; }

        /// <summary>Creates the exception for a given device.</summary>
        public DeviceConnectionException(Guid deviceId, string deviceName, string message) : base(message)
        {
            DeviceId = deviceId;
            DeviceName = deviceName;
        }
    }
}
