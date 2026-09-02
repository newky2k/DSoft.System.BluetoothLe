namespace System.BluetoothLe
{
    /// <summary>
    /// Why the platform refused to start a scan.
    /// </summary>
    /// <remarks>
    /// The three platforms report scan failures in incompatible ways - Android through an integer callback
    /// code, Apple through the central manager's state, Windows through a watcher status - so this is a
    /// deliberately coarse common denominator. It answers the question a consumer actually asks: is this
    /// something the user can fix, and if so what should I prompt them to do?
    /// </remarks>
    public enum ScanFailureReason
    {
        /// <summary>
        /// The Bluetooth adapter is switched off or otherwise unavailable. The user can fix this.
        /// </summary>
        AdapterOff = 0,

        /// <summary>
        /// The application lacks a permission scanning requires - on Android 12 and later BLUETOOTH_SCAN,
        /// on earlier releases a location permission. The user can fix this.
        /// </summary>
        PermissionDenied = 1,

        /// <summary>
        /// The hardware or the operating system does not support Bluetooth Low Energy scanning at all.
        /// </summary>
        NotSupported = 2,

        /// <summary>
        /// The platform failed the scan for a reason it did not attribute to any of the above.
        /// </summary>
        InternalError = 3,
    }
}
