using OOP.Domain.Entities;
using OOP.Domain.Interfaces;
using OOP.Infrastructure.Storage;

namespace OOP.Infrastructure.Repositories
{
    public class VehicleRepository : BaseRepository<Vehicle>, IVehicleRepository
    {
        public VehicleRepository(IStorage storage)
            : base(storage, "vehicles.json") { }

        public async Task<List<Vehicle>> GetAll()
        {
            await EnsureLoaded();
            return new List<Vehicle>(Items);
        }

        public async Task<Vehicle?> GetById(Guid id)
        {
            await EnsureLoaded();
            return Items.FirstOrDefault(v => v.Id == id);
        }

        public async Task<Vehicle?> GetByDriverId(Guid driverId)
        {
            await EnsureLoaded();
            return Items.FirstOrDefault(v => v.DriverId == driverId);
        }

        public async Task Add(Vehicle vehicle)
        {
            if (vehicle == null) throw new ArgumentNullException(nameof(vehicle));

            await EnsureLoaded();

            await WriteLock.WaitAsync();
            try
            {
                if (Items.Any(v => v.Id == vehicle.Id))
                    throw new InvalidOperationException($"Vehicle với Id '{vehicle.Id}' đã tồn tại.");

                Items.Add(vehicle);
                await Save();
            }
            finally
            {
                WriteLock.Release();
            }
        }

        public async Task Update(Vehicle vehicle)
        {
            if (vehicle == null) throw new ArgumentNullException(nameof(vehicle));

            await EnsureLoaded();

            await WriteLock.WaitAsync();
            try
            {
                var index = Items.FindIndex(v => v.Id == vehicle.Id);

                if (index == -1)
                    throw new KeyNotFoundException($"Không tìm thấy vehicle với Id '{vehicle.Id}'.");

                Items[index] = vehicle;
                await Save();
            }
            finally
            {
                WriteLock.Release();
            }
        }

        public async Task Delete(Guid vehicleId)
        {
            await EnsureLoaded();

            await WriteLock.WaitAsync();
            try
            {
                var count = Items.RemoveAll(v => v.Id == vehicleId);

                if (count == 0)
                    throw new KeyNotFoundException($"Không tìm thấy vehicle với Id '{vehicleId}'.");

                await Save();
            }
            finally
            {
                WriteLock.Release();
            }
        }
    }
}