using System.Text.Json;

namespace OOP.Infrastructure
{
    /// <summary>
    /// Singleton ConfigService - quản lý cấu hình app.
    /// Đọc config từ file appsettings.json hoặc dùng default values.
    /// </summary>
    public sealed class ConfigService
    {
        private static readonly object _lock = new object();
        private static ConfigService? _instance;
        private readonly AppSettings _settings;

        private ConfigService()
        {
            _settings = LoadSettings();
        }

        /// <summary>
        /// Singleton Instance - Thread-safe
        /// </summary>
        public static ConfigService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new ConfigService();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Load cấu hình từ file hoặc dùng default
        /// </summary>
        private AppSettings LoadSettings()
        {
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            
            if (File.Exists(configPath))
            {
                try
                {
                    string json = File.ReadAllText(configPath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    if (settings != null)
                    {
                        Logger.Instance.Info("Đã load cấu hình từ appsettings.json");
                        return settings;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.Error(ex, "Lỗi đọc appsettings.json, dùng default");
                }
            }

            // Return default settings
            Logger.Instance.Info("Sử dụng cấu hình mặc định");
            return GetDefaultSettings();
        }

        /// <summary>
        /// Default settings
        /// </summary>
        private AppSettings GetDefaultSettings()
        {
            return new AppSettings
            {
                // Simulation settings
                Simulation = new SimulationSettings
                {
                    Enabled = true,
                    MinLat = 10.7200,
                    MaxLat = 10.8500,
                    MinLng = 106.6000,
                    MaxLng = 106.7800,
                    UpdateIntervalMs = 2000
                },
                // Trip timeout settings
                TripTimeout = new TripTimeoutSettings
                {
                    SearchTimeoutSeconds = 180,
                    MatchedTimeoutSeconds = 60
                },
                // Fare settings
                Fare = new FareSettings
                {
                    DefaultCurrency = "VND",
                    MinimumFare = 10000
                },
                // Driver matching settings
                DriverMatching = new DriverMatchingSettings
                {
                    MaxDistanceKm = 10.0,
                    SearchRadiusKm = 5.0
                }
            };
        }

        // ─── Properties ─────────────────────────────────────────────────

        public SimulationSettings Simulation => _settings.Simulation;
        public TripTimeoutSettings TripTimeout => _settings.TripTimeout;
        public FareSettings Fare => _settings.Fare;
        public DriverMatchingSettings DriverMatching => _settings.DriverMatching;

        /// <summary>
        /// Get giá trị config theo key
        /// </summary>
        public string GetValue(string key, string defaultValue = "")
        {
            return Environment.GetEnvironmentVariable(key) ?? defaultValue;
        }

        public void Reload()
        {
            lock (_lock)
            {
                _instance = null; // Force reload
                _ = Instance; // Access to recreate
            }
            Logger.Instance.Info("Đã reload cấu hình");
        }
    }

    // ─── Settings Classes ─────────────────────────────────────────────────

    public class AppSettings
    {
        public SimulationSettings Simulation { get; set; } = new();
        public TripTimeoutSettings TripTimeout { get; set; } = new();
        public FareSettings Fare { get; set; } = new();
        public DriverMatchingSettings DriverMatching { get; set; } = new();
    }

    public class SimulationSettings
    {
        public bool Enabled { get; set; } = true;
        public double MinLat { get; set; } = 10.7200;
        public double MaxLat { get; set; } = 10.8500;
        public double MinLng { get; set; } = 106.6000;
        public double MaxLng { get; set; } = 106.7800;
        public int UpdateIntervalMs { get; set; } = 2000;
    }

    public class TripTimeoutSettings
    {
        public int SearchTimeoutSeconds { get; set; } = 180;
        public int MatchedTimeoutSeconds { get; set; } = 60;

        public TimeSpan SearchTimeout => TimeSpan.FromSeconds(SearchTimeoutSeconds);
        public TimeSpan MatchedTimeout => TimeSpan.FromSeconds(MatchedTimeoutSeconds);
    }

    public class FareSettings
    {
        public string DefaultCurrency { get; set; } = "VND";
        public decimal MinimumFare { get; set; } = 10000;
    }

    public class DriverMatchingSettings
    {
        public double MaxDistanceKm { get; set; } = 10.0;
        public double SearchRadiusKm { get; set; } = 5.0;
    }
}
