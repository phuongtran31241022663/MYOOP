using OOP.Application.Validators;
using OOP.Domain.Entities;
using OOP.Domain.Enums;
using OOP.Domain.Interfaces;
using OOP.Application.Services.Interfaces;

namespace OOP.Application.Services
{
    public class DriverMatchingService : IDriverMatchingService
    {
        private readonly IUserRepository _userRepo;
        private readonly IRouteService _routeService;

        public DriverMatchingService(IUserRepository userRepo, IRouteService routeService)
        {
            _userRepo = userRepo ?? throw new ArgumentNullException(nameof(userRepo));
            _routeService = routeService ?? throw new ArgumentNullException(nameof(routeService));
        }

        public async Task<Driver?> FindAvailableDriver(
            Location pickup,
            VehicleType vehicleType,
            IEnumerable<Guid>? excludedDriverIds = null)
        {
            if (pickup == null) throw new ArgumentNullException(nameof(pickup));

            var excluded = excludedDriverIds != null
                ? new HashSet<Guid>(excludedDriverIds)
                : new HashSet<Guid>();

            var allUsers = await _userRepo.GetAll();

            // Bước 1: Lọc nhanh các ứng viên tiềm năng
            var candidates = allUsers
                .OfType<Driver>()
                .Where(d => d.IsActive
                         && d.Status == DriverStatus.Available
                         && d.Vehicle.Type == vehicleType
                         && d.CurrentLocation != null
                         && !excluded.Contains(d.Id))
                .ToList();

            if (!candidates.Any()) return null;

            // Bước 2: Tính khoảng cách lộ trình thực tế
            Driver? bestDriver = null;
            double minDistance = double.MaxValue;

            foreach (var driver in candidates)
            {
                double routeDistance = await _routeService.CalculateDistanceAsync(driver.CurrentLocation, pickup);
                if (routeDistance < minDistance)
                {
                    minDistance = routeDistance;
                    bestDriver = driver;
                }
            }

            return bestDriver;
        }

        public async Task<Driver?> MatchDriver(Trip trip)
        {
            if (trip == null) throw new ArgumentNullException(nameof(trip));

            var driver = await FindAvailableDriver(trip.PickupLocation, trip.VehicleType, trip.RejectedDriverIds);

            if (driver == null) return null;

            TripValidator.ValidateDriverAssignment(trip, driver);

            return driver;
        }

        public async Task<List<Driver>> GetNearbyDrivers(Location pickup, VehicleType vehicleType, double maxKm)
        {
            if (pickup == null) throw new ArgumentNullException(nameof(pickup));
            if (maxKm <= 0) throw new ArgumentException("Bán kính phải lớn hơn 0.", nameof(maxKm));

            var allUsers = await _userRepo.GetAll();
            var candidates = allUsers
                .OfType<Driver>()
                .Where(d => d.IsActive
                         && d.Status == DriverStatus.Available
                         && d.Vehicle.Type == vehicleType
                         && d.CurrentLocation != null)
                .ToList();

            var result = new List<(Driver driver, double distance)>();
            foreach (var driver in candidates)
            {
                double routeDistance = await _routeService.CalculateDistanceAsync(driver.CurrentLocation, pickup);
                if (routeDistance <= maxKm)
                    result.Add((driver, routeDistance));
            }

            return result
                .OrderBy(r => r.distance)
                .Select(r => r.driver)
                .ToList();
        }
    }
}
