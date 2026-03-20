using OOP.Domain.Entities;
using OOP.Domain.Interfaces;
using OOP.Infrastructure.Storage;

namespace OOP.Infrastructure.Repositories
{
    // Thread-Safe Trip Repository - giải quyết vấn đề concurrency
    // Uses single lock for all operations (both reads and writes)
    public class ThreadSafeTripRepository : ITripRepository, ICacheRefreshable
    {
        private readonly ITripRepository _innerRepository;
        private readonly SemaphoreSlim _lock = new(1, 1);

        public ThreadSafeTripRepository(ITripRepository innerRepository)
        {
            _innerRepository = innerRepository ?? throw new ArgumentNullException(nameof(innerRepository));
        }

        public async Task Add(Trip trip)
        {
            if (trip == null) throw new ArgumentNullException(nameof(trip));

            await _lock.WaitAsync();
            try
            {
                await _innerRepository.Add(trip);
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task Update(Trip trip)
        {
            if (trip == null) throw new ArgumentNullException(nameof(trip));

            await _lock.WaitAsync();
            try
            {
                await _innerRepository.Update(trip);
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task Remove(Guid tripId)
        {
            await _lock.WaitAsync();
            try
            {
                await _innerRepository.Remove(tripId);
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task<Trip?> GetById(Guid tripId)
        {
            await _lock.WaitAsync();
            try
            {
                return await _innerRepository.GetById(tripId);
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task<List<Trip>> GetAll()
        {
            await _lock.WaitAsync();
            try
            {
                return await _innerRepository.GetAll();
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task<List<Trip>> GetByPassengerId(Guid passengerId)
        {
            await _lock.WaitAsync();
            try
            {
                return await _innerRepository.GetByPassengerId(passengerId);
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task<List<Trip>> GetByDriverId(Guid driverId)
        {
            await _lock.WaitAsync();
            try
            {
                return await _innerRepository.GetByDriverId(driverId);
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task<List<Trip>> GetByUserId(Guid userId)
        {
            await _lock.WaitAsync();
            try
            {
                return await _innerRepository.GetByUserId(userId);
            }
            finally
            {
                _lock.Release();
            }
        }

        // Implement ICacheRefreshable - forward to inner repository
        public async Task RefreshCacheAsync()
        {
            await _lock.WaitAsync();
            try
            {
                if (_innerRepository is ICacheRefreshable inner)
                    await inner.RefreshCacheAsync();
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}
