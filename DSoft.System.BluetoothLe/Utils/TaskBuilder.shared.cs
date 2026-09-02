using System;
using System.Threading;
using System.Threading.Tasks;

namespace System.BluetoothLe.Utils
{
    public static class TaskBuilder
    {
        /// <summary>
        /// How long to wait for the main-thread queue before giving up on an operation.
        /// </summary>
        public static TimeSpan SemaphoreQueueTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Platform specific main thread invocation. Useful to avoid GATT 133 errors on Android.
        /// Set this to NULL in order to disable main thread queued invocations.
        /// Android: already implemented and set by default
        /// UWP, iOS, macOS: NULL by default - not needed, turning this on is redundant as it's already handled internaly by the platform
        /// </summary>
        public static Action<Action> MainThreadInvoker { get; set; }

        private static readonly SemaphoreSlim QueueSemaphore = new SemaphoreSlim(1);

        /// <summary>
        /// Starts a native operation and completes when one of the supplied events fires.
        /// </summary>
        /// <remarks>
        /// <paramref name="execute"/> is a <see cref="Func{Task}"/> rather than an <see cref="Action"/> so that a
        /// caller whose native call is itself asynchronous is awaited rather than fired and forgotten. Previously an
        /// <c>async</c> lambda bound to <see cref="Action"/> became an async void call, and a failure inside it was
        /// raised on the thread pool as an unhandled exception instead of faulting the returned task.
        /// </remarks>
        public static async Task<TReturn> FromEvent<TReturn, TEventHandler, TRejectHandler>(
            Func<Task> execute,
            Func<Action<TReturn>, Action<Exception>, TEventHandler> getCompleteHandler,
            Action<TEventHandler> subscribeComplete,
            Action<TEventHandler> unsubscribeComplete,
            Func<Action<Exception>, TRejectHandler> getRejectHandler,
            Action<TRejectHandler> subscribeReject,
            Action<TRejectHandler> unsubscribeReject,
            CancellationToken token = default)
        {
            // RunContinuationsAsynchronously keeps the caller's continuation off whichever native callback thread
            // completed the task. Without it a consumer's continuation runs inline on the CoreBluetooth delegate
            // queue or the Android GATT callback thread, where blocking or issuing further GATT traffic deadlocks.
            var tcs = new TaskCompletionSource<TReturn>(TaskCreationOptions.RunContinuationsAsynchronously);
            void Complete(TReturn args) => tcs.TrySetResult(args);
            void CompleteException(Exception ex) => tcs.TrySetException(ex);
            void Reject(Exception ex) => tcs.TrySetException(ex);

            var handler = getCompleteHandler(Complete, CompleteException);
            var rejectHandler = getRejectHandler(Reject);

            try
            {
                subscribeComplete(handler);
                subscribeReject(rejectHandler);
                using (token.Register(() => tcs.TrySetCanceled(token), false))
                {
                    return await SafeEnqueueAndExecute(execute, token, tcs);
                }
            }
            finally
            {
                unsubscribeReject(rejectHandler);
                unsubscribeComplete(handler);
            }
        }

        /// <summary>
        /// Runs an operation on the platform's main thread, when a <see cref="MainThreadInvoker"/> is configured.
        /// </summary>
        public static Task EnqueueOnMainThreadAsync(Func<Task> execute, CancellationToken token = default)
            => SafeEnqueueAndExecute<bool>(execute, token);


        private static async Task<TReturn> SafeEnqueueAndExecute<TReturn>(Func<Task> execute, CancellationToken token, TaskCompletionSource<TReturn> tcs = null)
        {
            if (MainThreadInvoker != null)
            {
                var shouldReleaseSemaphore = false;
                var shouldCompleteTask = tcs == null;
                tcs = tcs ?? new TaskCompletionSource<TReturn>(TaskCreationOptions.RunContinuationsAsynchronously);
                if (await QueueSemaphore.WaitAsync(SemaphoreQueueTimeout, token))
                {
                    shouldReleaseSemaphore = true;
                    try
                    {
                        // The lambda below is async void by necessity: MainThreadInvoker takes an Action, because the
                        // platform post primitives it wraps are themselves fire-and-forget. It is safe here only
                        // because every path inside it is caught and routed into the TaskCompletionSource, so no
                        // exception can escape onto the main thread and terminate the process.
                        MainThreadInvoker.Invoke(async () =>
                        {
                            try
                            {
                                await execute();

                                if (shouldCompleteTask)
                                {
                                    tcs.TrySetResult(default);
                                }
                            }
                            catch (Exception ex)
                            {
                                tcs.TrySetException(ex);
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        // The invoker itself threw, so the lambda above will never run and never complete the task.
                        // Faulting the task here is what lets the finally below release the semaphore.
                        tcs.TrySetException(ex);
                    }
                }
                else
                {
                    tcs.TrySetCanceled(token);
                }

                try
                {
                    return await tcs.Task;
                }
                finally
                {
                    if (shouldReleaseSemaphore)
                    {
                        QueueSemaphore.Release();
                    }
                }
            }

            try
            {
                await execute();
            }
            catch (Exception ex) when (tcs != null)
            {
                // With no invoker there is no outer handler, so a synchronous failure to start the native operation
                // has to be pushed into the awaited task, or the caller waits for an event that will never arrive.
                tcs.TrySetException(ex);
            }

            return await (tcs?.Task ?? Task.FromResult(default(TReturn)));
        }
    }
}
