using System;

namespace System.BluetoothLe
{
    /// <summary>
    /// Thrown when a device is asked for by identifier and the platform has no record of it.
    /// </summary>
    /// <remarks>
    /// This type moved out of the <c>System.BluetoothLe.Exceptions</c> namespace in 4.0, so that it sits
    /// beside the rest of the public surface and a consumer needs one using directive rather than two.
    /// </remarks>
    public class DeviceNotFoundException : BleException
    {
        /// <summary>The device that could not be found.</summary>
        public Guid DeviceId { get; }

        /// <summary>Creates the exception naming the device that could not be found.</summary>
        public DeviceNotFoundException(Guid deviceId) : base($"Device with Id: {deviceId} not found.")
        {
            DeviceId = deviceId;
        }
    }
}
