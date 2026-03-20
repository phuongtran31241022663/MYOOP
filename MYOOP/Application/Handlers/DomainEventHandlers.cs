using OOP.Application.Services.Interfaces;
using OOP.Domain.Entities;
using OOP.Domain.Events;
using OOP.Domain.Interfaces;
using OOP.Domain.Enums;

namespace OOP.Application.Handlers
{
    // Domain Event Handlers - xử lý các sự kiện từ Aggregate
    public interface IDomainEventHandler<in TEvent> where TEvent : DomainEvent
    {
        Task HandleAsync(TEvent @event);
    }

    public class TripRequestedEventHandler : IDomainEventHandler<TripRequestedEvent>
    {
        private readonly IDriverMatchingService _matchingService;
        private readonly INotificationService _notificationService;

        public TripRequestedEventHandler(
            IDriverMatchingService matchingService,
            INotificationService notificationService)
        {
            _matchingService = matchingService;
            _notificationService = notificationService;
        }

        public async Task HandleAsync(TripRequestedEvent @event)
        {
            // Tìm tài xế phù hợp
            var bestDriver = await _matchingService.FindAvailableDriver(
                @event.PickupLocation,
                @event.VehicleType,
                new List<Guid>());

            if (bestDriver != null)
            {
                // Gửi thông báo cho tài xế
                await _notificationService.NotifyDriver(
                    bestDriver.Id,
                    $"Bạn có yêu cầu mới: {@event.PickupLocation.Address} → {@event.DestinationLocation.Address} (Ước tính {@event.EstimatedFare:N0} VNĐ)");
            }
        }
    }

    public class TripMatchedEventHandler : IDomainEventHandler<TripMatchedEvent>
    {
        private readonly INotificationService _notificationService;
        private readonly IUserRepository _userRepo;

        public TripMatchedEventHandler(
            INotificationService notificationService,
            IUserRepository userRepo)
        {
            _notificationService = notificationService;
            _userRepo = userRepo;
        }

        public async Task HandleAsync(TripMatchedEvent @event)
        {
            // Thông báo cho hành khách
            var driver = await _userRepo.GetById(@event.DriverId) as Driver;
            if (driver != null)
            {
                await _notificationService.NotifyPassenger(
                    @event.AggregateId, // TripId
                    $"Tài xế {driver.Name} đã nhận chuyến. Số điện thoại: {driver.Phone}");
            }
        }
    }

    public class TripArrivedEventHandler : IDomainEventHandler<TripArrivedEvent>
    {
        private readonly INotificationService _notificationService;

        public TripArrivedEventHandler(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task HandleAsync(TripArrivedEvent @event)
        {
            await _notificationService.NotifyTripUpdate(
                @event.AggregateId,
                "Tài xế đã đến điểm đón.");
        }
    }

    public class TripStartedEventHandler : IDomainEventHandler<TripStartedEvent>
    {
        private readonly INotificationService _notificationService;

        public TripStartedEventHandler(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task HandleAsync(TripStartedEvent @event)
        {
            await _notificationService.NotifyTripUpdate(
                @event.AggregateId,
                "Chuyến đi đã bắt đầu.");
        }
    }

    public class TripCompletedEventHandler : IDomainEventHandler<TripCompletedEvent>
    {
        private readonly INotificationService _notificationService;

        public TripCompletedEventHandler(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task HandleAsync(TripCompletedEvent @event)
        {
            // Chỉ gửi notification - business logic đã được xử lý trong TripService.CompleteTrip()
            await _notificationService.NotifyTripUpdate(
                @event.AggregateId,
                $"Chuyến đi hoàn thành. Cước phí: {@event.Fare:N0} VNĐ.");
        }
    }

    public class TripCancelledEventHandler : IDomainEventHandler<TripCancelledEvent>
    {
        private readonly INotificationService _notificationService;

        public TripCancelledEventHandler(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task HandleAsync(TripCancelledEvent @event)
        {
            // Chỉ gửi notification - driver status reset đã được xử lý trong TripService.CancelTrip()
            await _notificationService.NotifyTripUpdate(
                @event.AggregateId,
                $"Chuyến đi đã bị hủy. Lý do: {@event.Reason}");
        }
    }

    public class TripTimeoutEventHandler : IDomainEventHandler<TripTimeoutEvent>
    {
        private readonly INotificationService _notificationService;

        public TripTimeoutEventHandler(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task HandleAsync(TripTimeoutEvent @event)
        {
            await _notificationService.NotifyTripUpdate(
                @event.AggregateId,
                "Không có tài xế nhận. Yêu cầu đã hết thời gian.");
        }
    }

    public class DriverLocationUpdatedEventHandler : IDomainEventHandler<DriverLocationUpdatedEvent>
    {
        // Chỉ log - driver đã biết vị trí của mình, không cần notification
        public Task HandleAsync(DriverLocationUpdatedEvent @event)
        {
            // Log vị trí cập nhật (có thể dùng cho debugging/monitoring)
            System.Diagnostics.Debug.WriteLine(
                $"[DriverLocationUpdated] Driver {@event.AggregateId}: {@event.Location.Address}");
            return Task.CompletedTask;
        }
    }

    public class DriverStatusChangedEventHandler : IDomainEventHandler<DriverStatusChangedEvent>
    {
        // Chỉ log - driver đã biết mình thay đổi trạng thái, notification gây spam
        public Task HandleAsync(DriverStatusChangedEvent @event)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[DriverStatusChanged] Driver {@event.AggregateId}: {@event.OldStatus} -> {@event.NewStatus}");
            return Task.CompletedTask;
        }
    }
}