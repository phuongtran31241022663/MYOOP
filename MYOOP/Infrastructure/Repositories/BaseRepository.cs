﻿using OOP.Infrastructure.Storage;

namespace OOP.Infrastructure.Repositories
{
    public abstract class BaseRepository<T> where T : class
    {
        protected List<T> Items = new();

        protected readonly IStorage Storage;

        protected readonly string FileName;
        protected readonly SemaphoreSlim WriteLock = new(1, 1);
        private readonly SemaphoreSlim _loadLock = new(1, 1);
        private volatile bool _isLoaded = false;

        protected BaseRepository(IStorage storage, string fileName)
        {
            Storage = storage ?? throw new ArgumentNullException(nameof(storage));
            FileName = fileName;
        }

        protected async Task EnsureLoaded()
        {
            if (_isLoaded) return;

            await _loadLock.WaitAsync();
            try
            {
                if (_isLoaded) return;
                var loaded = await Storage.LoadAsync<List<T>>(FileName);
                Items = loaded ?? new List<T>();
                _isLoaded = true;
            }
            finally
            {
                _loadLock.Release();
            }
        }

        protected async Task Save()
        {
            await Storage.SaveAsync(FileName, Items);
        }
    }
}