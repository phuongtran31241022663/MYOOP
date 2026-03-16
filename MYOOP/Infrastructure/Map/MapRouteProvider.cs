using GMap.NET;
using GMap.NET.MapProviders;
using DomainLocation = OOP.Domain.Entities.Location;

namespace OOP.Infrastructure.Map
{
    public class MapRouteProvider : IMapRouteProvider
    {
        public async Task<MapRouteResult?> GetRouteAsync(DomainLocation start, DomainLocation end)
        {
            return await Task.Run(() =>
            {
                var startPoint = new PointLatLng(start.Lat, start.Lng);
                var endPoint = new PointLatLng(end.Lat, end.Lng);

                var route = GMapProviders.OpenStreetMap.GetRoute(
                    startPoint,
                    endPoint,
                    false,
                    false,
                    15);

                if (route == null || route.Points == null || route.Points.Count < 2)
                    return null;

                var points = route.Points
     .Select(p => new DomainLocation("RoutePoint", "RoutePoint", p.Lat, p.Lng))
     .ToList();

                var durationSeconds = (int)(route.Distance / 30 * 3600);

                return new MapRouteResult
                {
                    Distance = route.Distance,
                    Duration = durationSeconds,
                    Points = points
                };
            });
        }
    }
}