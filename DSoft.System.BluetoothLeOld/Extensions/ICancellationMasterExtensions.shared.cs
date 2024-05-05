using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace System.BluetoothLe
{
    public interface ICancellationMaster
    {
        CancellationTokenSource TokenSource { get; set; }
    }

    public static class ICancellationMasterExtensions
    {
        public static CancellationTokenSource GetCombinedSource(this ICancellationMaster cancellationMaster, CancellationToken token)
        {
            return CancellationTokenSource.CreateLinkedTokenSource(cancellationMaster.TokenSource.Token, token);
        }

        public static void CancelEverything(this ICancellationMaster cancellationMaster)
        {
            cancellationMaster.TokenSource?.Cancel();
            cancellationMaster.TokenSource?.Dispose();
            cancellationMaster.TokenSource = null;
        }

        public static void CancelEverythingAndReInitialize(this ICancellationMaster cancellationMaster)
        {
            cancellationMaster.CancelEverything();
            cancellationMaster.TokenSource = new CancellationTokenSource();
        }
    }
}
