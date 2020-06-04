using System;
using System.Collections.Generic;
using System.Text;

namespace System.BluetoothLe.Exceptions
{
    public class PlatformNotSupportedException : Exception
    {
        public PlatformNotSupportedException() : base("Platfrom not supported.  Ensure you are using the correct version of the library")
        {

        }
    }
}
