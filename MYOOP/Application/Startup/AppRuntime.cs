using OOP.Infrastructure;
using DomainLocation = OOP.Domain.Entities.GeoLocation;

namespace OOP
{
    internal static class AppRuntime
    {
        private static readonly Random Random = new();

        public static DomainLocation GenerateRandomLocation(string name, string address)
        {
            var config = ConfigService.Instance;
            var lat = config.Simulation.MinLat +
                      (config.Simulation.MaxLat - config.Simulation.MinLat) * Random.NextDouble();
            var lng = config.Simulation.MinLng +
                      (config.Simulation.MaxLng - config.Simulation.MinLng) * Random.NextDouble();
            return new DomainLocation(name, address, lat, lng);
        }

        internal static class SimulationConfig
        {
            private static bool _enabled = ConfigService.Instance.Simulation.Enabled;

            public static bool Enabled
            {
                get => _enabled;
                set => _enabled = value;
            }

            public static double MinLat => ConfigService.Instance.Simulation.MinLat;
            public static double MaxLat => ConfigService.Instance.Simulation.MaxLat;
            public static double MinLng => ConfigService.Instance.Simulation.MinLng;
            public static double MaxLng => ConfigService.Instance.Simulation.MaxLng;

            public static DomainLocation GenerateRandomLocation(string name, string address)
                => AppRuntime.GenerateRandomLocation(name, address);
        }

        internal static class TripTimeoutConfig
        {
            public static TimeSpan SearchTimeout => ConfigService.Instance.TripTimeout.SearchTimeout;
        }

        public static TimeSpan GetTripTimeout() => ConfigService.Instance.TripTimeout.SearchTimeout;
    }
}
