﻿using OOP.Domain.Entities;
namespace OOP.Domain.Interfaces
{
    public interface IUserRepository
    {
        Task<List<User>> GetAll();
        Task<List<Driver>> GetActiveDrivers(string VehicleType);
        Task<Driver?> TryReserveDriver(string VehicleType);
        Task<User?> GetById(Guid userId);
        Task<User?> GetByPhone(string phone);
        Task<bool> ExistsByPhone(string phone);
        Task Add(User user);
        Task Update(User user);
        Task Remove(Guid userId);
        // Cập nhật location riêng - không ghi đè các trường khác
        Task UpdateDriverLocation(Guid driverId, GeoLocation location);
    }
}
