namespace System.BluetoothLe
{
    /// <summary>
    /// Determines the connection state of the device.
    /// </summary>
	public enum DeviceState
    {
        /// <summary>
        /// Device is disconnected.
        /// </summary>
        Disconnected,

        /// <summary>
        /// Device is connecting.
        /// </summary>
        Connecting,

        /// <summary>
        /// Device is connected.
        /// </summary>
        Connected,

        /// <summary>
        /// OnAndroid: Device is connected to the system. In order to use this device please call connect it by using the Adapter. 
        /// </summary>
        Limited,

        /// <summary>
        /// Device is in the process of disconnecting. Reported where the platform surfaces the intermediate
        /// state; a platform that does not will move straight from Connected to Disconnected.
        /// </summary>
        /// <remarks>
        /// Appended rather than placed after <see cref="Disconnected"/> so that the numeric values of the
        /// existing members do not shift under a consumer that has persisted them.
        /// </remarks>
        Disconnecting
    }
}