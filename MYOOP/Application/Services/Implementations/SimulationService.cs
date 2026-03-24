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

        // tripId → (driverId, route, currentIndex, isToPickup)
        private readonly Dictionary<Guid, (Guid driverId, Route route, int index, bool toPickup)>
            _simulations = new();

        public SimulationService(
            IUserRepository userRepo,
            ITripRepository tripRepo,
            INotificationService _,          // injected but not used here
            ITripService tripService,
            IRouteService routeService)
        {
            _tripRepo = tripRepo ?? throw new ArgumentNullException(nameof(tripRepo));
            _userRepo = userRepo ?? throw new ArgumentNullException(nameof(userRepo));
            _routeService = routeService ?? throw new ArgumentNullException(nameof(routeService));
            _tripService = tripService ?? throw new ArgumentNullException(nameof(tripService));
        }

        // ── Setup simulations ─────────────────────────────────────────────────

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

            var route = await _routeService.GetFullRouteAsync(driver.Position, trip.Pickup)
                ?? throw new InvalidOperationException("Không lấy được lộ trình.");

            if (route.Points.Count < 2)
                throw new InvalidOperationException("Lộ trình quá ngắn.");

            _simulations[tripId] = (driver.Id, route, 0, toPickup: true);
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

            var route = await _routeService.GetFullRouteAsync(
                    trip.Pickup, trip.Destination)
                ?? throw new InvalidOperationException("Không lấy được lộ trình.");

            if (route.Points.Count < 2)
                throw new InvalidOperationException("Lộ trình quá ngắn.");

            _simulations[tripId] = (trip.DriverId.Value, route, 0, toPickup: false);
        }

		// ── Tick ─────────────────────────────────────────────────────────────

		public async Task Tick()
		{
			var activeIds = _simulations.Keys.ToList();
			var tasks = new List<Task>();

			foreach (var tripId in activeIds)
			{
				if (!_simulations.TryGetValue(tripId, out var sim)) continue;

				// 1. Kiểm tra nếu đã đi hết lộ trình
				if (sim.index >= sim.route.Points.Count)
				{
					tasks.Add(HandleArrival(tripId, sim.toPickup));
					continue;
				}

				// 2. Cập nhật vị trí hiện tại
				var currentPoint = sim.route.Points[sim.index];
				tasks.Add(_userRepo.UpdateDriverLocation(sim.driverId, currentPoint));

				// 3. Tăng index cho nhịp sau
				_simulations[tripId] = (sim.driverId, sim.route, sim.index + 1, sim.toPickup);
			}

			await Task.WhenAll(tasks); // Chạy song song các lệnh update DB
		}

		// ── Other ISimulationService methods ─────────────────────────────────

		public Task StopSimulation(Guid tripId)
        {
            _simulations.Remove(tripId);
            return Task.CompletedTask;
        }

        public bool IsSimulationActive(Guid tripId)
        {
            // Simulation đang chạy nếu tồn tại trong dictionary
            return _simulations.ContainsKey(tripId);
        }

        public async Task UpdateDriverLocations() => await Tick();

        public async Task SimulateTripProgress(Guid tripId)
        {
            var trip = await _tripRepo.GetById(tripId);
            if (trip == null) return;
			await StopSimulation(tripId);
			switch (trip.Status)
            {
                case TripStatus.Matched:
                    await SimulateDriverToPickup(tripId);
                    break;
                case TripStatus.Started:
                    await SimulateTripToDestination(tripId);
                    break;
                case TripStatus.Completed:
                case TripStatus.Cancelled:
                    await StopSimulation(tripId);
                    break;
            }
        }

        // ── Helper ────────────────────────────────────────────────────────────

        private async Task HandleArrival(Guid tripId, bool toPickup)
        {
            _simulations.Remove(tripId);

            // KHÔNG tự động chuyển trạng thái Arrived
            // Tài xế phải nhấn nút "Đã đến điểm đón" để xác nhận
            // Chỉ dừng simulation, driver phải tự xác nhận đến nơi
            
            // else: Do NOT auto-complete trip - driver must manually click to complete
            // The trip simulation stops at destination, driver will manually complete
        }
    }
}