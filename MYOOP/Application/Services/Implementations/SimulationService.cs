using OOP.Application.Interfaces;
using OOP.Application.Services.Interfaces;
using OOP.Domain.Entities;
using OOP.Domain.Enums;
using OOP.Domain.Interfaces;

namespace OOP.Application.Services
{
    public class SimulationService : ISimulationService
    {
        private readonly IUserRepository _userRepo;
        private readonly ITripRepository _tripRepo;
        private readonly INotificationService _notificationService;
        private readonly ITripService _tripService;
        private readonly IRouteService _routeService;

        private const double StepMeters = 100;
        private static readonly Random _rng = new();

        public SimulationService(
            IUserRepository userRepo,
            ITripRepository tripRepo,
            INotificationService notificationService,
            ITripService tripService,
            IRouteService routeService)
        {
            _userRepo = userRepo ?? throw new ArgumentNullException(nameof(userRepo));
            _tripRepo = tripRepo ?? throw new ArgumentNullException(nameof(tripRepo));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _tripService = tripService ?? throw new ArgumentNullException(nameof(tripService));
            _routeService = routeService ?? throw new ArgumentNullException(nameof(routeService));
        }

        // --- 1. Cập nhật vị trí tất cả tài xế Available và Busy ---
        public async Task UpdateDriverLocations()
        {
            var users = await _userRepo.GetAll();

            var activeDrivers = users
                .OfType<Driver>()
                .Where(d => d.IsActive &&
                           (d.Status == DriverStatus.Available || d.Status == DriverStatus.Busy))
                .ToList();

            foreach (var driver in activeDrivers)
                await SimulateDriverMovement(driver.Id);
        }

        // --- 2. Di chuyển một tài xế theo hướng ngẫu nhiên ---
        public async Task SimulateDriverMovement(Guid driverId)
        {
            var user = await _userRepo.GetById(driverId);
            if (user is not Driver driver) return;

            if (driver.Status == DriverStatus.Busy)
            {
                await MoveDriverTowardActiveTrip(driver);
                return;
            }

            var newLocation = RandomStep(driver.CurrentLocation);
            driver.UpdateLocation(newLocation);
            await _userRepo.Update(driver);
        }

        // --- 3. Tự động tiến trình một trip đang Ongoing ---
        public async Task SimulateTripProgress(Guid tripId)
        {
            var trip = await _tripRepo.GetById(tripId);
            if (trip == null) return;

            switch (trip.Status)
            {
                case TripStatus.Matched:
                    await _tripService.MarkArrived(tripId);
                    await _notificationService.NotifyTripUpdate(
                        tripId, "[Simulation] Tài xế đã đến điểm đón.");
                    break;

                case TripStatus.Arrived:
                    await _tripService.StartTrip(tripId);
                    await _notificationService.NotifyTripUpdate(
                        tripId, "[Simulation] Chuyến đi đã bắt đầu.");
                    break;

                case TripStatus.Ongoing:
                    if (trip.Distance <= 0)
                    {
                        var routeResult = await _routeService.GetFullRouteAsync(
                            trip.PickupLocation,
                            trip.DestinationLocation);

                        if (routeResult != null)
                        {
                            trip.ApplyDistance(routeResult.Distance);
                            await _tripRepo.Update(trip);
                        }
                        else
                        {
                            throw new InvalidOperationException("Không thể tìm thấy lộ trình đường bộ cho chuyến đi này.");
                        }
                    }

                    await _tripService.CompleteTrip(tripId);
                    break;
                default:
                    break;
            }
        }

        // --- Helpers ---

        private async Task MoveDriverTowardActiveTrip(Driver driver)
        {
            var trips = await _tripRepo.GetByDriverId(driver.Id);

            var activeTrip = trips.FirstOrDefault(t =>
                t.Status == TripStatus.Matched ||
                t.Status == TripStatus.Arrived ||
                t.Status == TripStatus.Ongoing);

            if (activeTrip == null)
            {
                driver.UpdateLocation(RandomStep(driver.CurrentLocation));
                await _userRepo.Update(driver);
                return;
            }

            var target = activeTrip.Status == TripStatus.Ongoing
                ? activeTrip.DestinationLocation
                : activeTrip.PickupLocation;

            var newLocation = StepToward(driver.CurrentLocation, target);
            driver.UpdateLocation(newLocation);
            await _userRepo.Update(driver);
        }

        private static Location StepToward(Location current, Location target)
        {
            const double metersPerDegLat = 111320;

            double latRad = current.Lat * Math.PI / 180;
            double metersPerDegLng = metersPerDegLat * Math.Cos(latRad);

            double dLatMeters = (target.Lat - current.Lat) * metersPerDegLat;
            double dLngMeters = (target.Lng - current.Lng) * metersPerDegLng;

            double dist = Math.Sqrt(dLatMeters * dLatMeters + dLngMeters * dLngMeters);

            if (dist < StepMeters)
                return target;

            double stepLat = (dLatMeters / dist) * StepMeters;
            double stepLng = (dLngMeters / dist) * StepMeters;

            double newLat = current.Lat + stepLat / metersPerDegLat;
            double newLng = current.Lng + stepLng / metersPerDegLng;

            return new Location(current.Name, current.Address, newLat, newLng);
        }

        private static Location RandomStep(Location current)
        {
            const double metersPerDegLat = 111320;

            double latRad = current.Lat * Math.PI / 180;
            double metersPerDegLng = metersPerDegLat * Math.Cos(latRad);

            double dLat = (_rng.NextDouble() - 0.5) * 2 * StepMeters / metersPerDegLat;
            double dLng = (_rng.NextDouble() - 0.5) * 2 * StepMeters / metersPerDegLng;

            return new Location(
                current.Name,
                current.Address,
                current.Lat + dLat,
                current.Lng + dLng
            );
        }
    }
}