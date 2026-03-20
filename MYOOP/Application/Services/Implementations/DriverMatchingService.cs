using OOP.Domain.Entities;
using OOP.Domain.Enums;
using OOP.Domain.Interfaces;
using OOP.Application.Services.Interfaces;
using System.Diagnostics;

namespace OOP.Application.Services
{
    public class DriverMatchingService : IDriverMatchingService
    {
        private readonly IUserRepository _userRepo;
        private readonly ITripRepository _tripRepo;
        private readonly IRouteService _routeService;

        public DriverMatchingService(IUserRepository userRepo, ITripRepository tripRepo, IRouteService routeService)
        {
            _userRepo = userRepo ?? throw new ArgumentNullException(nameof(userRepo));
            _tripRepo = tripRepo ?? throw new ArgumentNullException(nameof(tripRepo));
            _routeService = routeService ?? throw new ArgumentNullException(nameof(routeService));
        }

        public async Task<Driver?> FindAvailableDriver(
            GeoLocation pickup,
            VehicleType vehicleType,
            IEnumerable<Guid>? excludedDriverIds = null)
        {
            if (pickup == null)
                throw new ArgumentNullException(nameof(pickup), "Điểm đón không được để trống.");

            var excluded = excludedDriverIds != null
                ? new HashSet<Guid>(excludedDriverIds)
                : new HashSet<Guid>();

            Debug.WriteLine($"[DriverMatching] Finding available drivers for pickup: {pickup.Address}, vehicleType: {vehicleType}");

            // Refresh cache to get latest driver status from storage
            if (_userRepo is ICacheRefreshable cacheRefreshable)
            {
                await cacheRefreshable.RefreshCacheAsync();
            }

            var allUsers = await _userRepo.GetAvailableDrivers(vehicleType);
            Debug.WriteLine($"[DriverMatching] Repository returned {allUsers.Count} drivers");

            foreach (var driver in allUsers)
            {
                Debug.WriteLine($"[DriverMatching] Driver from repo: {driver.Id}, Name: {driver.Name}, Status: {driver.Status}, IsActive: {driver.IsActive}, Vehicle: {driver.Vehicle?.Type}, Position: {driver.Position?.Address}");
            }

            var candidates = FilterCandidates(allUsers.OfType<Driver>(), vehicleType)
                .Where(d => !excluded.Contains(d.Id))
                .ToList();

            Debug.WriteLine($"[DriverMatching] After filtering: {candidates.Count} candidates");

            if (!candidates.Any())
            {
                Debug.WriteLine("[DriverMatching] No candidates found after filtering!");
                return null;
            }

            foreach (var candidate in candidates)
            {
                Debug.WriteLine($"[DriverMatching] Candidate: {candidate.Name} at {candidate.Position.Address}");
            }

            var tasks = candidates.Select(async driver => new
            {
                Driver = driver,
                Distance = await _routeService.CalculateDistanceAsync(driver.Position, pickup)
            });

            var results = await Task.WhenAll(tasks);

            var best = results
                .OrderBy(r => r.Distance)
                .FirstOrDefault();

            Debug.WriteLine($"[DriverMatching] Best driver: {best?.Driver.Name} at distance {best?.Distance}km");

            return best?.Driver;
        }
        private IEnumerable<Driver> FilterCandidates(IEnumerable<Driver> drivers, VehicleType type)
        {
            Debug.WriteLine($"[DriverMatching] FilterCandidates called with {drivers.Count()} drivers, vehicleType: {type}");

            foreach (var d in drivers)
            {
                bool isActive = d.IsActive;
                bool isAvailable = d.Status == DriverStatus.Available;
                bool hasVehicle = d.Vehicle != null;
                bool correctType = d.Vehicle?.Type == type;
                bool hasPosition = d.Position != null;

                Debug.WriteLine($"[DriverMatching] Filtering Driver {d.Name}: IsActive={isActive}, Status={d.Status} (Available={isAvailable}), HasVehicle={hasVehicle}, CorrectType={correctType}, HasPosition={hasPosition}");

                if (!isActive) Debug.WriteLine($"[DriverMatching]   -> EXCLUDED: Driver is not active");
                if (!isAvailable) Debug.WriteLine($"[DriverMatching]   -> EXCLUDED: Status is {d.Status}, not Available");
                if (!hasVehicle) Debug.WriteLine($"[DriverMatching]   -> EXCLUDED: No vehicle assigned");
                if (hasVehicle && !correctType) Debug.WriteLine($"[DriverMatching]   -> EXCLUDED: Vehicle type {d.Vehicle?.Type} != {type}");
                if (!hasPosition) Debug.WriteLine($"[DriverMatching]   -> EXCLUDED: No position set");
            }

            return drivers.Where(d =>
                d.IsActive &&
                d.Status == DriverStatus.Available &&
                d.Vehicle != null &&
                d.Vehicle.Type == type &&
                d.Position != null);
        }
        public async Task<Driver?> MatchDriver(Trip trip)
        {
            if (trip == null) throw new ArgumentNullException(nameof(trip));

            var driver = await FindAvailableDriver(trip.PickupLocation, trip.VehicleType, trip.RejectedDriverIds);

            if (driver == null) return null;

            driver.SetBusy();
            trip.AssignDriver(driver);
            return driver;
        }

        public async Task<List<Driver>> GetNearbyDrivers(GeoLocation pickup, VehicleType vehicleType, double maxKm)
        {
            if(pickup == null)
                throw new ArgumentNullException(nameof(pickup), "Điểm đón không được để trống.");
            if (maxKm <= 0) throw new ArgumentException("Bán kính phải lớn hơn 0.", nameof(maxKm));

            var candidates = await _userRepo.GetAvailableDrivers(vehicleType);

            candidates = candidates
                .Where(d => d.Position != null)
                .ToList();

            var tasks = candidates.Select(async driver => new
            {
                Driver = driver,
                Distance = await _routeService.CalculateDistanceAsync(driver.Position, pickup)
            });

            var results = await Task.WhenAll(tasks);

            return results
                .Where(r => r.Distance <= maxKm)
                .OrderBy(r => r.Distance)
                .Select(r => r.Driver)
                .ToList();
        }
    }
}
