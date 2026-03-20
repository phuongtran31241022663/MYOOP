﻿using OOP.Domain.Entities;
using OOP.Domain.Enums;

namespace OOP.Application.Services.Interfaces
{
    public interface ITripService
    {
        Task<Trip> RequestTrip(
            Guid passengerId,
            GeoLocation pickup,
            GeoLocation destination,
            VehicleType vehicleType);

        Task AssignDriver(Guid tripId, Guid driverId);
        Task RejectTrip(Guid tripId, Guid driverId, string reason);
        Task MarkArrived(Guid tripId);
        Task StartTrip(Guid tripId);
        Task CompleteTrip(Guid tripId);
        Task CancelTrip(Guid tripId, string reason);
        Task<Trip?> GetTrip(Guid tripId);
        Task<List<Trip>> GetTripHistory(Guid userId);
        Task<List<Trip>> GetByUserId(Guid userId);
        Task<List<Trip>> GetAvailableTripsForDriver(Guid driverId);
        Task<List<Driver>> GetNearbyDrivers(GeoLocation pickup, VehicleType vehicleType, double maxKm);
        Task<Driver?> GetDriverForTrip(Guid tripId);
        Task<int> ExpireSearchingTrips(TimeSpan maxWait);
    }

}

