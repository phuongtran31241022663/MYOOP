using OOP.Application.Services.Interfaces;
using OOP.Domain.Entities;
using OOP.Domain.Enums;
using OOP.Domain.Interfaces;
using System.Diagnostics;

namespace OOP.Application.Services
{
    // TripService là orchestrator — điều phối các service khác
    // Không chứa business logic — delegate sang entity methods và validators
    public class TripService : ITripService
    {
        private readonly ITripRepository _tripRepo;
        private readonly IUserRepository _userRepo;
        private readonly IFareRepository _fareRuleRepo;
        private readonly IFareService _fareService;
        private readonly IPaymentService _paymentService;
        private readonly IDriverMatchingService _matchingService;
        private readonly INotificationService _notificationService;
        private readonly IRouteService _routeService;
        public TripService(
            ITripRepository tripRepo,
            IUserRepository userRepo,
            IFareRepository fareRuleRepo,
            IFareService fareService,
            IPaymentService paymentService,
            IDriverMatchingService matchingService,
            INotificationService notificationService,
            IRouteService routeService)
        {
            _tripRepo = tripRepo ?? throw new ArgumentNullException(nameof(tripRepo));
            _userRepo = userRepo ?? throw new ArgumentNullException(nameof(userRepo));
            _fareRuleRepo = fareRuleRepo ?? throw new ArgumentNullException(nameof(fareRuleRepo));
            _fareService = fareService ?? throw new ArgumentNullException(nameof(fareService));
            _paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));
            _matchingService = matchingService ?? throw new ArgumentNullException(nameof(matchingService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _routeService = routeService;
        }

        // --- 1. Hành khách đặt xe ---
        public async Task<Trip> RequestTrip(Guid passengerId, GeoLocation pickup, GeoLocation destination, VehicleType vehicleType)
        {
            var route = await _routeService.GetFullRouteAsync(pickup, destination);
            double estimatedDistance = route?.Distance ?? 0;

            if (estimatedDistance <= 0) throw new InvalidOperationException("Không thể xác định lộ trình.");

            var passenger = await _userRepo.GetById(passengerId)
                            ?? throw new KeyNotFoundException("Không tìm thấy hành khách.");

            if (passenger is not Passenger p || !p.IsActive)
                throw new InvalidOperationException("Tài khoản hành khách đã bị khóa.");

            var rule = await _fareRuleRepo.GetByVehicleType(vehicleType)
                       ?? throw new InvalidOperationException($"Không tìm thấy bảng giá cho loại xe '{vehicleType}'.");

            var trip = new Trip(passengerId, rule.Id, pickup, destination, vehicleType, estimatedDistance);
            var estimatedFare = rule.CalculateFare(estimatedDistance);
            trip.ApplyFare(estimatedFare);
            trip.MarkSearching();

            await _tripRepo.Add(trip);

            await _notificationService.NotifyPassenger(
                passengerId, $"Yêu cầu đặt xe đã được ghi nhận. Quãng đường dự kiến: {estimatedDistance:N2} km.");

            // Tìm và gán driver TRƯỚC KHI gửi notification
            var bestDriver = await _matchingService.FindAvailableDriver(pickup, vehicleType, trip.RejectedDriverIds);
            if (bestDriver != null)
            {
                // Gán driver vào trip trước
                await AssignDriver(trip.Id, bestDriver.Id);
                
                // Sau đó mới gửi notification
                await _notificationService.NotifyDriver(
                    bestDriver.Id,
                    $"Bạn có yêu cầu mới: {pickup.Address} → {destination.Address} (Ước tính {estimatedFare:N0} VNĐ)");
            }

            return trip;
        }

        // --- 2. Gán tài xế vào trip ---
        public async Task AssignDriver(Guid tripId, Guid driverId)
        {
            var trip = await GetTripOrThrow(tripId);
            var driver = await GetDriverOrThrow(driverId);

            if (trip.Status != TripStatus.Searching && trip.Status != TripStatus.Requested)
                throw new InvalidOperationException("Chuyến đi không ở trạng thái có thể gán tài xế.");

            driver.SetBusy();
            trip.AssignDriver(driver);

            await _tripRepo.Update(trip);
            await _userRepo.Update(driver);

            await _notificationService.NotifyTripUpdate(tripId, $"Tài xế {driver.Name} đã nhận chuyến.");
        }

        public async Task RejectTrip(Guid tripId, Guid driverId, string reason)
        {
            if (driverId == Guid.Empty)
                throw new ArgumentException("DriverId không hợp lệ.", nameof(driverId));

            var trip = await GetTripOrThrow(tripId);
            if (trip.Status != TripStatus.Requested && trip.Status != TripStatus.Searching)
                throw new InvalidOperationException("Chỉ có thể từ chối khi trip đang ở trạng thái Requested/Searching.");

            if (trip.Status == TripStatus.Requested)
                trip.MarkSearching();

            trip.AddRejectedDriver(driverId);
            await _tripRepo.Update(trip);

            await _notificationService.NotifyTripUpdate(
                tripId, $"Tài xế đã từ chối chuyến. Đang tìm tài xế khác... ({reason})");

            var nextDriver = await _matchingService.FindAvailableDriver(
                trip.PickupLocation, trip.VehicleType, trip.RejectedDriverIds);

            if (nextDriver != null)
            {
                await _notificationService.NotifyDriver(
                    nextDriver.Id,
                    $"Bạn có yêu cầu mới: {trip.PickupLocation.Address} → {trip.DestinationLocation.Address} (Ước tính {trip.Fare:N0} VNĐ)");
            }
        }

        // --- 3. Tài xế đến nơi đón ---
        public async Task MarkArrived(Guid tripId)
        {
            var trip = await GetTripOrThrow(tripId);
            trip.MarkArrived();
            await _tripRepo.Update(trip);
            await _notificationService.NotifyTripUpdate(tripId, "Tài xế đã đến điểm đón.");
        }

        // --- 4. Bắt đầu chuyến ---
        public async Task StartTrip(Guid tripId)
        {
            var trip = await GetTripOrThrow(tripId);
            trip.StartTrip();
            await _tripRepo.Update(trip);
            await _notificationService.NotifyTripUpdate(tripId, "Chuyến đi đã bắt đầu.");
        }

        // --- 5. Hoàn thành chuyến ---
        public async Task CompleteTrip(Guid tripId)
        {
            var trip = await GetTripOrThrow(tripId);

            double duration = trip.StartedAt.HasValue
                ? Math.Max((DateTime.Now - trip.StartedAt.Value).TotalMinutes, 1)
                : 1;
            if (trip.Distance <= 0)
            {
                var route = await _routeService.GetFullRouteAsync(trip.PickupLocation, trip.DestinationLocation);
                trip.ApplyDistance(route?.Distance ?? 0);
            }
            trip.CompleteTrip(trip.Distance, duration, trip.Fare);
            await _tripRepo.Update(trip);

            var payment = await _paymentService.CreatePayment(trip);
            await _paymentService.ProcessPayment(payment.Id);

            if (trip.DriverId.HasValue)
            {
                var driver = await GetDriverOrThrow(trip.DriverId.Value);

                driver.PayCommission(trip.Fare, payment.CommissionRate);
                driver.AddTrip();
                driver.SetAvailable();

                await _userRepo.Update(driver);
            }

            var passenger = await _userRepo.GetById(trip.PassengerId);
            if (passenger is Passenger p)
            {
                p.AddTrip();
                await _userRepo.Update(p);
            }

            await _notificationService.NotifyTripUpdate(
                tripId, $"Chuyến đi hoàn thành. Cước phí: {trip.Fare:N0} VNĐ.");
        }

        public async Task CancelTrip(Guid tripId, string reason)
        {
            var trip = await GetTripOrThrow(tripId);
            trip.CancelTrip(reason);

            if (trip.DriverId.HasValue)
            {
                var driver = await GetDriverOrThrow(trip.DriverId.Value);
                driver.SetAvailable();
                await _userRepo.Update(driver);
            }
            await _tripRepo.Update(trip);
        }

        // --- 7. Truy vấn ---
        public async Task<Trip?> GetTrip(Guid tripId)
        {
            return await _tripRepo.GetById(tripId);
        }

        public async Task<List<Trip>> GetTripHistory(Guid userId)
        {
            var byPassenger = await _tripRepo.GetByPassengerId(userId);
            var byDriver = await _tripRepo.GetByDriverId(userId);

            return byPassenger
                .UnionBy(byDriver, t => t.Id)
                .OrderByDescending(t => t.RequestedAt)
                .ToList();
        }

        public async Task<List<Trip>> GetAvailableTripsForDriver(Guid driverId)
        {
            var driver = await GetDriverOrThrow(driverId);
            Debug.WriteLine($"[GetAvailableTrips] Driver {driverId}: Status={driver.Status}, Vehicle={driver.Vehicle?.Type}");

            if (driver.Status != DriverStatus.Available)
            {
                Debug.WriteLine($"[GetAvailableTrips] Driver not Available, returning empty list");
                return new List<Trip>();
            }

            var trips = await _tripRepo.GetAll();
            var availableTrips = trips
                .Where(t => driver.Vehicle != null &&
                            (t.Status == TripStatus.Requested || t.Status == TripStatus.Searching) &&
                            t.VehicleType == driver.Vehicle.Type &&
                            !t.RejectedDriverIds.Contains(driver.Id))
                .OrderBy(t => t.RequestedAt)
                .ToList();

            Debug.WriteLine($"[GetAvailableTrips] Found {availableTrips.Count} trips for driver {driver.Name}");
            foreach (var trip in availableTrips)
            {
                Debug.WriteLine($"[GetAvailableTrips]   Trip {trip.Id}: {trip.PickupLocation.Address} -> {trip.DestinationLocation.Address}, Status={trip.Status}");
            }

            return availableTrips;
        }

        public async Task<List<Driver>> GetNearbyDrivers(
            GeoLocation pickup,
            VehicleType vehicleType,
            double maxKm)
        {
            return await _matchingService.GetNearbyDrivers(pickup, vehicleType, maxKm);
        }

        public async Task<Driver?> GetDriverForTrip(Guid tripId)
        {
            var trip = await _tripRepo.GetById(tripId);
            if (trip == null || !trip.DriverId.HasValue) return null;

            var user = await _userRepo.GetById(trip.DriverId.Value);
            return user as Driver;
        }

        public async Task<int> ExpireSearchingTrips(TimeSpan maxWait)
        {
            if (maxWait <= TimeSpan.Zero)
                throw new ArgumentException("Thời gian chờ phải lớn hơn 0.", nameof(maxWait));

            var now = DateTime.UtcNow;
            var trips = await _tripRepo.GetAll();

            var expired = trips
                .Where(t =>
                    (t.Status == TripStatus.Searching || t.Status == TripStatus.Requested) &&
                    !t.DriverId.HasValue &&
                    (now - t.RequestedAt) >= maxWait)
                .ToList();

            foreach (var t in expired)
            {
                try
                {
                    t.TimeoutTrip();
                    await _tripRepo.Update(t);
                    await _notificationService.NotifyTripUpdate(
                        t.Id, "Không có tài xế nhận. Yêu cầu đã hết thời gian.");
                }
                catch
                {
                    // ignore per-trip errors to avoid blocking other expirations
                }
            }

            return expired.Count;
        }

        // Duplicate of GetTripHistory - kept for interface compatibility
        public async Task<List<Trip>> GetByUserId(Guid userId)
        {
            return await GetTripHistory(userId);
        }

        // --- Helpers ---

        private async Task<Trip> GetTripOrThrow(Guid tripId)
        {
            return await _tripRepo.GetById(tripId)
                   ?? throw new KeyNotFoundException($"Không tìm thấy trip '{tripId}'.");
        }

        private async Task<Driver> GetDriverOrThrow(Guid driverId)
        {
            var user = await _userRepo.GetById(driverId)
                       ?? throw new KeyNotFoundException($"Không tìm thấy driver '{driverId}'.");

            return user as Driver
                   ?? throw new InvalidOperationException($"User '{driverId}' không phải Driver.");
        }
        public async Task UpdateDriverStatus(Guid driverId, DriverStatus status)
        {
            var driver = await GetDriverOrThrow(driverId);
            switch (status)
            {
                case DriverStatus.Available:
                    driver.SetAvailable();
                    break;
                case DriverStatus.Busy:
                    driver.SetBusy();
                    break;
                case DriverStatus.Offline:
                    // Driver không thể tự set offline - chỉ hệ thống (khi driver đóng app) mới gọi được
                    throw new InvalidOperationException("Tài xế không thể tự ngắt kết nối. Vui lòng đóng ứng dụng.");
                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status, "Trạng thái tài xế không hợp lệ.");
            }

            await _userRepo.Update(driver);
        }
        public async Task UpdateDriverLocation(Guid driverId, GeoLocation location)
        {
            if (location == null) throw new ArgumentNullException(nameof(location));
            var driver = await GetDriverOrThrow(driverId);

            driver.UpdateLocation(location);
            await _userRepo.Update(driver);
        }
    }
}

