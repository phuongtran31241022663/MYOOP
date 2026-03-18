﻿using OOP.Domain.Entities;
using OOP.Domain.Enums;

namespace OOP.Application.Services.Interfaces
{
    public interface IFareService
    {
        Task<decimal> CalculateFare(Trip trip);

        Task<Fare?> GetFareRule(VehicleType vehicleType);
    }
}