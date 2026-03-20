﻿using OOP.Infrastructure.Storage;
using OOP.Domain.Entities;
using OOP.Domain.Interfaces;
using OOP.Domain.Enums;

namespace OOP.Infrastructure.Repositories
{
    public class UserRepository : BaseRepository<User>, IUserRepository
    {
        public UserRepository(IStorage storage)
              : base(storage, "users.json") { }

        public async Task<List<User>> GetAll()
        {
            await EnsureLoaded();
            return new List<User>(Items);
        }

        public async Task<User?> GetById(Guid userId)
        {
            await EnsureLoaded();
            return Items.FirstOrDefault(u => u.Id == userId);
        }

        public async Task<User?> GetByPhone(string phone)
        {
            await EnsureLoaded();
            var trimmed = phone.Trim();
            return Items.FirstOrDefault(u => u.Phone.Trim() == trimmed);
        }

        public async Task<bool> ExistsByPhone(string phone)
        {
            await EnsureLoaded();
            var trimmed = phone.Trim();
            return Items.Any(u => u.Phone.Trim() == trimmed);
        }

        public async Task<List<Driver>> GetAvailableDrivers(VehicleType type)
        {
            await EnsureLoaded();
            return Items.OfType<Driver>()
                .Where(d => d.Status == DriverStatus.Available)
                .Where(d => d.Vehicle != null && d.Vehicle.Type == type)
                .ToList();
        }

        public async Task Add(User user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            await EnsureLoaded();

            await WriteLock.WaitAsync();
            try
            {
                if (Items.Any(u => u.Phone.Trim() == user.Phone.Trim()))
                    throw new InvalidOperationException($"Số điện thoại '{user.Phone}' đã được đăng ký.");

                Items.Add(user);
                await Save();
            }
            finally
            {
                WriteLock.Release();
            }
        }

        public async Task Update(User user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            await EnsureLoaded();

            await WriteLock.WaitAsync();
            try
            {
                var index = Items.FindIndex(u => u.Id == user.Id);

                if (index == -1)
                    throw new KeyNotFoundException($"Không tìm thấy user với Id '{user.Id}'.");

                Items[index] = user;
                await Save();
            }
            finally
            {
                WriteLock.Release();
            }
        }

        public async Task Remove(Guid userId)
        {
            await EnsureLoaded();

            await WriteLock.WaitAsync();
            try
            {
                var count = Items.RemoveAll(u => u.Id == userId);

                if (count == 0)
                    throw new KeyNotFoundException($"Không tìm thấy user với Id '{userId}'.");

                await Save();
            }
            finally
            {
                WriteLock.Release();
            }
        }
    }
}