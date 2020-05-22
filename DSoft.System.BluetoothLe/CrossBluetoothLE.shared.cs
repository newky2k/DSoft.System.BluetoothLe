using System;
using System.BluetoothLe;
using System.BluetoothLe.Contracts;

namespace System.BluetoothLe
{
    /// <summary>
    /// Cross platform bluetooth LE implemenation.
    /// </summary>
    public static class CrossBluetoothLE
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
                    throw NotImplementedInReferenceAssembly();
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

        internal static Exception NotImplementedInReferenceAssembly()
        {
            return new NotImplementedException("This functionality is not implemented in the portable version of this assembly.  You should reference the NuGet package from your main application project in order to reference the platform-specific implementation.");
        }
    }
}