﻿using OOP.Domain.Entities;
using OOP.Application.Services.Interfaces;

namespace OOP.Infrastructure.Map
{
    public class RouteService : IRouteService
    {
        private readonly IMapRouteProvider _mapProvider;

        public RouteService(IMapRouteProvider mapProvider)
        {
            _mapProvider = mapProvider;
        }

        public async Task<double> CalculateDistanceAsync(GeoLocation start, GeoLocation end)
        {
            var result = await _mapProvider.GetRouteAsync(start, end);

            if (result == null)
                throw new InvalidOperationException(
                    $"Không thể tính lộ trình từ [{start}] đến [{end}].");

            return result.Distance;
        }

        public async Task<IReadOnlyList<GeoLocation>> GetRoutePointsAsync(GeoLocation start, GeoLocation end)
        {
            var result = await _mapProvider.GetRouteAsync(start, end);
            return result?.Points ?? new List<GeoLocation>();
        }

        public async Task<Route?> GetFullRouteAsync(GeoLocation start, GeoLocation end)
        {
            return await _mapProvider.GetRouteAsync(start, end);
        }

        public async Task<bool> IsNearAsync(GeoLocation a, GeoLocation b, double radiusKm)
        {
            var result = await _mapProvider.GetRouteAsync(a, b);
            if (result == null) return false;

            return result.Distance <= radiusKm;
        }
    }
}
