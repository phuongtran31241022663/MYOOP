﻿using OOP.Infrastructure.Storage;
using OOP.Domain.Entities;
using OOP.Domain.Enums;
using OOP.Domain.Interfaces;

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

        public async Task<List<Driver>> GetActiveDrivers(string VehicleType)
        {
            await EnsureLoaded();
            return Items.OfType<Driver>()
                .Where(d => d.Status == DriverStatus.Active)
                .Where(d => d.Vehicle != null && string.Equals(d.Vehicle.GetVehicleType(), VehicleType, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public async Task<Driver?> TryReserveDriver(string VehicleType)
        {
            await EnsureLoaded();
            await WriteLock.WaitAsync();
            try
            {
                // Tìm tài xế Active đầu tiên với vehicle type phù hợp
                var driver = Items.OfType<Driver>()
                    .FirstOrDefault(d =>
                        d.Status == DriverStatus.Active &&
                        d.Vehicle != null &&
                        string.Equals(d.Vehicle.GetVehicleType(), VehicleType, StringComparison.OrdinalIgnoreCase));

                if (driver == null)
                {
                    System.Diagnostics.Debug.WriteLine("[TryReserveDriver] No Active drivers found");
                    return null;
                }

                // CRITICAL: Guard check - verify driver is still Active before setting OnTrip
                // This prevents double reservation and ensures atomic operation
                if (driver.Status != DriverStatus.Active)
                {
                    System.Diagnostics.Debug.WriteLine($"[TryReserveDriver] Driver {driver.Name} no longer Active (status: {driver.Status})");
                    return null;
                }

                try
                {
                    driver.SetOnTrip();
                }
                catch (InvalidOperationException ex)
                {
                    // Should not happen due to guard above, but handle gracefully
                    System.Diagnostics.Debug.WriteLine($"[TryReserveDriver] Failed to set OnTrip: {ex.Message}");
                    return null;
                }
                
                // Save the status change to storage
                await Save();
                
                System.Diagnostics.Debug.WriteLine($"[TryReserveDriver] Successfully reserved driver {driver.Name} (ID: {driver.Id})");
                return driver;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TryReserveDriver] Error: {ex.Message}");
                return null;
            }
            finally
            {
                WriteLock.Release();
            }
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

        /// <summary>
        /// Cập nhật location của driver mà không ghi đè các trường khác (bao gồm Status)
        /// </summary>
        public async Task UpdateDriverLocation(Guid driverId, GeoLocation location)
        {
            await EnsureLoaded();

            await WriteLock.WaitAsync();
            try
            {
                var driver = Items.OfType<Driver>().FirstOrDefault(d => d.Id == driverId);
                if (driver == null)
                    throw new KeyNotFoundException($"Không tìm thấy driver với Id '{driverId}'.");

                // Sử dụng method UpdateLocation để chỉ cập nhật Position, giữ nguyên các trường khác
                driver.UpdateLocation(location);

                await Save();
            }
            finally
            {
                WriteLock.Release();
            }
        }
    }
}