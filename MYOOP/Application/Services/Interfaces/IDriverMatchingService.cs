﻿using OOP.Domain.Entities;

namespace OOP.Application.Services.Interfaces
{
    public interface IDriverMatchingService
    {
        Task<Driver?> FindActiveDriver(
            GeoLocation pickup,
            string VehicleType,
            IEnumerable<Guid>? excludedDriverIds = null);

        /// <summary>
        /// Tìm và reserve một tài xế một cách atomic (tránh race condition)
        /// </summary>
        Task<Driver?> FindAndReserveDriver(
            GeoLocation pickup,
            string VehicleType,
            IEnumerable<Guid>? excludedDriverIds = null,
            int retryCount = 0);

        Task<Driver?> MatchDriver(Trip trip);
        Task<List<Driver>> GetNearbyDrivers(GeoLocation pickup, string VehicleType, double maxKm);
    }
}
