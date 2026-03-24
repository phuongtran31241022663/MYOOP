﻿using OOP.Domain.Entities;

namespace OOP.Application.Services.Interfaces
{
    public interface IFareService
    {
        Task<decimal> CalculateFare(Trip trip);

        Task<Fare?> GetFareRule(string VehicleType);
    }
}