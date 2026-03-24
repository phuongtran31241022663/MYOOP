﻿using OOP.Domain.Entities;

namespace OOP.Domain.Interfaces
{
    public interface IFareRepository
    {
        Task EnsureSeeded();
        Task<List<Fare>> GetAll();
        Task<Fare?> GetById(Guid id);
        Task<Fare?> GetByVehicleType(string VehicleType);
        Task Add(Fare rule);
        Task Update(Fare rule);
        Task Remove(Guid id);
    }
}