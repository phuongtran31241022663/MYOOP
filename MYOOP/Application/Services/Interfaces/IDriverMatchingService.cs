﻿using OOP.Domain.Entities;
using OOP.Domain.Enums;

namespace OOP.Application.Services.Interfaces
{
    public interface IDriverMatchingService
    {
        Task<Driver?> FindAvailableDriver(
            Location pickup,
            VehicleType vehicleType,
            IEnumerable<Guid>? excludedDriverIds = null);

        Task<Driver?> MatchDriver(Trip trip);
        Task<List<Driver>> GetNearbyDrivers(Location pickup, VehicleType vehicleType, double maxKm);
    }
}
