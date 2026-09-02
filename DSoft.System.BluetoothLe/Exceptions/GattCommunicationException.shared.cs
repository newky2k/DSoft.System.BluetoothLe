using System;

namespace System.BluetoothLe
{
    /// <summary>
    /// A GATT operation that is not a characteristic or descriptor read or write - service discovery,
    /// characteristic discovery, a request for access to a service - was refused by the peripheral or by
    /// the platform.
    /// </summary>
    /// <remarks>
    /// This exists because Windows reports those failures through a single status enumeration rather than
    /// through per-operation callbacks, and before 4.0 the Windows partials threw a bare
    /// <see cref="Exception"/> for all of them. That made Windows the one platform on which a GATT failure
    /// could not be caught as a <see cref="BleException"/>, so a consumer's Bluetooth error handling had to
    /// be written twice.
    /// </remarks>
    public class GattCommunicationException : BleException
    {
        /// <inheritdoc cref="Exception(string)"/>
        public GattCommunicationException(string message) : base(message)
        {
        }

        /// <inheritdoc cref="Exception(string, Exception)"/>
        public GattCommunicationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
