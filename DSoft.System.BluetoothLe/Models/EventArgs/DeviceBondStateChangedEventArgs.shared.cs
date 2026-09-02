using System;
using System.BluetoothLe;

namespace System.BluetoothLe.EventArgs
{
    /// <summary>
    /// Reports that a device's bond (pairing) state has changed.
    /// </summary>
    public class DeviceBondStateChangedEventArgs : System.EventArgs
    {
        /// <summary>The device whose bond state changed. Never null.</summary>
        public Device Device { get; }

        /// <summary>The bond state the device has moved to.</summary>
        public DeviceBondState State { get; }

        /// <summary>Creates the arguments for a bond state change.</summary>
        public DeviceBondStateChangedEventArgs(Device device, DeviceBondState state)
        {
            Device = device ?? throw new ArgumentNullException(nameof(device));
            State = state;
        }
    }
}
