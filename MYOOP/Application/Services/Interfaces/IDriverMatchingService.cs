﻿using OOP.Domain.Entities;
using OOP.Domain.Enums;

namespace OOP.Application.Services.Interfaces
{
    public interface IDriverMatchingService
    {
        Task<Driver?> FindActiveDriver(
            GeoLocation pickup,
            VehicleType VehicleType,
            IEnumerable<Guid>? excludedDriverIds = null);

        Task<List<Driver>> FindAvailableDrivers(GeoLocation pickup, VehicleType vehicleType, IEnumerable<Guid>? excludedDriverIds = null);

        Task<Driver?> MatchDriver(Trip trip);
        Task<List<Driver>> GetNearbyDrivers(GeoLocation pickup, VehicleType VehicleType, double maxKm);

        /// <summary>
        /// Gửi request tuần tự: tìm tài xế gần nhất (loại trừ rejected) và trả về.
        /// Không thay đổi state — chỉ trả về tài xế để caller gửi thông báo.
        /// </summary>
        Task<Driver?> DispatchToNearestDriver(Trip trip);
    }
}
