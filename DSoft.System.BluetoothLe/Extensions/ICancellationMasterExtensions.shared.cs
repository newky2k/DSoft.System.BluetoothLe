using System;
using System.Threading;

namespace System.BluetoothLe
{
    /// <summary>
    /// Implemented by an object that owns a cancellation scope covering every operation issued through it,
    /// so that tearing the object down cancels its outstanding work.
    /// </summary>
    /// <remarks>
    /// Internal from 4.0. It was never something a consumer could usefully implement - the library only ever
    /// consumes it, from Device - and exposing it meant the token source backing every device's in-flight GATT
    /// traffic could be replaced from outside.
    /// </remarks>
    internal interface ICancellationMaster
    {
        CancellationTokenSource TokenSource { get; set; }
    }

    internal static class CancellationMasterExtensions
    {
        /// <summary>
        /// Links the caller's token to the owner's, so an operation is cancelled either by the caller or by
        /// the owner being torn down.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The owner has already been torn down.</exception>
        public static CancellationTokenSource GetCombinedSource(this ICancellationMaster cancellationMaster, CancellationToken token)
        {
            // Snapshot into a local first. CancelEverything nulls the field, so reading it twice - once to test
            // and once to use - can see a live source and then a null one, and the NullReferenceException that
            // produces surfaces from inside a read or write rather than as a clean disposal failure.
            var source = cancellationMaster.TokenSource;
            if (source == null)
                throw new ObjectDisposedException(cancellationMaster.GetType().Name,
                    "The object owning this operation has been disposed, so no further Bluetooth operations can be started on it.");

            return CancellationTokenSource.CreateLinkedTokenSource(source.Token, token);
        }

        /// <summary>
        /// Cancels every operation issued through the owner and releases the scope.
        /// </summary>
        public static void CancelEverything(this ICancellationMaster cancellationMaster)
        {
            // Detach before cancelling, so a continuation that runs synchronously on Cancel cannot find a
            // source that is mid-disposal, and two concurrent callers cannot both dispose the same one.
            var source = cancellationMaster.TokenSource;
            cancellationMaster.TokenSource = null;

            if (source == null)
                return;

            try
            {
                source.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // Already disposed by a concurrent teardown; the operations are cancelled either way.
            }

            source.Dispose();
        }

        /// <summary>
        /// Cancels every operation issued through the owner and opens a fresh scope, so the object stays usable.
        /// </summary>
        public static void CancelEverythingAndReInitialize(this ICancellationMaster cancellationMaster)
        {
            cancellationMaster.CancelEverything();
            cancellationMaster.TokenSource = new CancellationTokenSource();
        }
    }
}
