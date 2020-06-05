using System;
using System.BluetoothLe;

namespace System.BluetoothLe
{
    /// <summary>
    /// Cross platform bluetooth LE implemenation.
    /// </summary>
    public static class BluetoothLE
    {
        static readonly Lazy<IBluetoothLE> Implementation = new Lazy<IBluetoothLE>(CreateImplementation, System.Threading.LazyThreadSafetyMode.PublicationOnly);

        /// <summary>
        /// Current bluetooth LE implementation.
        /// </summary>
        public static IBluetoothLE Current
        {
            get
            {
                var ret = Implementation.Value;
                if (ret == null)
                {
                    throw new PlatformNotSupportedException();
                }
                return ret;
            }
        }

        static IBluetoothLE CreateImplementation()
        {
            var implementation = new BleImplementation();
            implementation.Initialize();
            return implementation;
        }

    }
}