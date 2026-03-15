using OOP.Domain.Entities;

namespace OOP.Infrastructure.Map
{
    public interface IMapRouteProvider
    {
        Task<MapRouteResult?> GetRouteAsync(Location start, Location end);
    }
}
