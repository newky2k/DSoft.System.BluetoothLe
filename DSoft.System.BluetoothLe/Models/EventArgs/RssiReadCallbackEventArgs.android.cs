using System;
using Android.Bluetooth;
using System.BluetoothLe;

namespace System.BluetoothLe.EventArgs
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