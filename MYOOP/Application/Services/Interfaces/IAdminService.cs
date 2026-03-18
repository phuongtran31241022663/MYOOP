﻿using OOP.Domain.Entities;
using OOP.Application.Services.Models;

namespace OOP.Application.Services.Interfaces
{
    public interface IAdminService
    {
        Task<List<User>> GetAllUsers();
        Task<List<Trip>> GetAllTrips();
        Task<List<Fare>> GetFareRules();
        Task<Fare> CreateFareRule(Fare rule);
        Task UpdateFareRule(Fare rule);
        Task ActivateUser(Guid userId);
        Task DeactivateUser(Guid targetUserId, Guid currentAdminId);
        Task<TripReport> GetTripReport();
    }
}

