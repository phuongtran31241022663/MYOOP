﻿using OOP.Domain.Entities;

namespace OOP.Domain.Interfaces
{
    public interface IRatingRepository
    {
        Task<List<Rating>> GetAll();
        Task<Rating?> GetByTripId(Guid tripId);
        Task<List<Rating>> GetByDriverId(Guid driverId);
        Task<List<Rating>> GetByPassengerId(Guid passengerId);
        Task<bool> ExistsForTrip(Guid tripId);
        Task Add(Rating rating);
        Task Update(Rating rating);
    }
}
