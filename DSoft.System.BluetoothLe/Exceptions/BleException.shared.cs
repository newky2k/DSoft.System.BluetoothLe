using System;

namespace System.BluetoothLe
{
    /// <summary>
    /// The base type for every exception this library raises deliberately.
    /// </summary>
    /// <remarks>
    /// A consumer needs a single catch that separates "the Bluetooth stack told us this went wrong" from a
    /// genuine programming error such as <see cref="ArgumentNullException"/>. Before 4.0 the library's
    /// exceptions each derived straight from <see cref="Exception"/>, so the only way to do that was to list
    /// every type by name and to keep that list up to date with the library.
    /// </remarks>
    public abstract class BleException : Exception
    {
        /// <inheritdoc cref="Exception(string)"/>
        protected BleException(string message) : base(message)
        {
        }

        /// <inheritdoc cref="Exception(string, Exception)"/>
        protected BleException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
