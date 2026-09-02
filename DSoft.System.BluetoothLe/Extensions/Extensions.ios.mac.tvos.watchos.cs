using System;
using CoreBluetooth;

namespace System.BluetoothLe
{
    public static class CBUUIDExtension
    {
        private const string BluetoothBaseUuidSuffix = "-0000-1000-8000-00805f9b34fb";

        /// <summary>
        /// Create a full Guid from the Bluetooth uuid (short version)
        /// </summary>
        /// <returns>a Guid of the form {00002A37-0000-1000-8000-00805f9b34fb}</returns>
        /// <param name="uuid">Bluetooth uuid</param>
        /// <exception cref="ArgumentException">The CBUUID is not 16-, 32- or 128-bit.</exception>
        public static Guid GuidFromUuid(this CBUUID uuid)
        {
            //this sometimes returns only the significant bits, e.g.
            //180d or whatever. so we need to add the full string
            var id = uuid.ToString();

            switch (id.Length)
            {
                case 4:
                    return Guid.ParseExact("0000" + id + BluetoothBaseUuidSuffix, "d");

                // CoreBluetooth also hands back 32-bit UUIDs as eight characters. That case fell through the
                // old length check to Guid.ParseExact, which threw FormatException from inside a scan callback
                // - so a peripheral advertising a 32-bit service UUID took down the discovery path.
                case 8:
                    return Guid.ParseExact(id.PadLeft(8, '0') + BluetoothBaseUuidSuffix, "d");

                case 36:
                    return Guid.ParseExact(id, "d");

                default:
                    throw new ArgumentException(
                        $"'{id}' is not a recognised CBUUID: expected 4, 8 or 36 characters.", nameof(uuid));
            }
        }
    }
}
