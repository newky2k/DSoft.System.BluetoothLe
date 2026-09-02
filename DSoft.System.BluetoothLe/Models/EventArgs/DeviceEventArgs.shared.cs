using System;
using System.BluetoothLe;

namespace System.BluetoothLe.EventArgs
{
    /// <summary>
    /// Identifies the device an adapter event concerns.
    /// </summary>
    public class DeviceEventArgs : System.EventArgs
    {
        /// <summary>The device the event concerns. Never null.</summary>
        public Device Device { get; }

        /// <summary>Creates the arguments for a device event.</summary>
        public DeviceEventArgs(Device device)
        {
            Device = device ?? throw new ArgumentNullException(nameof(device));
        }
    }
}
