using System;
using System.Collections.Generic;
using System.Linq;

namespace System.BluetoothLe
{
    // Source: https://developer.bluetooth.org/gatt/descriptors/Pages/DescriptorsHomePage.aspx
    public static class KnownDescriptors
    {
        /// <summary>
        /// Finds the Bluetooth SIG's name for a descriptor UUID, or a placeholder when the UUID is not a
        /// standard one.
        /// </summary>
        public static KnownDescriptor Lookup(Guid id)
        {
            return LookupTable.TryGetValue(id, out var known) ? known : new KnownDescriptor("Unknown descriptor", Guid.Empty);
        }

        private static readonly IList<KnownDescriptor> Descriptors = new List<KnownDescriptor>()
        {
            new KnownDescriptor("Characteristic Extended Properties", Guid.ParseExact("00002900-0000-1000-8000-00805f9b34fb", "d")),
            new KnownDescriptor("Characteristic User Description", Guid.ParseExact("00002901-0000-1000-8000-00805f9b34fb", "d")),
            new KnownDescriptor("Client Characteristic Configuration", Guid.ParseExact("00002902-0000-1000-8000-00805f9b34fb", "d")),
            new KnownDescriptor("Server Characteristic Configuration", Guid.ParseExact("00002903-0000-1000-8000-00805f9b34fb", "d")),
            new KnownDescriptor("Characteristic Presentation Format", Guid.ParseExact("00002904-0000-1000-8000-00805f9b34fb", "d")),
            new KnownDescriptor("Characteristic Aggregate Format", Guid.ParseExact("00002905-0000-1000-8000-00805f9b34fb", "d")),
            new KnownDescriptor("Valid Range", Guid.ParseExact("00002906-0000-1000-8000-00805f9b34fb", "d")),
            new KnownDescriptor("External Report Reference", Guid.ParseExact("00002907-0000-1000-8000-00805f9b34fb", "d")),
            new KnownDescriptor("Export Reference", Guid.ParseExact("00002908-0000-1000-8000-00805f9b34fb", "d")),
        };

        // Declared after the list it is built from: static field initialisers run in textual order, and a
        // field initialiser rather than a static constructor leaves the type beforefieldinit, so the table is
        // built lazily on first lookup instead of on the first touch of anything in this class.
        private static readonly Dictionary<Guid, KnownDescriptor> LookupTable = Descriptors.ToDictionary(d => d.Id, d => d);
    }
}
