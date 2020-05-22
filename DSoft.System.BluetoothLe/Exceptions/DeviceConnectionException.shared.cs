using System;

namespace System.BluetoothLe.Exceptions
{
    public class DeviceConnectionException : Exception
    {
        public Guid DeviceId { get; }
        public string DeviceName { get; }

        // TODO: maybe pass IDevice instead (after Connect refactoring)
        public DeviceConnectionException(Guid deviceId, string deviceName, string message) : base(message)
        {
            DeviceId = deviceId;
            DeviceName = deviceName;
        }
    }
}