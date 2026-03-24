﻿using OOP.Domain.Entities;
using OOP.Domain.Enums;
namespace OOP.Domain.Interfaces
{
    public interface IUserRepository
    {
        Task<List<User>> GetAll();
        Task<List<Driver>> GetAvailableDrivers(VehicleType type);
        Task<User?> GetById(Guid userId);
        Task<User?> GetByPhone(string phone);
        Task<bool> ExistsByPhone(string phone);
        Task Add(User user);
        Task Update(User user);
        Task Remove(Guid userId);
    }
}
