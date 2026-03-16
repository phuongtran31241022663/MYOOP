﻿using OOP.Domain.Entities;

namespace OOP.Domain.Interfaces
{
    public interface ITripRepository
    {
        Task<Trip?> GetById(Guid tripId);
        Task<List<Trip>> GetByPassengerId(Guid passengerId);
        Task<List<Trip>> GetByDriverId(Guid driverId);
        Task<List<Trip>> GetAll();
        Task Add(Trip trip);
        Task Update(Trip trip);
        Task Remove(Guid tripId);
        Task<List<Trip>> GetByUserId(Guid Id);
    }
}
