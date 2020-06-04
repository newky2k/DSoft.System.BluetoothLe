using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.BluetoothLe.Contracts;

namespace System.BluetoothLe
{
    public partial class Service : IService
    {
        private readonly List<ICharacteristic> _characteristics = new List<ICharacteristic>();

        public string Name => KnownServices.Lookup(Id).Name;

        public Guid Id => NativeGuid;

        public bool IsPrimary => NativeIsPrimary;

        public IDevice Device { get; }

        protected Service(IDevice device)
        {
            Device = device;
        }

        public async Task<IReadOnlyList<ICharacteristic>> GetCharacteristicsAsync()
        {
            if (!_characteristics.Any())
            {
                _characteristics.AddRange(await GetCharacteristicsNativeAsync());
            }

            // make a copy here so that the caller cant modify the original list
            return _characteristics.ToList();
        }

        public async Task<ICharacteristic> GetCharacteristicAsync(Guid id)
        {
            var characteristics = await GetCharacteristicsAsync();
            return characteristics.FirstOrDefault(c => c.Id == id);
        }

    }
}
