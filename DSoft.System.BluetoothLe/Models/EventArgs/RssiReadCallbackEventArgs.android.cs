using System;
using Android.Bluetooth;
using System.BluetoothLe.Contracts;

namespace System.BluetoothLe.CallbackEventArgs
{
    public class RssiReadCallbackEventArgs : System.EventArgs
    {
        public Exception Error { get; }
        public int Rssi { get; }

        public RssiReadCallbackEventArgs(Exception error, int rssi)
        {
            Error = error;
            Rssi = rssi;
        }
    }
}