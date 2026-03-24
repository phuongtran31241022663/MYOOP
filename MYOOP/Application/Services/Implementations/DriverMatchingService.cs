using OOP.Domain.Entities;
using OOP.Domain.Interfaces;
using OOP.Domain.Policies;
using OOP.Application.Services.Interfaces;
using System.Diagnostics;

namespace OOP.Application.Services
{
    public class DriverMatchingService : IDriverMatchingService
    {
        private readonly IUserRepository _userRepo;
        private readonly ITripRepository _tripRepo;
        private readonly IRouteService _routeService;

        private const int MaxRetryAttempts = 5;

        public DriverMatchingService(
            IUserRepository userRepo,
            ITripRepository tripRepo,
            IRouteService routeService)
        {
            _userRepo = userRepo ?? throw new ArgumentNullException(nameof(userRepo));
            _tripRepo = tripRepo ?? throw new ArgumentNullException(nameof(tripRepo));
            _routeService = routeService ?? throw new ArgumentNullException(nameof(routeService));
        }

        // ── Find (read-only, no reservation) ─────────────────────────────────

        public async Task<Driver?> FindActiveDriver(
            GeoLocation pickup,
            string VehicleType,
            IEnumerable<Guid>? excludedDriverIds = null)
        {
            if (pickup == null) throw new ArgumentNullException(nameof(pickup));

            var excluded = excludedDriverIds != null
                ? new HashSet<Guid>(excludedDriverIds)
                : new HashSet<Guid>();

            await RefreshCacheIfSupported();

            var allUsers = await _userRepo.GetActiveDrivers(VehicleType);
            var candidates = DriverMatchingPolicy
                .FilterEligibleCandidates(allUsers.OfType<Driver>(), VehicleType, excluded)
                .ToList();

            Debug.WriteLine($"[FindActive] {candidates.Count} candidates for {VehicleType}");
            if (!candidates.Any()) return null;

            var tasks = candidates.Select(async d => new
            {
                Driver = d,
                Distance = await _routeService.CalculateDistanceAsync(d.Position, pickup)
            });

            var results = await Task.WhenAll(tasks);
            return results.OrderBy(r => r.Distance).FirstOrDefault()?.Driver;
        }

        // ── Find + Reserve (atomic, with retry) ───────────────────────────────

        /// <summary>
        /// Tìm và giữ tài xế. Tránh race condition bằng TryReserveDriver.
        /// Có giới hạn retry để tránh CPU spike.
        /// </summary>
        public async Task<Driver?> FindAndReserveDriver(
            GeoLocation pickup,
            string VehicleType,
            IEnumerable<Guid>? excludedDriverIds = null,
            int retryCount = 0)
        {
            if (pickup == null) throw new ArgumentNullException(nameof(pickup));
            if (retryCount >= MaxRetryAttempts)
            {
                Debug.WriteLine($"[FindAndReserve] Max retries ({MaxRetryAttempts}) reached.");
                return null;
            }

            var excluded = excludedDriverIds != null
                ? new HashSet<Guid>(excludedDriverIds)
                : new HashSet<Guid>();

            await RefreshCacheIfSupported();

            var reserved = await _userRepo.TryReserveDriver(VehicleType);
            if (reserved == null)
            {
                Debug.WriteLine("[FindAndReserve] No Active drivers.");
                return null;
            }

            if (excluded.Contains(reserved.Id))
            {
                Debug.WriteLine($"[FindAndReserve] Reserved driver {reserved.Name} is excluded — releasing and retrying.");

                // Release back to Active
                reserved.SetActive();
                await _userRepo.Update(reserved);

                // Small delay to reduce CPU spike during retry (exponential backoff)
                if (retryCount < MaxRetryAttempts - 1)
                {
                    await Task.Delay(50 * (retryCount + 1)); // 50ms, 100ms, 150ms...
                }

                // Retry with this driver added to excluded to prevent infinite loop
                var newExcluded = new HashSet<Guid>(excluded) { reserved.Id };
                return await FindAndReserveDriver(pickup, VehicleType, newExcluded, retryCount + 1);
            }

            Debug.WriteLine($"[FindAndReserve] Reserved driver: {reserved.Name}");
            return reserved;
        }

        // ── Match (used by auto-matching flow) ────────────────────────────────

        public async Task<Driver?> MatchDriver(Trip trip)
        {
            if (trip == null) throw new ArgumentNullException(nameof(trip));

            var driver = await FindActiveDriver(
                trip.Pickup, trip.VehicleType, trip.RejectedDriverIds);

            if (driver == null) return null;

            driver.SetOnTrip();
            trip.AssignDriver(driver);
            return driver;
        }

        // ── Nearby drivers (for passenger map display) ────────────────────────

        public async Task<List<Driver>> GetNearbyDrivers(
            GeoLocation pickup, string VehicleType, double maxKm)
        {
            if (pickup == null) throw new ArgumentNullException(nameof(pickup));
            if (maxKm <= 0) throw new ArgumentException("Bán kính phải lớn hơn 0.", nameof(maxKm));

            var allDrivers = await _userRepo.GetActiveDrivers(VehicleType);
            var candidates = DriverMatchingPolicy
                .FilterEligibleCandidates(allDrivers.OfType<Driver>(), VehicleType)
                .ToList();

            var tasks = candidates.Select(async d => new
            {
                Driver = d,
                Distance = await _routeService.CalculateDistanceAsync(d.Position, pickup)
            });

            var results = await Task.WhenAll(tasks);
            return results
                .Where(r => r.Distance <= maxKm)
                .OrderBy(r => r.Distance)
                .Select(r => r.Driver)
                .ToList();
        }

        // ── Helper ────────────────────────────────────────────────────────────

        private async Task RefreshCacheIfSupported()
        {
            if (_userRepo is ICacheRefreshable cr)
                await cr.RefreshCacheAsync();
        }
    }
}