﻿using OOP.Domain.Entities;

namespace OOP.Application.Services.Interfaces
{
    public interface IRatingService
    {
        Task<Rating> CreateRating(Guid tripId, Guid passengerId, int score, string comment);

        Task<Rating?> GetRatingByTrip(Guid tripId);

        Task<List<Rating>> GetRatingsByDriver(Guid driverId);
    }
}