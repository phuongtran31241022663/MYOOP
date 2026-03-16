﻿using OOP.Domain.Entities;

namespace OOP.Application.Interfaces
{
    public interface ISimulationService
    {
        // Cập nhật vị trí tất cả tài xế đang Available/Busy
        Task UpdateDriverLocations();

        // Di chuyển ngẫu nhiên một tài xế theo bước nhỏ
        Task SimulateDriverMovement(Guid driverId);

        // Tự động tiến trình một trip đang Ongoing
        Task SimulateTripProgress(Guid tripId);
    }
}