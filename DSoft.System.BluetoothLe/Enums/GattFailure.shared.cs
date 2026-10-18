namespace System.BluetoothLe
{
    /// <summary>
    /// What a failed GATT operation's status says went wrong, for the statuses an app needs to act on.
    /// </summary>
    /// <remarks>
    /// The raw code is on <see cref="IGattStatusException.GattStatus"/>. The security failures matter most: a
    /// peripheral that requires a bonded, encrypted link answers an unbonded central with one of them, and the
    /// app should ask the user to pair - or to forget the device and pair again, if the peripheral has lost its
    /// keys - rather than report a generic fault.
    /// </remarks>
    public enum GattFailure
    {
        /// <summary>
        /// No GATT status was reported: the operation could not be started, the device disconnected, or the
        /// platform failed it for a reason of its own.
        /// </summary>
        None,

        /// <summary>A GATT status that is none of the others. Inspect <see cref="IGattStatusException.GattStatus"/>.</summary>
        Other,

        /// <summary>ATT error 0x05, Insufficient Authentication: the link must be authenticated (bonded) first.</summary>
        InsufficientAuthentication,

        /// <summary>ATT error 0x08, Insufficient Authorization: the peripheral refuses this central the operation.</summary>
        InsufficientAuthorization,

        /// <summary>ATT error 0x0F, Insufficient Encryption: the link must be encrypted first.</summary>
        InsufficientEncryption
    }

    /// <summary>
    /// A failed read or write that can carry the GATT status the peripheral or the platform's GATT layer
    /// reported. Implemented by <see cref="CharacteristicReadException"/>, <see cref="CharacteristicWriteException"/>,
    /// <see cref="DescriptorReadException"/> and <see cref="DescriptorWriteException"/>, so one catch can recognise a
    /// security failure from any of them.
    /// </summary>
    /// <example>
    /// <code>
    /// catch (BleException ex) when (ex is IGattStatusException { GattFailure: GattFailure.InsufficientAuthentication or GattFailure.InsufficientEncryption })
    /// {
    ///     // Ask the user to pair.
    /// }
    /// </code>
    /// </example>
    public interface IGattStatusException
    {
        /// <summary>
        /// The GATT status code, where one was reported: on Android the <c>GattStatus</c> from the callback, on
        /// Apple platforms the <c>CBATTError</c> code when the error is in <c>CBATTErrorDomain</c>, and on Windows
        /// the protocol error. ATT error codes have the same value on every platform; Android can also report
        /// codes of its own GATT layer, such as 0x85. <see langword="null"/> when no status was reported.
        /// </summary>
        int? GattStatus { get; }

        /// <summary><see cref="GattStatus"/>, classified.</summary>
        GattFailure GattFailure { get; }
    }

    internal static class GattStatusCodes
    {
        public static GattFailure Classify(int? gattStatus)
        {
            switch (gattStatus)
            {
                case null:
                    return GattFailure.None;
                case 0x05:
                    return GattFailure.InsufficientAuthentication;
                case 0x08:
                    return GattFailure.InsufficientAuthorization;
                case 0x0F:
                    return GattFailure.InsufficientEncryption;
                default:
                    return GattFailure.Other;
            }
        }
    }
}
