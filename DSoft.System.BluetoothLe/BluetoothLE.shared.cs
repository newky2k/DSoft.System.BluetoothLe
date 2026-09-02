using System;
using System.BluetoothLe.EventArgs;
using System.BluetoothLe.Utils;
using System.Threading;
using System.Threading.Tasks;

namespace System.BluetoothLe
{
    /// <summary>
    /// The entry point to the library: the Bluetooth radio itself, and the <see cref="Adapter"/> that scans
    /// and connects with it.
    /// </summary>
    public partial class BluetoothLE : IDisposable
    {
        #region Static Singleton Accessors

        // ExecutionAndPublication rather than PublicationOnly: the factory calls Initialize(), which registers
        // a broadcast receiver on Android and constructs a CBCentralManager on Apple. Under PublicationOnly
        // two threads racing on first access both run it, and the loser's radio objects are silently
        // abandoned - on Android leaving a receiver registered against an instance nothing will ever dispose.
        static readonly Lazy<BluetoothLE> Implementation = new Lazy<BluetoothLE>(CreateImplementation, LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// Current bluetooth LE implementation.
        /// </summary>
        /// <remarks>
        /// This succeeds on every target framework, including those with no Bluetooth support, where
        /// <see cref="State"/> reports <see cref="BluetoothState.Unavailable"/>. Only <see cref="Adapter"/>
        /// throws on such a target.
        /// </remarks>
        public static BluetoothLE Current => Implementation.Value;

        static BluetoothLE CreateImplementation()
        {
            var implementation = new BluetoothLE();
            implementation.Initialize();
            return implementation;
        }

        #endregion

        #region Fields
        private readonly Lazy<Adapter> _adapter;
        private BluetoothState _state;
        private bool _disposed;
        #endregion

        #region Events

        /// <summary>
        /// Raised whenever <see cref="State"/> changes.
        /// </summary>
        /// <remarks>
        /// Raised on whichever thread the platform reported the change on - the main thread on Android, the
        /// CoreBluetooth delegate queue on Apple. Marshal to your UI thread in the handler.
        /// </remarks>
        public event EventHandler<BluetoothStateChangedArgs> StateChanged;

        #endregion

        #region Properties

        /// <summary>Whether the device has a usable Bluetooth Low Energy radio at all.</summary>
        public bool IsAvailable => _state != BluetoothState.Unavailable;

        /// <summary>Whether the radio is switched on right now.</summary>
        /// <remarks>
        /// Do not gate a scan on this. Immediately after start-up the radio has not settled and reports
        /// <see cref="BluetoothState.Unknown"/> on Apple, so a consumer that checks this and bails out fails
        /// against a working adapter. Await <see cref="WaitForStateAsync"/> instead.
        /// </remarks>
        public bool IsOn => _state == BluetoothState.On;

        /// <summary>
        /// The adapter used to scan for and connect to devices.
        /// </summary>
        /// <exception cref="PlatformNotSupportedException">
        /// The target framework has no Bluetooth implementation - typically because a platform-neutral build
        /// of this package was resolved. Reference the package from a platform-specific target framework.
        /// </exception>
        public Adapter Adapter => _adapter.Value;

        /// <summary>The current state of the Bluetooth radio.</summary>
        public BluetoothState State
        {
            get => _state;
            protected set
            {
                if (_state == value)
                    return;

                var oldState = _state;
                _state = value;
                StateChanged?.Invoke(this, new BluetoothStateChangedArgs(oldState, _state));
            }
        }

        #endregion

        #region Constructors

        internal BluetoothLE()
        {
            // ExecutionAndPublication for the same reason as the singleton above: the factory builds a native
            // adapter, and constructing two of those is not free of side effects.
            _adapter = new Lazy<Adapter>(CreateAdapter, LazyThreadSafetyMode.ExecutionAndPublication);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Brings up the platform radio bindings. Called once by <see cref="Current"/>.
        /// </summary>
        public void Initialize()
        {
            InitializeNative();
            State = GetInitialStateNative();
        }

        /// <summary>
        /// Completes once the radio reaches <paramref name="desiredState"/>, or immediately if it is already
        /// in that state.
        /// </summary>
        /// <param name="desiredState">The state to wait for.</param>
        /// <param name="cancellationToken">Cancels the wait.</param>
        /// <returns>The state that satisfied the wait.</returns>
        /// <remarks>
        /// This exists so a consumer can await readiness rather than poll <see cref="IsOn"/>. A central
        /// manager that has just been constructed has not yet settled and reports
        /// <see cref="BluetoothState.Unknown"/>, so code that pre-checks <see cref="IsOn"/> and gives up
        /// defeats the platform's own wait-for-powered-on handling and fails against a working radio.
        /// </remarks>
        public Task<BluetoothState> WaitForStateAsync(BluetoothState desiredState, CancellationToken cancellationToken = default)
            => WaitForStateCoreAsync(state => state == desiredState, cancellationToken);

        /// <summary>
        /// Completes once the radio has settled into any determinate state - that is, once
        /// <see cref="State"/> is no longer <see cref="BluetoothState.Unknown"/>.
        /// </summary>
        /// <param name="cancellationToken">Cancels the wait.</param>
        /// <returns>The state the radio settled into, which may well be off or unauthorised.</returns>
        /// <remarks>
        /// Use this when you want to report the real state to the user rather than wait for a particular one:
        /// it distinguishes "still starting up" from "switched off", which <see cref="IsOn"/> cannot.
        /// </remarks>
        public Task<BluetoothState> WaitForAvailabilityAsync(CancellationToken cancellationToken = default)
            => WaitForStateCoreAsync(state => state != BluetoothState.Unknown, cancellationToken);

        private async Task<BluetoothState> WaitForStateCoreAsync(Func<BluetoothState, bool> predicate, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (predicate(State))
                return State;

            // RunContinuationsAsynchronously so the caller's continuation does not run inline on the Android
            // main thread or the CoreBluetooth delegate queue that raised StateChanged.
            var tcs = new TaskCompletionSource<BluetoothState>(TaskCreationOptions.RunContinuationsAsynchronously);

            void OnStateChanged(object sender, BluetoothStateChangedArgs args)
            {
                if (predicate(args.NewState))
                    tcs.TrySetResult(args.NewState);
            }

            StateChanged += OnStateChanged;
            try
            {
                // The state can move between the check above and the subscription taking effect. That change
                // raises StateChanged before this handler is attached and is never raised again, so without
                // this second read the wait would hang until the next unrelated state change.
                if (predicate(State))
                    return State;

                using (cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken), false))
                {
                    return await tcs.Task;
                }
            }
            finally
            {
                StateChanged -= OnStateChanged;
            }
        }

        private Adapter CreateAdapter()
        {
            return CreateNativeAdapter();
        }

        /// <summary>
        /// Releases the platform radio bindings - the Android broadcast receiver, the Apple central manager
        /// delegate subscription.
        /// </summary>
        /// <remarks>
        /// Only meaningful at application shutdown, or in a test host that builds and tears the stack down
        /// repeatedly. <see cref="Current"/> is a singleton, so disposing it leaves the process without a
        /// working radio until it is restarted. The <see cref="Adapter"/> is deliberately not disposed here:
        /// it has its own lifetime and a consumer may still hold connected devices through it.
        /// </remarks>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            DisposeNative();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Implemented by whichever platform partial is compiled into this target framework. Targets with no
        /// Bluetooth support supply no implementation, and the call is elided.
        /// </summary>
        partial void DisposeNative();

        #endregion
    }
}
