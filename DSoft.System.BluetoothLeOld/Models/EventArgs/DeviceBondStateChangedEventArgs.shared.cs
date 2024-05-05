using System.BluetoothLe;

namespace System.BluetoothLe.EventArgs
{
    public class DeviceBondStateChangedEventArgs : System.EventArgs
    {
        public Device Device { get; set; }
        public DeviceBondState State { get; set; }
    }
}