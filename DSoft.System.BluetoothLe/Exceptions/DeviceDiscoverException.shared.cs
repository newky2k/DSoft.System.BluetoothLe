﻿using System;

namespace System.BluetoothLe.Exceptions
{
    public class DeviceDiscoverException : Exception
    {
        public DeviceDiscoverException() : base("Could not find the specific device.")
        {
        }
    }
}