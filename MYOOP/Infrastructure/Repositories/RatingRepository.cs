using OOP.Domain.Entities;
using OOP.Domain.Interfaces;
using OOP.Infrastructure.Storage;

namespace OOP.Infrastructure.Repositories
{
    public class RatingRepository : BaseRepository<Rating>, IRatingRepository
    {
        public RatingRepository(IStorage storage)
            : base(storage, "ratings.json") { }

        public async Task<List<Rating>> GetAll()
        {
            await EnsureLoaded();
            return new List<Rating>(Items);
        }

        public async Task<Rating?> GetByTripId(Guid tripId)
        {
            await EnsureLoaded();
            return Items.FirstOrDefault(r => r.TripId == tripId);
        }

        public async Task<List<Rating>> GetByDriverId(Guid driverId)
        {
            await EnsureLoaded();
            return Items.Where(r => r.DriverId == driverId).ToList();
        }

        public async Task<List<Rating>> GetByPassengerId(Guid passengerId)
        {
            await EnsureLoaded();
            return Items.Where(r => r.PassengerId == passengerId).ToList();
        }

        public async Task<bool> ExistsForTrip(Guid tripId)
        {
            await EnsureLoaded();
            return Items.Any(r => r.TripId == tripId);
        }

        public async Task Add(Rating rating)
        {
            if (rating == null) throw new ArgumentNullException(nameof(rating));

            await EnsureLoaded();

            await WriteLock.WaitAsync();
            try
            {
                if (Items.Any(r => r.TripId == rating.TripId))
                    throw new InvalidOperationException(
                        $"Chuyến đi '{rating.TripId}' đã được đánh giá trước đó.");

                Items.Add(rating);
                await Save();
            }
            finally
            {
                WriteLock.Release();
            }
        }

        public async Task Update(Rating rating)
        {
            if (rating == null) throw new ArgumentNullException(nameof(rating));

            await EnsureLoaded();

            await WriteLock.WaitAsync();
            try
            {
                var index = Items.FindIndex(r => r.Id == rating.Id);

                if (index == -1)
                    throw new KeyNotFoundException(
                        $"Không tìm thấy rating với Id '{rating.Id}'.");

                Items[index] = rating;
                await Save();
            }
            finally
            {
                WriteLock.Release();
            }
        }
        public async Task<bool> Exists(Guid id)
        {
            await EnsureLoaded();
            return Items.Any(r => r.Id == id);
        }
    }
}