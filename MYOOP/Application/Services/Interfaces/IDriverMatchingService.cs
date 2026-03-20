﻿using OOP.Domain.Entities;
using OOP.Domain.Enums;

namespace OOP.Application.Services.Interfaces
{
    public interface IDriverMatchingService
    {
        Task<Driver?> FindAvailableDriver(
            GeoLocation pickup,
            VehicleType vehicleType,
            IEnumerable<Guid>? excludedDriverIds = null);

        Task<Driver?> MatchDriver(Trip trip);
        Task<List<Driver>> GetNearbyDrivers(GeoLocation pickup, VehicleType vehicleType, double maxKm);
    }
}
