using System;

namespace Plugin.BLE.Exceptions
{
    public class DeviceDiscoverException : Exception
    {
        public DeviceDiscoverException() : base("Could not find the specific device.")
        {
        }
    }
}