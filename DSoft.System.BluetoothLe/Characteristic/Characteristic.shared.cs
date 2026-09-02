using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.BluetoothLe.EventArgs;

namespace System.BluetoothLe
{
    /// <summary>
    /// A single GATT characteristic on a connected device.
    /// </summary>
    /// <remarks>
    /// Instances are cached by their owning <see cref="Service"/>, so repeated lookups of the same
    /// characteristic return the same object and a consumer's <see cref="ValueUpdated"/> subscription survives
    /// them. Do not construct one directly.
    /// </remarks>
    public partial class Characteristic
    {
        #region Fields
        private IReadOnlyList<Descriptor> _descriptors;
        private CharacteristicWriteType _writeType = CharacteristicWriteType.Default;
        private string _name;

        #endregion

        #region Events

        // Raised only from the platform partials. The netstandard/net10.0 build has no notification source at
        // all, so on that target alone the compiler is right that nothing assigns it; suppressing the warning
        // here rather than project-wide keeps a genuinely dead event elsewhere visible.
#pragma warning disable CS0067
        /// <summary>
        /// Raised when the peripheral pushes a new value for this characteristic, after
        /// <see cref="StartUpdatesAsync"/> has succeeded.
        /// </summary>
        /// <remarks>
        /// Raised on the platform's GATT callback thread, not on the thread that subscribed. A handler must not
        /// block, and a consumer that updates a view model has to marshal for itself.
        /// </remarks>
        public event EventHandler<CharacteristicUpdatedEventArgs> ValueUpdated;
#pragma warning restore CS0067

        #endregion

        #region Properties
        /// <summary>The characteristic's UUID.</summary>
        public Guid Id => NativeGuid;

        /// <summary>The characteristic's UUID in the platform's own string form.</summary>
        public string Uuid => NativeUuid;

        /// <summary>The most recently read or notified value, or an empty array when there is none.</summary>
        public byte[] Value => NativeValue;

        /// <summary>The operations the peripheral says this characteristic supports.</summary>
        public CharacteristicPropertyType Properties => NativeProperties;

        /// <summary>
        /// A human-readable name, taken from the peripheral where it offers one and otherwise from the
        /// Bluetooth SIG's assigned-numbers table.
        /// </summary>
        public string Name => _name ?? (_name = NativeName);

        /// <summary>The service this characteristic belongs to.</summary>
        public Service Service { get; }


        /// <summary>
        /// Forces writes to use a particular acknowledgement mode. Leave it at
        /// <see cref="CharacteristicWriteType.Default"/> to let <see cref="WriteAsync"/> pick from
        /// <see cref="Properties"/>.
        /// </summary>
        public CharacteristicWriteType WriteType
        {
            get => _writeType;
            set
            {
                if (value == CharacteristicWriteType.WithResponse && !Properties.HasFlag(CharacteristicPropertyType.Write) ||
                    value == CharacteristicWriteType.WithoutResponse && !Properties.HasFlag(CharacteristicPropertyType.WriteWithoutResponse))
                {
                    throw new InvalidOperationException($"Write type {value} is not supported");
                }

                _writeType = value;
            }
        }

        /// <summary>Whether <see cref="ReadAsync"/> is supported.</summary>
        public bool CanRead => Properties.HasFlag(CharacteristicPropertyType.Read);

        /// <summary>Whether <see cref="StartUpdatesAsync"/> is supported.</summary>
        public bool CanUpdate => Properties.HasFlag(CharacteristicPropertyType.Notify) |
                                 Properties.HasFlag(CharacteristicPropertyType.Indicate);

        /// <summary>Whether <see cref="WriteAsync"/> is supported.</summary>
        public bool CanWrite => Properties.HasFlag(CharacteristicPropertyType.Write) |
                                Properties.HasFlag(CharacteristicPropertyType.WriteWithoutResponse);

        /// <summary><see cref="Value"/> decoded as UTF-8.</summary>
        public string StringValue
        {
            get
            {
                var val = Value;
                if (val == null)
                    return string.Empty;

                return Encoding.UTF8.GetString(val, 0, val.Length);
            }
        }

        #endregion

        #region Constructors

        private Characteristic(Service service)
        {
            Service = service ?? throw new ArgumentNullException(nameof(service));
        }

        #endregion

        #region Methods


        /// <summary>
        /// Reads the characteristic's value from the peripheral.
        /// </summary>
        /// <exception cref="InvalidOperationException">The characteristic does not support reading.</exception>
        /// <exception cref="CharacteristicReadException">The peripheral rejected the read, or the link dropped.</exception>
        public async Task<byte[]> ReadAsync(CancellationToken cancellationToken = default)
        {
            if (!CanRead)
            {
                throw new InvalidOperationException("Characteristic does not support read.");
            }

            Trace.Message("Characteristic.ReadAsync");

            using (var source = CreateOperationSource(cancellationToken))
            {
                return await ReadNativeAsync(source.Token);
            }
        }

        /// <summary>
        /// Writes a value to the characteristic.
        /// </summary>
        /// <remarks>
        /// Returns <see cref="Task"/> rather than <c>Task&lt;bool&gt;</c> as of 4.0. A write that fails now
        /// throws <see cref="CharacteristicWriteException"/> carrying the platform status, where previously two
        /// platforms returned an unexplained <c>false</c> and the third threw.
        /// </remarks>
        /// <exception cref="InvalidOperationException">The characteristic does not support writing.</exception>
        /// <exception cref="CharacteristicWriteException">The peripheral rejected the write, or the link dropped.</exception>
        public async Task WriteAsync(byte[] data, CancellationToken cancellationToken = default)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (!CanWrite)
            {
                throw new InvalidOperationException("Characteristic does not support write.");
            }

