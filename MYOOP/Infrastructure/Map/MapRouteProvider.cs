using GMap.NET;
using GMap.NET.MapProviders;
using DomainGeoLocation = OOP.Domain.Entities.GeoLocation;
using GMapRoute = GMap.NET.MapRoute;

namespace OOP.Infrastructure.Map
{
    public class MapRouteProvider : IMapRouteProvider
    {
        public async Task<OOP.Domain.Entities.Route?> GetRouteAsync(DomainGeoLocation start, DomainGeoLocation end)
        {
            return await Task.Run(() =>
            {
                var startPoint = new PointLatLng(start.Lat, start.Lng);
                var endPoint = new PointLatLng(end.Lat, end.Lng);

                GMapRoute? route = GMapProviders.OpenStreetMap.GetRoute(
                    startPoint,
                    endPoint,
                    false,
                    false,
                    15);

                if (route == null || route.Points == null || route.Points.Count < 2)
                    return null;

                // Convert GMap points to GeoLocation list (without LINQ)
                var points = new List<DomainGeoLocation>();
                foreach (var p in route.Points)
                {
                    points.Add(new DomainGeoLocation("RoutePoint", "RoutePoint", p.Lat, p.Lng));
                }

                var durationMinutes = (int)(route.Distance / 30.0 * 60); // minutes at 30 km/h average speed

                var routeObj = new OOP.Domain.Entities.Route(start, end, route.Distance, durationMinutes, points);
                return routeObj;
            });
        }
    }
}
