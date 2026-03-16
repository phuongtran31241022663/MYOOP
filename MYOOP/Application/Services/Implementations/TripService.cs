﻿using OOP.Application.Services.Interfaces;
using OOP.Application.Validators;
using OOP.Domain.Entities;
using OOP.Domain.Enums;
using OOP.Domain.Interfaces;

namespace OOP.Application.Services
{
    // TripService là orchestrator — điều phối các service khác
    // Không chứa business logic — delegate sang entity methods và validators
    public class TripService : ITripService
    {
        private readonly ITripRepository _tripRepo;
        private readonly IUserRepository _userRepo;
        private readonly IFareRuleRepository _fareRuleRepo;
        private readonly IFareService _fareService;
        private readonly IPaymentService _paymentService;
        private readonly IDriverMatchingService _matchingService;
        private readonly INotificationService _notificationService;
        private readonly IRouteService _routeService;
        public TripService(
            ITripRepository tripRepo,
            IUserRepository userRepo,
            IFareRuleRepository fareRuleRepo,
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
        public async Task<Trip> RequestTrip(
      Guid passengerId,
      Location pickup,
      Location destination,
      VehicleType vehicleType)
        {
            // 1. Lấy thông tin lộ trình dự kiến từ RouteService
            var route = await _routeService.GetFullRouteAsync(pickup, destination);
            double estimatedDistance = route?.Distance ?? 0;

            // 2. Validate với đầy đủ tham số (bao gồm distance)
            TripValidator.ValidateRequest(pickup, destination, vehicleType, estimatedDistance);

            var passenger = await _userRepo.GetById(passengerId)
                            ?? throw new KeyNotFoundException("Không tìm thấy hành khách.");

            if (!passenger.IsActive)
                throw new InvalidOperationException("Tài khoản hành khách đã bị khóa.");

            var rule = await _fareRuleRepo.GetByVehicleType(vehicleType)
                       ?? throw new InvalidOperationException($"Không tìm thấy bảng giá cho loại xe '{vehicleType}'.");

            // 3. Khởi tạo Trip với estimatedDistance mới có
            var trip = new Trip(passengerId, rule.Id, pickup, destination, vehicleType, estimatedDistance);

            await _tripRepo.Add(trip);

            await _notificationService.NotifyPassenger(
                passengerId, $"Yêu cầu đặt xe đã được ghi nhận. Quãng đường dự kiến: {estimatedDistance:N2} km.");

            return trip;
        }

        // --- 2. Gán tài xế vào trip ---
        public async Task AssignDriver(Guid tripId, Guid driverId)
        {
            var trip = await GetTripOrThrow(tripId);
            var driver = await GetDriverOrThrow(driverId);

            TripValidator.ValidateDriverAssignment(trip, driver);

            trip.AssignDriver(driverId);
            driver.SetBusy();

            await _tripRepo.Update(trip);
            await _userRepo.Update(driver);

            await _notificationService.NotifyTripUpdate(
                tripId, $"Tài xế {driver.Name} đã nhận chuyến của bạn.");
        }

        // --- 3. Tài xế đến nơi đón ---
        public async Task MarkArrived(Guid tripId)
        {
            var trip = await GetTripOrThrow(tripId);

            trip.MarkArrived();

            await _tripRepo.Update(trip);

            await _notificationService.NotifyTripUpdate(
                tripId, "Tài xế đã đến điểm đón. Vui lòng ra xe.");
        }

        // --- 4. Bắt đầu chuyến ---
        public async Task StartTrip(Guid tripId)
        {
            var trip = await GetTripOrThrow(tripId);

            TripValidator.ValidateStart(trip);

            trip.StartTrip();

            await _tripRepo.Update(trip);

            await _notificationService.NotifyTripUpdate(tripId, "Chuyến đi đã bắt đầu.");
        }

        // --- 5. Hoàn thành chuyến ---
        public async Task CompleteTrip(Guid tripId)
        {
            var trip = await GetTripOrThrow(tripId);
            double duration = 1;
            if (trip.StartedAt.HasValue)
            {
                duration = Math.Max((DateTime.Now - trip.StartedAt.Value).TotalMinutes, 1);
            }
            if (trip.Distance <= 0)
            {
                var route = await _routeService.GetFullRouteAsync(trip.PickupLocation, trip.DestinationLocation);
                trip.ApplyDistance(route?.Distance ?? 0);
            }

            await _fareService.CalculateFare(trip);

            trip.CompleteTrip(trip.Distance, duration, trip.Fare);

            await _tripRepo.Update(trip);

            var payment = await _paymentService.CreatePayment(trip);

            await _paymentService.ProcessPayment(payment.Id);

            if (trip.DriverId.HasValue)
            {
                var driver = await GetDriverOrThrow(trip.DriverId.Value);
                driver.TopUpWallet(payment.DriverIncome);
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

        // --- 6. Hủy chuyến ---
        public async Task CancelTrip(Guid tripId, string reason)
        {
            var trip = await GetTripOrThrow(tripId);

            TripValidator.ValidateCancellation(trip);

            trip.CancelTrip(reason);

            if (trip.DriverId.HasValue)
            {
                var driver = await GetDriverOrThrow(trip.DriverId.Value);
                driver.SetAvailable();
                await _userRepo.Update(driver);
            }

            await _tripRepo.Update(trip);

            await _notificationService.NotifyTripUpdate(tripId, $"Chuyến đi đã bị hủy: {reason}");
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
            if (driver.Status != DriverStatus.Available) return new List<Trip>();

            var trips = await _tripRepo.GetAll();
            return trips
                .Where(t => t.Status == TripStatus.Requested &&
                            t.VehicleType == driver.Vehicle.Type)
                .OrderBy(t => t.RequestedAt)
                .ToList();
        }

        public async Task<List<Trip>> GetByUserId(Guid id)
        {
            var byPassenger = await _tripRepo.GetByPassengerId(id);
            var byDriver = await _tripRepo.GetByDriverId(id);

            return byPassenger
                .UnionBy(byDriver, t => t.Id)
                .OrderByDescending(t => t.RequestedAt)
                .ToList();
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
    }
}

