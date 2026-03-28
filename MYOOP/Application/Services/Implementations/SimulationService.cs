using OOP.Domain.Entities;
using OOP.Domain.Enums;
using OOP.Domain.Interfaces;
using OOP.Application.Services.Interfaces;

namespace OOP.Application.Services
{
    public class SimulationService : ISimulationService
    {
        private readonly ITripRepository _tripRepo;
        private readonly IDriverRepository _driverRepo;
        private readonly IUserRepository _userRepo;
        private readonly IRouteService _routeService;
        private readonly ITripService _tripService;

        // tripId → (driverId, route, currentIndex, toPickup, startTime, phase)
        // phase: "moving" | "waiting"
        private readonly Dictionary<Guid, (Guid driverId, Route route, int index, bool toPickup, DateTime startTime, string phase)>
            _simulations = new();

        // Timer interval in milliseconds (from config or default)
        private const int TICK_INTERVAL_MS = 2000;

        public SimulationService(
            IUserRepository userRepo,
            IDriverRepository driverRepo,
            ITripRepository tripRepo,
            INotificationService _,          // injected but not used here
            ITripService tripService,
            IRouteService routeService)
        {
            _tripRepo = tripRepo ?? throw new ArgumentNullException(nameof(tripRepo));
            _driverRepo = driverRepo ?? throw new ArgumentNullException(nameof(driverRepo));
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

            _simulations[tripId] = (driver.Id, route, 0, toPickup: true, DateTime.Now, "moving");
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

            _simulations[tripId] = (trip.DriverId.Value, route, 0, toPickup: false, DateTime.Now, "moving");
        }

		// ── Tick ─────────────────────────────────────────────────────────────

		public async Task Tick()
		{
			var activeIds = _simulations.Keys.ToList();
			var tasks = new List<Task>();
			var random = new Random();

			foreach (var tripId in activeIds)
			{
				if (!_simulations.TryGetValue(tripId, out var sim)) continue;

				// Lấy thông tin driver để biết loại xe và tốc độ
				var driver = await _userRepo.GetById(sim.driverId) as Driver;
				var vehicle = driver?.Vehicle;
				
				// Xác định tốc độ dựa trên loại xe
				double minSpeed = vehicle?.GetMinSpeed() ?? 25;
				double maxSpeed = vehicle?.GetMaxSpeed() ?? 70;
				
				// Kiểm tra phase hiện tại
				if (sim.phase == "waiting")
				{
					// Phase 2: Wait - đứng yên trong 30-60 giây
					var elapsed = DateTime.Now - sim.startTime;
					if (elapsed.TotalSeconds >= 30 + random.Next(30)) // 30-60 seconds
					{
						// Chuyển sang phase "moving" (Ride phase)
						var trip = await _tripRepo.GetById(tripId);
						if (trip != null)
						{
							var route = await _routeService.GetFullRouteAsync(trip.Pickup, trip.Destination);
							if (route != null && route.Points.Count >= 2)
							{
								_simulations[tripId] = (sim.driverId, route, 0, false, DateTime.Now, "moving");
								continue;
							}
						}
					}
					// Vẫn đang chờ, không di chuyển
					continue;
				}
				
				// Phase 1 hoặc 3: Moving
				// Pickup (toPickup=true): chạy chậm hơn (đường thành phố)
				// Ride (!toPickup): chạy nhanh hơn (đường chính)
				double speedMin = sim.toPickup ? minSpeed * 0.8 : minSpeed;
				double speedMax = sim.toPickup ? maxSpeed * 0.8 : maxSpeed;
				
				// Random tốc độ trong khoảng
				double speedKmH = speedMin + (speedMax - speedMin) * random.NextDouble();
				
				// Tính khoảng cách di chuyển trong TICK_INTERVAL_MS (2 giây)
				double distanceKm = speedKmH * (TICK_INTERVAL_MS / 1000.0 / 3600.0);

				// 1. Kiểm tra nếu đã đi hết lộ trình
				if (sim.index >= sim.route.Points.Count)
				{
					tasks.Add(HandleArrival(tripId, sim.toPickup));
					continue;
				}

				// 2. Cập nhật vị trí hiện tại
				var currentPoint = sim.route.Points[sim.index];
				tasks.Add(_driverRepo.UpdateDriverLocation(sim.driverId, currentPoint));

				// 3. Tăng index cho nhịp sau
				_simulations[tripId] = (sim.driverId, sim.route, sim.index + 1, sim.toPickup, sim.startTime, sim.phase);
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

        /// <summary>
        /// Tính thời gian chờ ngẫu nhiên (30-60 giây)
        /// </summary>
        private static TimeSpan GetWaitTime()
        {
            var random = new Random();
            int seconds = random.Next(30, 61); // 30-60 seconds
            return TimeSpan.FromSeconds(seconds);
        }

        /// <summary>
        /// Tính thời gian ước tính di chuyển dựa trên khoảng cách và tốc độ
        /// </summary>
        private static TimeSpan EstimateTravelTime(double distanceKm, double minSpeed, double maxSpeed)
        {
            // Sử dụng tốc độ trung bình
            double avgSpeed = (minSpeed + maxSpeed) / 2;
            double hours = distanceKm / avgSpeed;
            return TimeSpan.FromHours(hours);
        }

        private async Task HandleArrival(Guid tripId, bool toPickup)
        {
            // Lấy thông tin trip và driver
            var trip = await _tripRepo.GetById(tripId);
            if (trip == null)
            {
                _simulations.Remove(tripId);
                return;
            }

            if (toPickup)
            {
                // T0 -> T1: Driver đã đến điểm đón
                // Chuyển sang phase "waiting" (T1 -> T2)
                // Đứng yên 30-60 giây
                var route = new Route(trip.Pickup, trip.Pickup, 0, 0, new List<GeoLocation> { trip.Pickup });
                _simulations[tripId] = (trip.DriverId!.Value, route, 0, false, DateTime.Now, "waiting");
            }
            else
            {
                // T2 -> T3: Ride phase đã hoàn thành
                // Xóa simulation vì đã đến đích
                // Driver sẽ tự xác nhận hoàn thành chuyến
                _simulations.Remove(tripId);
            }
        }
    }
}
