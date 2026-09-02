using System;

namespace System.BluetoothLe
{
    /// <summary>
    /// Thrown from <c>StartScanningForDevicesAsync</c> when the platform refuses to start the scan.
    /// </summary>
    /// <remarks>
    /// Before 4.0 a scan that could not start simply completed empty after the full <c>ScanTimeout</c>, so an
    /// adapter that was switched off and a genuinely empty room were indistinguishable to the caller.
    /// </remarks>
    public class AdapterScanException : BleException
    {
        /// <summary>Why the platform refused to scan.</summary>
        public ScanFailureReason Reason { get; }

        /// <inheritdoc cref="AdapterScanException(ScanFailureReason, string)"/>
        public AdapterScanException(ScanFailureReason reason)
            : base($"The Bluetooth scan could not be started: {reason}.")
        {
            Reason = reason;
        }

        /// <summary>Creates the exception with a platform-supplied explanation.</summary>
        public AdapterScanException(ScanFailureReason reason, string message) : base(message)
        {
            Reason = reason;
        }
    }
}
