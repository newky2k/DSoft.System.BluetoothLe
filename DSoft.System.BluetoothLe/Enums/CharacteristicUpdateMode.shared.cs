namespace System.BluetoothLe
{
    /// <summary>
    /// Which of the two GATT server-initiated update mechanisms a subscription should use.
    /// </summary>
    /// <remarks>
    /// Notifications are unacknowledged and indications are acknowledged, so a peripheral generally supports
    /// one or the other, and writing the wrong bit into the client characteristic configuration descriptor
    /// either fails outright or silently produces no traffic. Before 4.0 the Android implementation wrote the
    /// descriptor twice whenever a characteristic advertised both flags - indicate first, then notify - so the
    /// second write always won and the first was wasted GATT traffic on the connection's single outstanding
    /// operation. Making the choice explicit removes the double write and lets a consumer override the guess
    /// for a peripheral whose advertised properties do not match its behaviour.
    /// </remarks>
    public enum CharacteristicUpdateMode
    {
        /// <summary>
        /// Choose from the characteristic's advertised properties: indicate only when indicate is the sole
        /// mechanism offered, otherwise notify. This reproduces the outcome of the pre-4.0 double write.
        /// </summary>
        Auto = 0,

        /// <summary>Subscribe with unacknowledged notifications.</summary>
        Notify = 1,

        /// <summary>Subscribe with acknowledged indications.</summary>
        Indicate = 2,
    }
}
