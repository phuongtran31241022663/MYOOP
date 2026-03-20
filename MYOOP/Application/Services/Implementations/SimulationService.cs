using OOP.Domain.Entities;
using OOP.Domain.Enums;
using OOP.Domain.Interfaces;
using OOP.Application.Services.Interfaces;

namespace OOP.Application.Services
{
    public class SimulationService : ISimulationService
    {
        private readonly ITripRepository _tripRepo;
        private readonly IUserRepository _userRepo;
        private readonly IRouteService _routeService;
        private readonly ITripService _tripService;

        private readonly Dictionary<Guid, (Guid driverId, Route route, int index, bool toPickup)> _simulations
            = new();

        public SimulationService(
            IUserRepository userRepo,
            ITripRepository tripRepo,
            INotificationService _,
            ITripService tripService,
            IRouteService routeService)
        {
            _tripRepo = tripRepo;
            _userRepo = userRepo;
            _routeService = routeService;
            _tripService = tripService;
        }

        public async Task SimulateDriverToPickup(Guid tripId)
        {
            if (tripId == Guid.Empty)
                throw new ArgumentException("Trip ID không hợp lệ.", nameof(tripId));

            if (_simulations.ContainsKey(tripId)) return;

            var trip = await _tripRepo.GetById(tripId)
                ?? throw new KeyNotFoundException($"Không tìm thấy trip '{tripId}'.");

            if (!trip.DriverId.HasValue)
                throw new InvalidOperationException("Trip chưa có driver.");

            var driver = await _userRepo.GetById(trip.DriverId.Value) as Driver
                ?? throw new KeyNotFoundException($"Không tìm thấy driver '{trip.DriverId.Value}'.");

            if (driver.Position == null)
                throw new InvalidOperationException("Driver không có vị trí.");

            var route = await _routeService.GetFullRouteAsync(
                driver.Position,
                trip.PickupLocation);

            if (route == null || route.Points.Count < 2)
                throw new InvalidOperationException("Không lấy được lộ trình.");

            _simulations[tripId] = (driver.Id, route, 0, true);
        }

        public async Task SimulateTripToDestination(Guid tripId)
        {
            if (tripId == Guid.Empty)
                throw new ArgumentException("Trip ID không hợp lệ.", nameof(tripId));

            if (_simulations.ContainsKey(tripId)) return;

            var trip = await _tripRepo.GetById(tripId)
                ?? throw new KeyNotFoundException($"Không tìm thấy trip '{tripId}'.");

            if (!trip.DriverId.HasValue)
                throw new InvalidOperationException("Trip chưa có driver.");

            if (trip.PickupLocation == null || trip.DestinationLocation == null)
                throw new InvalidOperationException("Trip không có địa điểm đón hoặc trả.");

            var route = await _routeService.GetFullRouteAsync(
                trip.PickupLocation,
                trip.DestinationLocation);

            if (route == null || route.Points.Count < 2)
                throw new InvalidOperationException("Không lấy được lộ trình.");

            _simulations[tripId] = (trip.DriverId.Value, route, 0, false);
        }

        public async Task Tick()
        {
            var activeTripIds = _simulations.Keys.ToList();

            foreach (var tripId in activeTripIds)
            {
                if (!_simulations.TryGetValue(tripId, out var sim)) continue;

                var nextIndex = sim.index;

                if (nextIndex >= sim.route.Points.Count)
                {
                    await HandleArrival(tripId, sim.toPickup);
                    continue;
                }

                // NOTE: Chỉ cập nhật vị trí trong bộ nhớ để hiển thị UI
                // KHÔNG ghi vào repository - sẽ gây race condition với TripService
                // TripService quản lý trạng thái driver (Available/Busy)
                var driver = await _userRepo.GetById(sim.driverId) as Driver;
                if (driver != null)
                {
                    driver.UpdateLocation(sim.route.Points[nextIndex]);
                    // ĐÃ XÓA: await _userRepo.Update(driver);
                    // Lý do: Simulation chỉ nên update vị trí cho UI, không persist
                    // Việc ghi đè sẽ làm mất trạng thái đúng (Available -> Busy)
                }

                _simulations[tripId] = (sim.driverId, sim.route, nextIndex + 1, sim.toPickup);
            }
        }

        private async Task HandleArrival(Guid tripId, bool toPickup)
        {
            if (toPickup)
            {
                await _tripService.MarkArrived(tripId);
            }
            else
            {
                await _tripService.CompleteTrip(tripId);
            }

            _simulations.Remove(tripId);
        }

        public Task StopSimulation(Guid tripId)
        {
            _simulations.Remove(tripId);
            return Task.CompletedTask;
        }

        public async Task UpdateDriverLocations()
        {
            await Tick();
        }

        public async Task SimulateTripProgress(Guid tripId)
        {
            var trip = await _tripRepo.GetById(tripId);
            if (trip == null) return;

            if (trip.Status == TripStatus.Matched)
                await SimulateDriverToPickup(tripId);
            else if (trip.Status == TripStatus.Started)
                await SimulateTripToDestination(tripId);
            else if (trip.Status == TripStatus.Completed || trip.Status == TripStatus.Cancelled)
                await StopSimulation(tripId);
        }
    }
}

