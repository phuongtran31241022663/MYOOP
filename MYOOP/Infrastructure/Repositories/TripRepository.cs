using OOP.Domain.Entities;
using OOP.Domain.Interfaces;
using OOP.Infrastructure.Storage;

namespace OOP.Infrastructure.Repositories
{
    public class TripRepository : BaseRepository<Trip>, ITripRepository
    {
        public TripRepository(IStorage storage)
            : base(storage, "trips.json") { }

        public async Task<List<Trip>> GetAll()
        {
            await EnsureLoaded();
            return new List<Trip>(Items);
        }

        public async Task<Trip?> GetById(Guid tripId)
        {
            await EnsureLoaded();
            return Items.FirstOrDefault(t => t.Id == tripId);
        }

        public async Task<List<Trip>> GetByPassengerId(Guid passengerId)
        {
            await EnsureLoaded();
            return Items.Where(t => t.PassengerId == passengerId).ToList();
        }

        public async Task<List<Trip>> GetByDriverId(Guid driverId)
        {
            await EnsureLoaded();
            return Items.Where(t => t.DriverId == driverId).ToList();
        }
        public async Task<List<Trip>> GetByUserId(Guid userId)
        {
            await EnsureLoaded();
            return Items
                .Where(t => t.PassengerId == userId || t.DriverId == userId)
                .ToList();
        }

        public async Task Add(Trip trip)
        {
            if (trip == null) throw new ArgumentNullException(nameof(trip));

            await EnsureLoaded();

            await WriteLock.WaitAsync();
            try
            {
                if (Items.Any(t => t.Id == trip.Id))
                    throw new InvalidOperationException($"Trip với Id '{trip.Id}' đã tồn tại.");

                Items.Add(trip);
                await Save();
            }
            finally
            {
                WriteLock.Release();
            }
        }

        public async Task Update(Trip trip)
        {
            if (trip == null) throw new ArgumentNullException(nameof(trip));

            await EnsureLoaded();

            await WriteLock.WaitAsync();
            try
            {
                var index = Items.FindIndex(t => t.Id == trip.Id);

                if (index == -1)
                    throw new KeyNotFoundException($"Không tìm thấy trip với Id '{trip.Id}'.");

                Items[index] = trip;
                await Save();
            }
            finally
            {
                WriteLock.Release();
            }
        }

        public async Task Remove(Guid tripId)
        {
            await EnsureLoaded();

            await WriteLock.WaitAsync();
            try
            {
                var count = Items.RemoveAll(t => t.Id == tripId);

                if (count == 0)
                    throw new KeyNotFoundException($"Không tìm thấy trip với Id '{tripId}'.");

                await Save();
            }
            finally
            {
                WriteLock.Release();
            }
        }
    }
}