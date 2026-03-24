﻿using OOP.Infrastructure.Storage;
using OOP.Domain.Interfaces;

namespace OOP.Infrastructure.Repositories
{
    public abstract class BaseRepository<T> : ICacheRefreshable where T : class
    {
        protected List<T> Items = new();

        protected readonly IStorage Storage;

        protected readonly string FileName;
        // Static SemaphoreSlim shared across all instances - prevents race condition without DB
        private static readonly SemaphoreSlim _globalWriteLock = new(1, 1);
        private readonly SemaphoreSlim _instanceWriteLock = new(1, 1);
        private readonly SemaphoreSlim _loadLock = new(1, 1);
        private volatile bool _isLoaded = false;

        protected BaseRepository(IStorage storage, string fileName)
        {
            Storage = storage ?? throw new ArgumentNullException(nameof(storage));
            FileName = fileName;
        }

        /// <summary>
        /// Gets the write lock. Override in subclass to use instance lock, or use static for global lock.
        /// </summary>
        protected virtual SemaphoreSlim WriteLock => _instanceWriteLock;

        protected async Task EnsureLoaded()
        {
            if (_isLoaded) return;

            bool lockAcquired = false;
            try
            {
                await _loadLock.WaitAsync();
                lockAcquired = true;
                if (_isLoaded) return;
                var loaded = await Storage.LoadAsync<List<T>>(FileName);
                Items = loaded ?? new List<T>();
                _isLoaded = true;
            }
            catch
            {
                throw;
            }
            finally
            {
                try { if (lockAcquired) _loadLock.Release(); }
                catch { /* ignore */ }
            }
        }

        protected async Task Save()
        {
            await Storage.SaveAsync(FileName, Items);
        }

        /// <summary>
        /// Force reload data from storage to refresh cache.
        /// </summary>
        public async Task RefreshCacheAsync()
        {
            _isLoaded = false;
            await EnsureLoaded();
        }
    }
}