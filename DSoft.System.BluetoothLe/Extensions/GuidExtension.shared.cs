using System;

namespace System.BluetoothLe.Extensions
{
    public static class GuidExtension
    {
        // The 96 bits every 16- and 32-bit Bluetooth assigned number is expanded against.
        private const string BluetoothBaseUuidSuffix = "-0000-1000-8000-00805f9b34fb";

        /// <summary>
        /// Create a full Guid from the Bluetooth "Assigned Number" (short version)
        /// </summary>
        /// <returns>a Guid of the form {00002A37-0000-1000-8000-00805f9b34fb}</returns>
        /// <param name="partial">4 digit hex value, eg 0x2A37 (which is heart rate measurement)</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The value does not fit in the 16 bits a Bluetooth assigned number occupies.
        /// </exception>
        /// <remarks>
        /// The parameter stays <see cref="int"/> rather than becoming <see cref="ushort"/> even though the
        /// range is 16 bits: extension method lookup on an integer literal such as
        /// <c>0x2902.UuidFromPartial()</c> will not find an extension declared on <see cref="ushort"/>, so
        /// narrowing the type would break every call site that reads naturally.
        /// </remarks>
        public static Guid UuidFromPartial(this Int32 @partial)
        {
            if (@partial < 0 || @partial > 0xFFFF)
                throw new ArgumentOutOfRangeException(nameof(@partial), @partial,
                    "A Bluetooth assigned number is a 16-bit value, so it must be between 0x0000 and 0xFFFF.");

            // X4 rather than X plus PadRight. Padding on the right moved the digits: 0x0A37 formatted as "A37"
            // padded to "A370", which produced the wrong UUID for every assigned number below 0x1000.
            var id = @partial.ToString("X4");

            return Guid.ParseExact("0000" + id + BluetoothBaseUuidSuffix, "d");
        }

        /// <summary>
        /// Extract the Bluetooth "Assigned Number" from a Uuid 
        /// </summary>
        /// <returns>4 digit hex value, eg 0x2A37 (which is heart rate measurement)</returns>
        /// <param name="uuid">a Guid of the form {00002A37-0000-1000-8000-00805f9b34fb}</param>
        /// <exception cref="ArgumentException">
        /// The Guid is not built on the Bluetooth base UUID, so it carries no assigned number.
        /// </exception>
        public static string PartialFromUuid(this Guid uuid)
        {
            // opposite of the UuidFromPartial method
            var id = uuid.ToString("d");

            // Validating the suffix rather than only the length: a vendor-specific 128-bit UUID is the same
            // length as a base-derived one, and slicing four characters out of it returned a plausible looking
            // "assigned number" that means nothing.
            if (!id.EndsWith(BluetoothBaseUuidSuffix, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException(
                    $"The UUID {uuid} is not derived from the Bluetooth base UUID, so it has no assigned number.",
                    nameof(uuid));

            return "0x" + id.Substring(4, 4).ToUpperInvariant();
        }

        public static string ToHexString(this byte[] bytes)
        {
            return bytes != null ? BitConverter.ToString(bytes) : string.Empty;
        }
    }
}
