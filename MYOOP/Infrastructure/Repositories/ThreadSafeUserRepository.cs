using OOP.Domain.Entities;
using OOP.Domain.Interfaces;
using OOP.Domain.Enums;

namespace OOP.Infrastructure.Repositories
{
    // Thread-Safe User Repository - giải quyết vấn đề concurrency
    // Uses single lock for all operations (both reads and writes)
    public class ThreadSafeUserRepository : IUserRepository, IDriverRepository, ICacheRefreshable
    {
        private readonly IUserRepository _innerRepository;
        private readonly SemaphoreSlim _lock = new(1, 1);

        public ThreadSafeUserRepository(IUserRepository innerRepository)
        {
            _innerRepository = innerRepository ?? throw new ArgumentNullException(nameof(innerRepository));
        }

        public async Task<List<User>> GetAll()
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

        public async Task<User?> GetById(Guid userId)
        {
            await _lock.WaitAsync();
            try
            {
                return await _innerRepository.GetById(userId);
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task<User?> GetByPhone(string phone)
        {
            await _lock.WaitAsync();
            try
            {
                return await _innerRepository.GetByPhone(phone);
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task<bool> ExistsByPhone(string phone)
        {
            await _lock.WaitAsync();
            try
            {
                return await _innerRepository.ExistsByPhone(phone);
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task<List<Driver>> GetActiveDrivers(VehicleType VehicleType)
        {
            await _lock.WaitAsync();
            try
            {
                return await ((IDriverRepository)_innerRepository).GetActiveDrivers(VehicleType);
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task<Driver?> TryReserveDriver(VehicleType VehicleType)
        {
            await _lock.WaitAsync();
            try
            {
                return await ((IDriverRepository)_innerRepository).TryReserveDriver(VehicleType);
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task Add(User user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            await _lock.WaitAsync();
            try
            {
                await _innerRepository.Add(user);
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task Update(User user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            await _lock.WaitAsync();
            try
            {
                await _innerRepository.Update(user);
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task Remove(Guid userId)
        {
            await _lock.WaitAsync();
            try
            {
                await _innerRepository.Remove(userId);
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <summary>
        /// Cập nhật location của driver mà không ghi đè các trường khác (bao gồm Status)
        /// </summary>
        public async Task UpdateDriverLocation(Guid driverId, GeoLocation location)
        {
            await _lock.WaitAsync();
            try
            {
                await ((IDriverRepository)_innerRepository).UpdateDriverLocation(driverId, location);
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
