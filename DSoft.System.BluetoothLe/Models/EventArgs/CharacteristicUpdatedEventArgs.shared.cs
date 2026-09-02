using System;
using System.BluetoothLe;

namespace System.BluetoothLe.EventArgs
{
    /// <summary>
    /// Reports that a characteristic's value has been updated by a notification or an indication.
    /// </summary>
    public class CharacteristicUpdatedEventArgs : System.EventArgs
    {
        /// <summary>The characteristic whose value changed. Never null.</summary>
        public Characteristic Characteristic { get; }

        /// <summary>Creates the arguments for a characteristic update.</summary>
        public CharacteristicUpdatedEventArgs(Characteristic characteristic)
        {
            Characteristic = characteristic ?? throw new ArgumentNullException(nameof(characteristic));
        }
    }
}
