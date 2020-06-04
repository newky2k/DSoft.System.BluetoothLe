using System;
using Windows.Devices.Bluetooth.Advertisement;
using System.BluetoothLe.Contracts;
using Trace = System.BluetoothLe.Trace;

namespace System.BluetoothLe.Extensions
{
    /// <summary>
    /// See https://github.com/xabre/xamarin-bluetooth-le/blob/master/doc/scanmode_mapping.md
    /// </summary>
    internal static class ScanModeExtension
    {
        public static BluetoothLEScanningMode ToNative(this ScanMode scanMode)
        {
            switch (scanMode)
            {
                case ScanMode.Passive:
                    return BluetoothLEScanningMode.Passive;
                case ScanMode.LowPower:
                case ScanMode.Balanced:
                case ScanMode.LowLatency:
                    return BluetoothLEScanningMode.Active;
                default:
                    throw new ArgumentOutOfRangeException(nameof(scanMode), scanMode, null);
            }
        }
    }
}