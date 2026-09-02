using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace System.BluetoothLe
{
    /// <summary>
    /// A GATT service on a connected device.
    /// </summary>
    /// <remarks>
    /// A service caches the characteristics it discovers, so every lookup of the same characteristic returns
    /// the same object. Do not construct one directly.
    /// </remarks>
    public partial class Service : IDisposable
    {
        #region Fields
        private readonly List<Characteristic> _characteristics = new List<Characteristic>();
        private string _name;
        private bool _disposed;

        #endregion

        #region Properties
        /// <summary>
        /// A human-readable name from the Bluetooth SIG's assigned-numbers table, or "Unknown Service".
        /// </summary>
        public string Name => _name ?? (_name = KnownServices.Lookup(Id).Name);

        /// <summary>The service's UUID.</summary>
        public Guid Id => NativeGuid;

        /// <summary>
        /// Whether this is a primary service. Always reported as <see langword="true"/> on Windows, whose API
        /// no longer exposes the distinction.
        /// </summary>
        public bool IsPrimary => NativeIsPrimary;

        /// <summary>The device this service belongs to.</summary>
        public Device Device { get; }

        #endregion

        #region Constructors

        private Service(Device device)
        {
            Device = device ?? throw new ArgumentNullException(nameof(device));
        }

        #endregion

        #region Methods

        /// <summary>
        /// Discovers the service's characteristics. The result is cached, so the same
        /// <see cref="Characteristic"/> objects come back on every call.
        /// </summary>
        public async Task<IReadOnlyList<Characteristic>> GetCharacteristicsAsync(CancellationToken cancellationToken = default)
        {
            lock (_characteristics)
            {
                if (_characteristics.Count > 0)
                {
                    return _characteristics.ToArray();
                }
            }

            using (var source = Device.GetCombinedSource(cancellationToken))
            {
                var characteristics = await GetCharacteristicsNativeAsync(source.Token);

                lock (_characteristics)
                {
                    // A concurrent caller may have filled the cache while this discovery was in flight. Keeping
                    // whichever result landed first is what makes a Characteristic's identity stable, and that
                    // identity is what a consumer's ValueUpdated subscription hangs on.
                    if (_characteristics.Count == 0 && characteristics != null)
                    {
                        _characteristics.AddRange(characteristics);
                    }

                    return _characteristics.ToArray();
                }
            }
        }

        /// <summary>
        /// Finds one of the service's characteristics by UUID, or <see langword="null"/> when the service does
        /// not expose it.
        /// </summary>
        /// <remarks>
        /// This always goes through the discovery cache. Before 4.0 a process-wide switch could route it to a
        /// per-platform lookup that minted a fresh <see cref="Characteristic"/> on every call, so a consumer
        /// subscribing to <see cref="Characteristic.ValueUpdated"/> on one instance and calling
        /// <see cref="Characteristic.StartUpdatesAsync"/> on another silently received nothing.
        /// </remarks>
        public async Task<Characteristic> GetCharacteristicAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var characteristics = await GetCharacteristicsAsync(cancellationToken);
            return characteristics.FirstOrDefault(c => c.Id == id);
        }

        /// <summary>
        /// Releases the service's cached characteristics and any native handle behind it.
        /// </summary>
        /// <remarks>
        /// Called from <see cref="Device.ClearServices"/> when the link is already gone, so it detaches the
        /// notification plumbing without issuing the GATT traffic that <see cref="Characteristic.StopUpdatesAsync"/>
        /// would, which on a dead connection could only fail or hang.
        /// </remarks>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            lock (_characteristics)
            {
                foreach (var characteristic in _characteristics)
                {
                    characteristic.DetachNotifications();
                }

                _characteristics.Clear();
            }

            DisposeNative();

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Implemented by the platform partials that hold a disposable native handle. No-op elsewhere.
        /// </summary>
        partial void DisposeNative();

        #endregion
    }
}
