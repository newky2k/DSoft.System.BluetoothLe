using System;
using Android.Bluetooth;

namespace System.BluetoothLe.EventArgs
{
    /// <summary>
    /// Carries the outcome of a descriptor read or write up from the Android GATT callback.
    /// </summary>
    public class DescriptorCallbackEventArgs
    {
        /// <summary>The descriptor the callback was about.</summary>
        public BluetoothGattDescriptor Descriptor { get; }

        /// <summary>Set when the platform reported a failing status, otherwise <see langword="null"/>.</summary>
        public Exception Exception { get; }

        /// <summary>
        /// The raw GATT status, carried so that the exception a caller sees can name the actual failure rather
        /// than only that one occurred.
        /// </summary>
        public GattStatus Status { get; }

        public DescriptorCallbackEventArgs(BluetoothGattDescriptor descriptor, GattStatus status, Exception exception = null)
        {
            Descriptor = descriptor;
            Status = status;
            Exception = exception;
        }
    }
}
