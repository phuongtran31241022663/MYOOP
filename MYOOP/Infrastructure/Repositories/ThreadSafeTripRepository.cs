using OOP.Domain.Entities;
using OOP.Domain.Interfaces;
using System.Collections.Concurrent;

namespace OOP.Infrastructure.Repositories
{
    /// <summary>
    /// Decorator thread-safe cho ITripRepository using ConcurrentDictionary.
    /// Option B: Dùng collection thread-safe thay vì ReaderWriterLockSlim để tránh
    /// SynchronizationLockException do nested lock hoặc lock state corruption.
    /// </summary>
    public sealed class ThreadSafeTripRepository : ITripRepository, ICacheRefreshable, IDisposable
    {
        private readonly ITripRepository _inner;
        private readonly ConcurrentDictionary<Guid, Trip> _tripsCache = new();
        private readonly SemaphoreSlim _cacheLock = new(1, 1);
        private bool _disposed;

        public ThreadSafeTripRepository(ITripRepository inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        // ── Read operations — ConcurrentDictionary provides thread-safety ─────────

        public async Task<Trip?> GetById(Guid id)
        {
            ThrowIfDisposed();

            if (_tripsCache.TryGetValue(id, out var cachedTrip))
                return cachedTrip;

            var trip = await _inner.GetById(id);
            if (trip != null)
            {
                _tripsCache[trip.Id] = trip;
            }

            return trip;
        }

        public async Task<List<Trip>> GetAll()
        {
            // If cache is empty, load from inner repository first
            if (_tripsCache.IsEmpty)
            {
                await LoadCacheFromInner();
            }
            return await ReadAsync(() => _tripsCache.Values.ToList());
        }

        public async Task<List<Trip>> GetByPassengerId(Guid id)
        {
            if (_tripsCache.IsEmpty)
                await LoadCacheFromInner();
            return await ReadAsync(() =>
                _tripsCache.Values.Where(t => t.PassengerId == id).ToList());
        }

        public async Task<List<Trip>> GetByDriverId(Guid id)
        {
            if (_tripsCache.IsEmpty)
                await LoadCacheFromInner();
            return await ReadAsync(() =>
                _tripsCache.Values.Where(t => t.DriverId == id).ToList());
        }

        public async Task<List<Trip>> GetByUserId(Guid id)
        {
            if (_tripsCache.IsEmpty)
                await LoadCacheFromInner();
            return await ReadAsync(() =>
                _tripsCache.Values
                    .Where(t => t.PassengerId == id || t.DriverId == id)
                    .ToList());
        }

        // ── Write operations — ConcurrentDictionary provides thread-safety ─────────

        public async Task Add(Trip trip)
        {
            if (trip == null) throw new ArgumentNullException(nameof(trip));
            await _inner.Add(trip);
            // Update cache after inner add succeeds
            _tripsCache[trip.Id] = trip;
        }

        public async Task Update(Trip trip)
        {
            if (trip == null) throw new ArgumentNullException(nameof(trip));
            await _inner.Update(trip);
            // Update cache after inner update succeeds
            _tripsCache[trip.Id] = trip;
        }

        public async Task Remove(Guid id)
        {
            await _inner.Remove(id);
            // Remove from cache after inner remove succeeds
            _tripsCache.TryRemove(id, out _);
        }

        // ── ICacheRefreshable ─────────────────────────────────────────────────

        public async Task RefreshCacheAsync()
        {
            if (_inner is ICacheRefreshable cr)
            {
                await cr.RefreshCacheAsync();
                // Reload cache from inner repository
                await _cacheLock.WaitAsync();
                try
                {
                    await LoadCacheFromInnerInternal();
                }
                finally
                {
                    _cacheLock.Release();
                }
            }
        }

        // ── Cache helpers ──────────────────────────────────────────────────────

        private async Task LoadCacheFromInner()
        {
            if (!_tripsCache.IsEmpty) return;
            await _cacheLock.WaitAsync();
            try
            {
                if (!_tripsCache.IsEmpty) return;
                await LoadCacheFromInnerInternal();
            }
            finally
            {
                _cacheLock.Release();
            }
        }

        private async Task LoadCacheFromInnerInternal()
        {
            var trips = await _inner.GetAll();
            _tripsCache.Clear();
            foreach (var trip in trips)
            {
                _tripsCache[trip.Id] = trip;
            }
        }

        // ── Lock helpers (simplified - no ReaderWriterLockSlim needed) ────────

        private async Task<T> ReadAsync<T>(Func<T> action)
        {
            ThrowIfDisposed();
            // ConcurrentDictionary is thread-safe, no lock needed
            return await Task.Run(action);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(ThreadSafeTripRepository));
        }

        // ── IDisposable ───────────────────────────────────────────────────────

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (_inner is IDisposable d) d.Dispose();
            _cacheLock.Dispose();
        }
    }
}
