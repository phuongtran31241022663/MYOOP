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
            try
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
            catch (Exception ex)
            {
                // Fallback: calculate Haversine distance when map service fails
                System.Diagnostics.Debug.WriteLine($"[MapRouteProvider] Error: {ex.Message}. Using Haversine fallback.");

                double distance = CalculateHaversineDistance(start, end);
                int durationMinutes = (int)(distance / 30.0 * 60); // 30 km/h average speed

                // Create simple route with just start and end points
                var points = new List<DomainGeoLocation> { start, end };
                return new OOP.Domain.Entities.Route(start, end, distance, durationMinutes, points);
            }
        }

        /// <summary>
        /// Calculate distance using Haversine formula as fallback
        /// </summary>
        private static double CalculateHaversineDistance(DomainGeoLocation from, DomainGeoLocation to)
        {
            const double R = 6371; // Earth radius in km
            double lat1 = from.Lat * Math.PI / 180;
            double lat2 = to.Lat * Math.PI / 180;
            double dLat = (to.Lat - from.Lat) * Math.PI / 180;
            double dLon = (to.Lng - from.Lng) * Math.PI / 180;

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1) * Math.Cos(lat2) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }
    }
}
