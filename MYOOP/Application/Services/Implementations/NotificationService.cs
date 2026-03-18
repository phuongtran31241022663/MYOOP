﻿using OOP.Application.Services.Interfaces;
using OOP.Domain.Interfaces;


namespace OOP.Application.Services
{
    // WinForms không có push notification thật
    // NotificationService log ra console và có thể hook vào UI event sau
    public class NotificationService : INotificationService
    {
        private readonly IUserRepository _userRepo;
        private readonly ITripRepository _tripRepo;

        // Event để Presentation Layer subscribe — Form có thể hiện MessageBox khi có thông báo
        public event Action<Guid, string>? OnPassengerNotified;
        public event Action<Guid, string>? OnDriverNotified;
        public event Action<Guid, string>? OnTripUpdated;

        public NotificationService(IUserRepository userRepo, ITripRepository tripRepo)
        {
            _userRepo = userRepo ?? throw new ArgumentNullException(nameof(userRepo));
            _tripRepo = tripRepo ?? throw new ArgumentNullException(nameof(tripRepo));
        }

        public async Task NotifyPassenger(Guid passengerId, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("Nội dung thông báo không được để trống.");

            var user = await _userRepo.GetById(passengerId);
            if (user == null) return;

            var log = $"[PASSENGER] {user.Name}: {message}";
            Console.WriteLine(log);

            // Raise event để Form có thể hiển thị
            OnPassengerNotified?.Invoke(passengerId, message);
        }

        public async Task NotifyDriver(Guid driverId, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("Nội dung thông báo không được để trống.");

            var user = await _userRepo.GetById(driverId);
            if (user == null) return;

            var log = $"[DRIVER] {user.Name}: {message}";
            Console.WriteLine(log);

            OnDriverNotified?.Invoke(driverId, message);
        }

        public async Task NotifyTripUpdate(Guid tripId, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("Nội dung thông báo không được để trống.");

            var trip = await _tripRepo.GetById(tripId);
            if (trip == null) return;

            Console.WriteLine($"[TRIP {tripId.ToString()[..8]}] {message}");

            OnTripUpdated?.Invoke(tripId, message);
        }
    }
    public class TripNotificationSubscriber
    {
        private readonly INotificationService _notification;
        private readonly ITripRepository _tripRepo;

        public TripNotificationSubscriber(
            INotificationService notification,
            ITripRepository tripRepo)
        {
            _notification = notification;
            _tripRepo = tripRepo;
        }

        public async Task Handle(Guid tripId, string message)
        {
            var trip = await _tripRepo.GetById(tripId);
            if (trip == null) return;

            await _notification.NotifyPassenger(trip.PassengerId, message);

            if (trip.DriverId.HasValue)
                await _notification.NotifyDriver(trip.DriverId.Value, message);
        }
    }
}
