using OOP.Domain.Entities;
using OOP.Infrastructure.Map;
using OOP.Application.Services.Interfaces;

namespace OOP.Application.Services.Implementations
{
    public class RouteService : IRouteService
    {
        private readonly IMapRouteProvider _mapProvider;

        public RouteService(IMapRouteProvider mapProvider)
        {
            _mapProvider = mapProvider;
        }

        public async Task<double> CalculateDistanceAsync(Location start, Location end)
        {
            var result = await _mapProvider.GetRouteAsync(start, end);
            return result?.Distance ?? double.MaxValue;
        }

        public async Task<List<Location>> GetRoutePointsAsync(Location start, Location end)
        {
            var result = await _mapProvider.GetRouteAsync(start, end);
            return result?.Points ?? new List<Location>();
        }

        public async Task<MapRouteResult?> GetFullRouteAsync(Location start, Location end)
        {
            return await _mapProvider.GetRouteAsync(start, end);
        }

        public async Task<bool> IsNearAsync(Location a, Location b, double radiusKm)
        {
            var result = await _mapProvider.GetRouteAsync(a, b);
            if (result == null) return false;

            return result.Distance <= radiusKm;
        }
    }
}