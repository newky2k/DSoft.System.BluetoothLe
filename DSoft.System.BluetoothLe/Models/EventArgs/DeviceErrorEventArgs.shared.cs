namespace System.BluetoothLe.EventArgs
{
    /// <summary>
    /// Identifies the device an adapter failure concerns, and why it failed.
    /// </summary>
    public class DeviceErrorEventArgs : DeviceEventArgs
    {
        /// <summary>What went wrong. May be empty when the platform gave no reason.</summary>
        public string ErrorMessage { get; }

        /// <summary>Creates the arguments for a device failure.</summary>
        public DeviceErrorEventArgs(Device device, string errorMessage) : base(device)
        {
            ErrorMessage = errorMessage;
        }
    }
}
