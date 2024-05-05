using System;
using System.Collections.Generic;
using System.Text;

namespace System.BluetoothLe.Exceptions
{
    public class DeviceNotFoundException : Exception
    {
        public DeviceNotFoundException(Guid deviceId) : base($"Device with Id: {deviceId} not found.")
        {

        }
    }
}
