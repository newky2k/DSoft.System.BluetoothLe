namespace System.BluetoothLe
{
    /// <summary>
    /// The bond (pairing) state of a device, as the platform reports it.
    /// </summary>
    public enum DeviceBondState
    {
        /// <summary>The platform reports that the device is not bonded.</summary>
        NotBonded,

        /// <summary>Bonding with the device is in progress.</summary>
        Bonding,

        /// <summary>The device is bonded: the platform holds keys for an encrypted link to it.</summary>
        Bonded,

        /// <summary>
        /// The platform does not expose the bond state. Apple platforms always report this: CoreBluetooth
        /// pairs on demand, the first time an encrypted characteristic is accessed, and offers no way to ask
        /// whether a bond exists. Also reported where the state cannot be read, for example on Android 12 and
        /// later without the BLUETOOTH_CONNECT permission, and for a stand-in device.
        /// </summary>
        /// <remarks>Declared last so the values of the existing members are unchanged.</remarks>
        Unknown
    }
}