            var writeType = GetWriteType();

            Trace.Message("Characteristic.WriteAsync");

            using (var source = CreateOperationSource(cancellationToken))
            {
                await WriteNativeAsync(data, writeType, source.Token);
            }
        }

        private CharacteristicWriteType GetWriteType()
        {
            if (WriteType != CharacteristicWriteType.Default)
                return WriteType;

            return Properties.HasFlag(CharacteristicPropertyType.Write) ?
                CharacteristicWriteType.WithResponse :
                CharacteristicWriteType.WithoutResponse;
        }

        /// <summary>
        /// Subscribes to server-initiated updates, raising <see cref="ValueUpdated"/> for each one.
        /// </summary>
        /// <param name="mode">
        /// Which mechanism to subscribe with. <see cref="CharacteristicUpdateMode.Auto"/> chooses from
        /// <see cref="Properties"/>.
        /// </param>
        /// <param name="cancellationToken">Abandons the subscription attempt.</param>
        /// <exception cref="InvalidOperationException">
        /// The characteristic supports neither notifications nor indications, or it does not support the
        /// mechanism explicitly asked for.
        /// </exception>
        public async Task StartUpdatesAsync(CharacteristicUpdateMode mode = CharacteristicUpdateMode.Auto, CancellationToken cancellationToken = default)
        {
            if (!CanUpdate)
            {
                throw new InvalidOperationException("Characteristic does not support update.");
            }

            var resolvedMode = ResolveUpdateMode(mode);

            Trace.Message("Characteristic.StartUpdates ({0})", resolvedMode);

            using (var source = CreateOperationSource(cancellationToken))
            {
                await StartUpdatesNativeAsync(resolvedMode, source.Token);
            }
        }

        /// <summary>
        /// Unsubscribes from server-initiated updates.
        /// </summary>
        public async Task StopUpdatesAsync(CancellationToken cancellationToken = default)
        {
            if (!CanUpdate)
            {
                throw new InvalidOperationException("Characteristic does not support update.");
            }

            using (var source = CreateOperationSource(cancellationToken))
            {
                await StopUpdatesNativeAsync(source.Token);
            }
        }

        /// <summary>
        /// Discovers the characteristic's descriptors. The result is cached, so the same
        /// <see cref="Descriptor"/> objects come back on every call.
        /// </summary>
        public async Task<IReadOnlyList<Descriptor>> GetDescriptorsAsync(CancellationToken cancellationToken = default)
        {
            if (_descriptors != null)
            {
                return _descriptors;
            }

            using (var source = CreateOperationSource(cancellationToken))
            {
                var descriptors = await GetDescriptorsNativeAsync(source.Token);

                // Windows returns null rather than an empty list when a characteristic has no descriptors, and
                // caching null would rediscover on every call for exactly the characteristics that have none.
                return _descriptors = descriptors ?? (IReadOnlyList<Descriptor>)Array.Empty<Descriptor>();
            }
        }

        /// <summary>
        /// Finds one of the characteristic's descriptors by UUID, or <see langword="null"/> when it has none.
        /// </summary>
        public async Task<Descriptor> GetDescriptorAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var descriptors = await GetDescriptorsAsync(cancellationToken).ConfigureAwait(false);
            return descriptors.FirstOrDefault(d => d.Id == id);
        }

        /// <summary>
        /// Detaches the notification plumbing without issuing any GATT traffic, for use while the owning
        /// service is being torn down and the link is already gone.
        /// </summary>
        internal void DetachNotifications()
        {
            try
            {
                DetachNotificationsNative();
            }
            catch (Exception ex)
            {
                Trace.Message("Characteristic: exception while detaching notifications for {0}: {1}", Id, ex.Message);
            }
        }

        /// <summary>
        /// Links the caller's token to the owning device's, so that <see cref="Device.ClearServices"/> and
        /// disposal cancel GATT work that is already in flight rather than leaving it to time out.
        /// </summary>
        private CancellationTokenSource CreateOperationSource(CancellationToken cancellationToken)
            => Service.Device.GetCombinedSource(cancellationToken);

        /// <summary>
        /// Turns <see cref="CharacteristicUpdateMode.Auto"/> into a concrete mechanism, and rejects an explicit
        /// one the peripheral does not advertise.
        /// </summary>
        private CharacteristicUpdateMode ResolveUpdateMode(CharacteristicUpdateMode mode)
        {
            switch (mode)
            {
                case CharacteristicUpdateMode.Auto:
                    // Indicate only when it is the sole mechanism on offer. A characteristic advertising both
                    // ended up subscribed by notification before 4.0, because the notify descriptor write came
                    // second and overwrote the indicate one.
                    return Properties.HasFlag(CharacteristicPropertyType.Indicate) && !Properties.HasFlag(CharacteristicPropertyType.Notify)
                        ? CharacteristicUpdateMode.Indicate
                        : CharacteristicUpdateMode.Notify;

                case CharacteristicUpdateMode.Notify when !Properties.HasFlag(CharacteristicPropertyType.Notify):
                    throw new InvalidOperationException("Characteristic does not support notifications.");

                case CharacteristicUpdateMode.Indicate when !Properties.HasFlag(CharacteristicPropertyType.Indicate):
                    throw new InvalidOperationException("Characteristic does not support indications.");

                default:
                    return mode;
            }
        }

        /// <summary>
        /// Implemented by each platform partial to unsubscribe the internal notification forwarder. No-op on
        /// targets with no notification source of their own.
        /// </summary>
        partial void DetachNotificationsNative();

        #endregion
    }
}
