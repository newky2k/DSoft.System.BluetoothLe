namespace System.BluetoothLe.EventArgs
{
    /// <summary>
    /// Raised when a scan cannot be started, or is terminated by the platform before its timeout elapses.
    /// </summary>
    public class ScanFailedEventArgs : System.EventArgs
    {
        /// <summary>Why the platform refused, or abandoned, the scan.</summary>
        public ScanFailureReason Reason { get; }

        /// <summary>Creates the arguments for a scan failure.</summary>
        public ScanFailedEventArgs(ScanFailureReason reason)
        {
            Reason = reason;
        }
    }
}
