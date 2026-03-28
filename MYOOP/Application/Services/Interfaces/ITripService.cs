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
            VehicleType VehicleType);

        Task<bool> TryAssignDriver(Guid tripId, Guid driverId);
        /// <summary>
        /// Gán tài xế (phiên bản cũ)
        /// </summary>
        [Obsolete("Use TryAssignDriver instead")]
        Task AssignDriver(Guid tripId, Guid driverId);
        Task RejectTrip(Guid tripId, Guid driverId, string reason);
        Task MarkArrived(Guid tripId);
        Task StartTrip(Guid tripId);
        Task CompleteTrip(Guid tripId);
        /// <summary>
        /// Driver xác nhận đã nhận tiền mặt từ khách hàng.
        /// </summary>
        Task ConfirmPayment(Guid tripId, decimal actualFare);
        Task CancelTrip(Guid tripId, string reason);
        Task<Trip?> GetTrip(Guid tripId);
        Task<List<Trip>> GetTripHistory(Guid userId);
        Task<List<Trip>> GetByUserId(Guid userId);
        Task<List<Trip>> GetActiveTripsForDriver(Guid driverId);
        Task<List<Driver>> GetNearbyDrivers(GeoLocation pickup, VehicleType VehicleType, double maxKm);
        Task<Driver?> GetDriverForTrip(Guid tripId);
        Task<int> ExpireSearchingTrips(TimeSpan maxWait);
        /// <summary>
        /// Expire các trip đã được gán tài xế nhưng tài xế không phản hồi trong thời gian quy định.
        /// </summary>
        Task<int> ExpireMatchedTrips(TimeSpan maxWait);
    }

}

