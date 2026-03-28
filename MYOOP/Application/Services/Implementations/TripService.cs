using OOP.Application.Services.Interfaces;
using OOP.Application.Builders;
using OOP.Domain.Entities;
using OOP.Domain.Enums;
using OOP.Domain.Events;
using OOP.Domain.Interfaces;
using OOP.Domain.Policies;
using OOP.Infrastructure;
using System.Diagnostics;

namespace OOP.Application.Services
{
    /// <summary>
    /// Orchestrator — điều phối các service khác.
    /// Không chứa business logic — delegate sang entity methods và validators.
    /// </summary>
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
        private readonly IEventDispatcher _eventDispatcher;

        // Race condition: serialize driver assignment to prevent
        // two drivers from accepting the same trip simultaneously.
        private readonly SemaphoreSlim _assignLock = new(1, 1);

        public TripService(
            ITripRepository tripRepo,
            IUserRepository userRepo,
            IFareRepository fareRuleRepo,
            IFareService fareService,
            IPaymentService paymentService,
            IDriverMatchingService matchingService,
            INotificationService notificationService,
            IRouteService routeService,
            IEventDispatcher eventDispatcher)
        {
            _tripRepo = tripRepo ?? throw new ArgumentNullException(nameof(tripRepo));
            _userRepo = userRepo ?? throw new ArgumentNullException(nameof(userRepo));
            _fareRuleRepo = fareRuleRepo ?? throw new ArgumentNullException(nameof(fareRuleRepo));
            _fareService = fareService ?? throw new ArgumentNullException(nameof(fareService));
            _paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));
            _matchingService = matchingService ?? throw new ArgumentNullException(nameof(matchingService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _routeService = routeService ?? throw new ArgumentNullException(nameof(routeService));
            _eventDispatcher = eventDispatcher ?? throw new ArgumentNullException(nameof(eventDispatcher));
        }

        // ── 1. Hành khách đặt xe ─────────────────────────────────────────────

        public async Task<Trip> RequestTrip(
            Guid passengerId,
            GeoLocation pickup,
            GeoLocation destination,
            VehicleType VehicleType)
        {
            var route = await _routeService.GetFullRouteAsync(pickup, destination);
            double distance = route?.Distance ?? 0;
            if (distance <= 0)
                throw new InvalidOperationException("Không thể xác định lộ trình.");

            var passenger = await _userRepo.GetById(passengerId)
                ?? throw new KeyNotFoundException("Không tìm thấy hành khách.");

            if (passenger is not Passenger p || !p.IsActive)
                throw new InvalidOperationException("Tài khoản hành khách đã bị khóa.");

            var rule = await _fareRuleRepo.GetByVehicleType(VehicleType)
                ?? throw new InvalidOperationException($"Không tìm thấy bảng giá cho loại xe '{VehicleType}'.");

            var fare = rule.CalculateFare(distance);
            var tripBuilder = new TripRequestBuilder();
            var trip = tripBuilder
                .SetPassenger(passengerId)
                .SetFareRule(rule.Id)
                .SetPickup(pickup)
                .SetDestination(destination)
                .SetVehicleType(VehicleType)
                .SetDistance(distance)
                .SetFare(fare)
                .Build();

            // Lưu trip vào repository
            await _tripRepo.Add(trip);

            var events = trip.DomainEvents.ToList();
            trip.ClearDomainEvents();
            foreach (var evt in events)
            {
                await _eventDispatcher.DispatchAsync(evt);
            }

            // Thông báo cho passenger - đang chờ driver nhận
            string message = $"Đặt xe thành công! Đang chờ tài xế nhận cuốc... Quãng đường: {distance:N2} km. Cước ước tính: {fare:N0} VNĐ.";
            await _notificationService.NotifyPassenger(passengerId, message);

            // Giai đoạn tìm tài xế (Sequential Dispatch):
            // 1. Lấy tất cả tài xế có loại xe phù hợp
            // 2. DriverMatchingPolicy loại bỏ người không đủ điều kiện (busy, locked, rejected)
            // 3. Task.WhenAll tính khoảng cách song song
            // 4. Sắp xếp theo khoảng cách (gần nhất)
            // 5. Gửi request lần lượt cho tài xế gần nhất
            await NotifyNearestDriver(trip);

            // Log trip creation
            Logger.Instance.Info($"Chuyến đi được tạo: {trip.Id} - Mã hành khách: {passengerId}, Khoảng cách: {distance:F2}km, Giá: {fare:N0} VNĐ");

            return trip;
        }

        // ── 2. Gán tài xế ────────────────────────────────────────────────────

        /// <summary>
        /// Gán tài xế cho chuyến đi.
        /// Sử dụng SemaphoreSlim để ngăn race condition:
        /// Hai tài xế cùng nhấn "Accept" một chuyến tại cùng một thời điểm.
        /// Tài xế thứ hai sẽ nhận được thông báo "Chuyến đi đã có người nhận".
        /// </summary>
        /// <returns>true nếu gán thành công, false nếu chuyến đã có tài xế khác nhận</returns>
        /// <exception cref="InvalidOperationException">Ném khi tài xế không hợp lệ</exception>
        public async Task<bool> TryAssignDriver(Guid tripId, Guid driverId)
        {
            await _assignLock.WaitAsync();
            try
            {
                var trip = await GetTripOrThrow(tripId);
                var driver = await GetDriverOrThrow(driverId);

                // Nếu Trip.DriverId đã khác null → chuyến đi đã có người nhận
                if (trip.DriverId.HasValue && trip.DriverId.Value != driverId)
                {
                    Debug.WriteLine($"[RaceCondition] Driver {driverId} bị từ chối: Trip {tripId} đã có tài xế {trip.DriverId}.");
                    return false;
                }

                if (trip.Status == TripStatus.Matched && trip.DriverId.HasValue && trip.DriverId.Value != driverId)
                    return false;

                if (trip.Status != TripStatus.Searching && trip.Status != TripStatus.Requested)
                    throw new InvalidOperationException("Chuyến đi không ở trạng thái có thể gán tài xế.");

                if (driver.Status != DriverStatus.Available)
                    throw new InvalidOperationException(
                        $"Tài xế hiện không sẵn sàng nhận chuyến (trạng thái: '{driver.Status}').");

                driver.SetOnTrip();
                trip.AssignDriver(driver);

                await _tripRepo.Update(trip);
                await _userRepo.Update(driver);

                // Dispatch domain events
                var events = trip.DomainEvents.ToList();
                trip.ClearDomainEvents();
                foreach (var evt in events)
                {
                    await _eventDispatcher.DispatchAsync(evt);
                }

                await _notificationService.NotifyTripUpdate(tripId, $"Tài xế {driver.Name} đã nhận chuyến.");
                await _notificationService.NotifyDriver(driverId, "Bạn đã nhận chuyến thành công!");

                return true;
            }
            finally
            {
                _assignLock.Release();
            }
        }

        /// <summary>
        /// Gán tài xế (phiên bản cũ - giữ để tương thích)
        /// </summary>
        [Obsolete("Sử dụng TryAssignDriver thay thế")]
        public async Task AssignDriver(Guid tripId, Guid driverId)
        {
            var result = await TryAssignDriver(tripId, driverId);
            if (!result)
                throw new InvalidOperationException("Chuyến đã được tài xế khác nhận.");
        }

        // ── 3. Tài xế từ chối ────────────────────────────────────────────────

        public async Task RejectTrip(Guid tripId, Guid driverId, string reason)
        {
            if (driverId == Guid.Empty)
                throw new ArgumentException("DriverId không hợp lệ.", nameof(driverId));

            var trip = await GetTripOrThrow(tripId);
            if (trip.Status is not (TripStatus.Requested or TripStatus.Searching))
                throw new InvalidOperationException("Chỉ có thể từ chối khi trip đang ở trạng thái Requested/Searching.");

            if (trip.Status == TripStatus.Requested)
                trip.MarkSearching();

            trip.AddRejectedDriver(driverId);
            await _tripRepo.Update(trip);

            await _notificationService.NotifyTripUpdate(
                tripId, $"Tài xế đã từ chối. Đang tìm tài xế khác... ({reason})");

            // Gửi request lần lượt: tìm tài xế gần nhất tiếp theo
            await NotifyNearestDriver(trip);
        }

        // ── 4. Tài xế đến nơi đón ────────────────────────────────────────────

        public async Task MarkArrived(Guid tripId)
        {
            var trip = await GetTripOrThrow(tripId);
            trip.MarkArrived();
            await _tripRepo.Update(trip);

            // Dispatch domain events
            var events = trip.DomainEvents.ToList();
            trip.ClearDomainEvents();
            foreach (var evt in events)
            {
                await _eventDispatcher.DispatchAsync(evt);
            }

            await _notificationService.NotifyTripUpdate(tripId, "Tài xế đã đến điểm đón.");
        }

        // ── 5. Bắt đầu chuyến ────────────────────────────────────────────────

        public async Task StartTrip(Guid tripId)
        {
            var trip = await GetTripOrThrow(tripId);
            trip.StartTrip();
            await _tripRepo.Update(trip);

            // Dispatch domain events
            var events = trip.DomainEvents.ToList();
            trip.ClearDomainEvents();
            foreach (var evt in events)
            {
                await _eventDispatcher.DispatchAsync(evt);
            }

            await _notificationService.NotifyTripUpdate(tripId, "Chuyến đi đã bắt đầu.");
        }

        // ── 6. Hoàn thành chuyến (chờ xác nhận tiền mặt) ────────────────────

        public async Task CompleteTrip(Guid tripId)
        {
            var trip = await GetTripOrThrow(tripId);
            Route? route = null;
            double speedKmH = new Random().Next(30, 50);
            double assumedDuration = (trip.Distance / speedKmH) * 60;
            trip.CompleteTrip(trip.Distance, assumedDuration, trip.Fare);

            await _tripRepo.Update(trip);

            if (trip.DriverId.HasValue)
            {
                var driver = await GetDriverOrThrow(trip.DriverId.Value);
                driver.ForceSetAvailable();
                await _userRepo.Update(driver);
            }

            // Bắn Event và Thông báo
            var events = trip.DomainEvents.ToList();
            trip.ClearDomainEvents();
            foreach (var evt in events)
            {
                await _eventDispatcher.DispatchAsync(evt);
            }

            await _notificationService.NotifyTripUpdate(
                tripId,
                $"Chuyến đi hoàn thành. Vui lòng xác nhận đã nhận {trip.Fare:N0} VNĐ tiền mặt từ khách.");
        }

        // ── 6b. Xác nhận thanh toán tiền mặt ────────────────────────────────

        public async Task ConfirmPayment(Guid tripId, decimal actualFare)
        {
            var trip = await GetTripOrThrow(tripId);

            if (trip.Status != TripStatus.Completed)
                throw new InvalidOperationException("Chỉ có thể xác nhận thanh toán khi chuyến đã hoàn thành.");
            if (actualFare <= 0)
                throw new ArgumentException("Cước thực tế không hợp lệ.", nameof(actualFare));
            if (actualFare != trip.Fare)
                throw new InvalidOperationException("Cước thực tế không khớp với cước đã chốt. Không thể cập nhật sau khi trip đã Completed.");
            var payment = await _paymentService.CreatePayment(trip);
            await _paymentService.ProcessPayment(payment.Id);
            if (trip.DriverId.HasValue)
            {
                var driver = await GetDriverOrThrow(trip.DriverId.Value);
                driver.PayCommission(trip.Fare, payment.CommissionRate);
                driver.AddTrip();
                await _userRepo.Update(driver);
            }
            var passengerUser = await _userRepo.GetById(trip.PassengerId);
            if (passengerUser is Passenger p)
            {
                p.AddTrip();
                await _userRepo.Update(p);
            }

            var events = trip.DomainEvents.ToList();
            trip.ClearDomainEvents();
            foreach (var evt in events)
            {
                await _eventDispatcher.DispatchAsync(evt);
            }

            await _notificationService.NotifyTripUpdate(
                tripId,
                $"Chuyến đi hoàn thành. Cước phí: {trip.Fare:N0} VNĐ. Đã thanh toán tiền mặt.");
        }

        // ── 7. Hủy chuyến ────────────────────────────────────────────────────

        public async Task CancelTrip(Guid tripId, string reason)
        {
            var trip = await GetTripOrThrow(tripId);
            trip.CancelTrip(reason);

            if (trip.DriverId.HasValue)
            {
                var driver = await GetDriverOrThrow(trip.DriverId.Value);
                driver.ForceSetAvailable();
                await _userRepo.Update(driver);
            }

            await _tripRepo.Update(trip);

            // Dispatch domain events
            var events = trip.DomainEvents.ToList();
            trip.ClearDomainEvents();
            foreach (var evt in events)
            {
                await _eventDispatcher.DispatchAsync(evt);
            }

            await _notificationService.NotifyTripUpdate(tripId, $"Chuyến đi đã bị hủy: {reason}");
        }

        // ── 8. Queries ────────────────────────────────────────────────────────

        public async Task<Trip?> GetTrip(Guid tripId) =>
            await _tripRepo.GetById(tripId);

        public async Task<List<Trip>> GetTripHistory(Guid userId)
        {
            var byPassenger = await _tripRepo.GetByPassengerId(userId);
            var byDriver = await _tripRepo.GetByDriverId(userId);

            return byPassenger
                .UnionBy(byDriver, t => t.Id)
                .OrderByDescending(t => t.RequestedAt)
                .ToList();
        }

        // Alias for interface compatibility
        public Task<List<Trip>> GetByUserId(Guid userId) => GetTripHistory(userId);

        public async Task<List<Trip>> GetActiveTripsForDriver(Guid driverId)
        {
            var driver = await GetDriverOrThrow(driverId);
            Debug.WriteLine($"[GetActiveTrips] Driver {driverId}: Status={driver.Status}, Vehicle={driver.Vehicle?.GetVehicleType()}");

            if (driver.Status != DriverStatus.Available)
            {
                Debug.WriteLine("[GetActiveTrips] Driver not Active, returning empty list");
                return new List<Trip>();
            }

            var trips = await _tripRepo.GetAll();
            var Active = MatchingPolicies.TripMatchingPolicy.FilterActiveTripsForDriver(trips, driver).ToList();

            Debug.WriteLine($"[GetActiveTrips] Found {Active.Count} trips for driver {driver.Name}");
            return Active;
        }

        public async Task<List<Driver>> GetNearbyDrivers(GeoLocation pickup, VehicleType VehicleType, double maxKm) =>
            await _matchingService.GetNearbyDrivers(pickup, VehicleType, maxKm);

        public async Task<Driver?> GetDriverForTrip(Guid tripId)
        {
            var trip = await _tripRepo.GetById(tripId);
            if (trip?.DriverId == null) return null;
            return await _userRepo.GetById(trip.DriverId.Value) as Driver;
        }

        // ── 9. Driver status / location (called from DriverDashboardForm) ────
        public async Task UpdateDriverLocation(Guid driverId, GeoLocation location)
        {
            if (location == null) throw new ArgumentNullException(nameof(location));
            var driver = await GetDriverOrThrow(driverId);
            driver.UpdateLocation(location);
            await _userRepo.Update(driver);
        }

        // ── 10. Expiry ────────────────────────────────────────────────────────

        public async Task<int> ExpireSearchingTrips(TimeSpan maxWait)
        {
            if (maxWait <= TimeSpan.Zero)
                throw new ArgumentException("Thời gian chờ phải lớn hơn 0.", nameof(maxWait));

            var now = DateTime.UtcNow;
            var trips = await _tripRepo.GetAll();
            var expired = trips
                .Where(t =>
                    t.Status is TripStatus.Searching or TripStatus.Requested &&
                    !t.DriverId.HasValue &&
                    (now - t.RequestedAt) >= maxWait)
                .ToList();

            foreach (var t in expired)
            {
                try
                {
                    t.TimeoutTrip();
                    await _tripRepo.Update(t);
                    await _notificationService.NotifyTripUpdate(t.Id, "Không có tài xế nhận. Yêu cầu đã hết thời gian.");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ExpireSearchingTrips] Trip {t.Id}: {ex.GetType().Name}: {ex.Message}");
                }
            }

            return expired.Count;
        }

        public async Task<int> ExpireMatchedTrips(TimeSpan maxWait)
        {
            if (maxWait <= TimeSpan.Zero)
                throw new ArgumentException("Thời gian chờ phải lớn hơn 0.", nameof(maxWait));

            var now = DateTime.UtcNow;
            var trips = await _tripRepo.GetAll();
            var expired = trips
                .Where(t =>
                    t.Status == TripStatus.Matched &&
                    t.DriverId.HasValue &&
                    t.MatchedAt.HasValue &&
                    (now - t.MatchedAt.Value) >= maxWait)
                .ToList();

            foreach (var t in expired)
            {
                try
                {
                    var driverId = t.DriverId!.Value;
                    t.AddRejectedDriver(driverId);
                    t.MarkSearching();
                    await _tripRepo.Update(t);

                    var driver = await GetDriverOrThrow(driverId);
                    driver.ForceSetAvailable();
                    await _userRepo.Update(driver);

                    await _notificationService.NotifyTripUpdate(t.Id, "Tài xế không phản hồi. Đang tìm tài xế khác...");

                    // Tuần tự: gửi request cho tài xế gần nhất tiếp theo
                    await NotifyNearestDriver(t);
                }
                catch { /* ignore per-trip errors */ }
            }

            return expired.Count;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private async Task<Trip> GetTripOrThrow(Guid tripId) =>
            await _tripRepo.GetById(tripId)
            ?? throw new KeyNotFoundException($"Không tìm thấy trip '{tripId}'.");

        private async Task<Driver> GetDriverOrThrow(Guid driverId)
        {
            var user = await _userRepo.GetById(driverId)
                ?? throw new KeyNotFoundException($"Không tìm thấy driver '{driverId}'.");
            return user as Driver
                ?? throw new InvalidOperationException($"User '{driverId}' không phải Driver.");
        }

        /// <summary>
        /// Gửi request cho tài xế gần nhất (tuần tự).
        /// Flow: DispatchToNearestDriver → gửi thông báo cho tài xế.
        /// Nếu không còn tài xế khả dụng → thông báo cho hành khách.
        /// </summary>
        private async Task NotifyNearestDriver(Trip trip)
        {
            var nearest = await _matchingService.DispatchToNearestDriver(trip);
            if (nearest != null)
            {
                await _notificationService.NotifyDriver(
                    nearest.Id,
                    $"Bạn có yêu cầu mới: {trip.Pickup.Name} → {trip.Destination.Name} (Ước tính {trip.Fare:N0} VNĐ)");
                Debug.WriteLine($"[SequentialDispatch] Đã thông báo tài xế {nearest.Name} cho Trip {trip.Id.ToString()[..8]}");
            }
            else
            {
                await _notificationService.NotifyTripUpdate(
                    trip.Id, "Đang tìm tài xế... Không còn tài xế gần bạn. Vui lòng chờ.");
                Debug.WriteLine($"[SequentialDispatch] Không còn tài xế cho Trip {trip.Id.ToString()[..8]}");
            }
        }
    }
}
