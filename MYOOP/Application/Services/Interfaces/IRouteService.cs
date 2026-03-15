using OOP.Domain.Entities;
using OOP.Infrastructure.Map;

namespace OOP.Application.Services.Interfaces
{
    public interface IRouteService
    {
        Task<double> CalculateDistanceAsync(Location start, Location end);

        Task<List<Location>> GetRoutePointsAsync(Location start, Location end);

        Task<MapRouteResult?> GetFullRouteAsync(Location start, Location end);

        Task<bool> IsNearAsync(Location a, Location b, double radiusKm);
    }
}