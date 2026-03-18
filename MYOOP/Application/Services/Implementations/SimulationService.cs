using OOP.Domain.Entities;
using OOP.Domain.Enums;
using OOP.Domain.Interfaces;
using OOP.Application.Services.Interfaces;
using OOP.Infrastructure.Map;

namespace OOP.Application.Services
{
    public class SimulationService : ISimulationService
    {
        private readonly ITripRepository _tripRepo;
        private readonly IUserRepository _userRepo;
        private readonly IRouteService _routeService;
        private readonly ITripService _tripService;

        private readonly Dictionary<Guid, (Guid driverId, MapRouteResult route, int index, bool toPickup)> _simulations
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

        // DRIVER → PICKUP
        public async Task SimulateDriverToPickup(Guid tripId)
        {
            if (_simulations.ContainsKey(tripId)) return;

            var trip = await _tripRepo.GetById(tripId)
                ?? throw new Exception("Trip not found");

            if (!trip.DriverId.HasValue)
                throw new Exception("Trip chưa có driver.");

            var driver = await _userRepo.GetById(trip.DriverId.Value) as Driver
                ?? throw new Exception("Driver not found");

            var route = await _routeService.GetFullRouteAsync(
                driver.CurrentLocation,
                trip.PickupLocation);

            if (route == null || route.Points.Count < 2)
                throw new Exception("Không lấy được route.");

            _simulations[tripId] = (driver.Id, route, 0, true);
        }

        // PICKUP → DESTINATION
        public async Task SimulateTripToDestination(Guid tripId)
        {
            if (_simulations.ContainsKey(tripId)) return;

            var trip = await _tripRepo.GetById(tripId)
                ?? throw new Exception("Trip not found");

            if (!trip.DriverId.HasValue)
                throw new Exception("Trip chưa có driver.");

            var route = await _routeService.GetFullRouteAsync(
                trip.PickupLocation,
                trip.DestinationLocation);

            if (route == null || route.Points.Count < 2)
                throw new Exception("Không lấy được route.");

            _simulations[tripId] = (trip.DriverId.Value, route, 0, false);
        }

        // TICK LOOP
        public async Task Tick()
        {
            foreach (var key in _simulations.Keys.ToList())
            {
                var sim = _simulations[key];

                var driver = await _userRepo.GetById(sim.driverId) as Driver;
                if (driver == null) continue;

                if (sim.index >= sim.route.Points.Count)
                {
                    await HandleArrival(key, sim.toPickup);
                    continue;
                }

                var point = sim.route.Points[sim.index];

                driver.UpdateLocation(point);

                await _userRepo.Update(driver);

                _simulations[key] = (sim.driverId, sim.route, sim.index + 1, sim.toPickup);
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

        // Adapter methods used by Program.cs
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
