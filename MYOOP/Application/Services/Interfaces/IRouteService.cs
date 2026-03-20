﻿using OOP.Domain.Entities;

namespace OOP.Application.Services.Interfaces
{
    public interface IRouteService
    {
        Task<double> CalculateDistanceAsync(GeoLocation start, GeoLocation end);

        Task<IReadOnlyList<GeoLocation>> GetRoutePointsAsync(GeoLocation start, GeoLocation end);

        Task<Route?> GetFullRouteAsync(GeoLocation start, GeoLocation end);

        Task<bool> IsNearAsync(GeoLocation a, GeoLocation b, double radiusKm);
    }
}
