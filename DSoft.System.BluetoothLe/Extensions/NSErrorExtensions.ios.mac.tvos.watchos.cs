using Foundation;

namespace System.BluetoothLe.Extensions
{
    internal static class NSErrorExtensions
    {
        // The value of CoreBluetooth's CBATTErrorDomain constant. Compared as a string so this does not depend on
        // how the binding surfaces the constant.
        private const string AttErrorDomain = "CBATTErrorDomain";

        /// <summary>
        /// The ATT error code carried by <paramref name="error"/> when it is a <c>CBATTError</c>, which shares its
        /// values with the ATT specification; <see langword="null"/> for an error in any other domain, such as a
        /// <c>CBError</c>, whose codes mean something else.
        /// </summary>
        public static int? GetAttStatus(this NSError error)
        {
            if (error == null || error.Domain != AttErrorDomain)
            {
                return null;
            }

            return (int)error.Code;
        }
    }
}
