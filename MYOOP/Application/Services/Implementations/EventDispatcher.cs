using OOP.Domain.Events;
using OOP.Domain.Entities;
using OOP.Domain.Interfaces;
using OOP.Application.Services.Interfaces;

namespace OOP.Application.Services.Implementations
{
    /// <summary>
    /// Event dispatcher that routes events to registered handlers.
    /// For WinForms app without DI container - handlers are created directly.
    /// </summary>
    public class EventDispatcher : IEventDispatcher
    {
        private readonly IUserRepository _userRepo;
        private readonly ITripRepository _tripRepo;
        private readonly IDriverMatchingService _matchingService;
        private readonly INotificationService _notificationService;

        public EventDispatcher(
            IUserRepository userRepo,
            ITripRepository tripRepo,
            IDriverMatchingService matchingService,
            INotificationService notificationService)
        {
            _userRepo = userRepo;
            _tripRepo = tripRepo;
            _matchingService = matchingService;
            _notificationService = notificationService;
        }

        public async Task DispatchAsync<TEvent>(TEvent @event) where TEvent : DomainEvent
        {
            try
            {
                switch (@event)
                {
                    case TripRequestedEvent tripEvent:
                        await HandleAsync(tripEvent);
                        break;
                    case TripSearchingEvent searchingEvent:
                        await HandleAsync(searchingEvent);
                        break;
                    case TripMatchedEvent matchedEvent:
                        await HandleAsync(matchedEvent);
                        break;
                    case TripArrivedEvent arrivedEvent:
                        await HandleAsync(arrivedEvent);
                        break;
                    case TripStartedEvent startedEvent:
                        await HandleAsync(startedEvent);
                        break;
                    case TripCompletedEvent completedEvent:
                        await HandleAsync(completedEvent);
                        break;
                    case TripCancelledEvent cancelledEvent:
                        await HandleAsync(cancelledEvent);
                        break;
                    case TripTimeoutEvent timeoutEvent:
                        await HandleAsync(timeoutEvent);
                        break;
                    default:
                        System.Diagnostics.Debug.WriteLine($"[EventDispatcher] Unknown event type: {@event.GetType().Name}");
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EventDispatcher] Error dispatching {@event.GetType().Name}: {ex.Message}");
            }
        }

        private async Task HandleAsync(TripRequestedEvent @event)
        {
            System.Diagnostics.Debug.WriteLine($"[EventDispatcher] Handling TripRequestedEvent for trip {@event.AggregateId}");

            try
            {
                var bestDriver = await _matchingService.FindAndReserveDriver(
                    @event.Pickup,
                    @event.VehicleType,
                    new List<Guid>());

                if (bestDriver != null)
                {
                    await _notificationService.NotifyDriver(
                        bestDriver.Id,
                        $"Bạn có yêu cầu mới: {@event.Pickup.Address} → {@event.Destination.Address} (Ước tính {@event.Fare:N0} VNĐ)");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TripRequestedEventHandler] Error: {ex.Message}");
            }
        }

        private async Task HandleAsync(TripSearchingEvent @event)
        {
            System.Diagnostics.Debug.WriteLine($"[EventDispatcher] Handling TripSearchingEvent for trip {@event.AggregateId}");
            // TripSearchingEvent is informational - driver matching is already triggered by TripRequestedEvent
            // This handler can log or perform additional actions if needed
            await Task.CompletedTask;
        }

        private async Task HandleAsync(TripMatchedEvent @event)
        {
            System.Diagnostics.Debug.WriteLine($"[EventDispatcher] Handling TripMatchedEvent for trip {@event.AggregateId}");
            
            try
            {
                var driver = await _userRepo.GetById(@event.DriverId) as Driver;
                if (driver != null)
                {
                    await _notificationService.NotifyPassenger(
                        @event.AggregateId,
                        $"Tài xế {driver.Name} đã nhận chuyến. Số điện thoại: {driver.Phone}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TripMatchedEventHandler] Error: {ex.Message}");
            }
        }

        private async Task HandleAsync(TripArrivedEvent @event)
        {
            System.Diagnostics.Debug.WriteLine($"[EventDispatcher] Handling TripArrivedEvent for trip {@event.AggregateId}");
            
            try
            {
                await _notificationService.NotifyTripUpdate(
                    @event.AggregateId,
                    "Tài xế đã đến điểm đón.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TripArrivedEventHandler] Error: {ex.Message}");
            }
        }

        private async Task HandleAsync(TripStartedEvent @event)
        {
            System.Diagnostics.Debug.WriteLine($"[EventDispatcher] Handling TripStartedEvent for trip {@event.AggregateId}");
            
            try
            {
                await _notificationService.NotifyTripUpdate(
                    @event.AggregateId,
                    "Chuyến đi đã bắt đầu.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TripStartedEventHandler] Error: {ex.Message}");
            }
        }

        private async Task HandleAsync(TripCompletedEvent @event)
        {
            System.Diagnostics.Debug.WriteLine($"[EventDispatcher] Handling TripCompletedEvent for trip {@event.AggregateId}");
            
            try
            {
                await _notificationService.NotifyTripUpdate(
                    @event.AggregateId,
                    $"Chuyến đi hoàn thành. Cước phí: {@event.Fare:N0} VNĐ.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TripCompletedEventHandler] Error: {ex.Message}");
            }
        }

        private async Task HandleAsync(TripCancelledEvent @event)
        {
            System.Diagnostics.Debug.WriteLine($"[EventDispatcher] Handling TripCancelledEvent for trip {@event.AggregateId}");
            
            try
            {
                await _notificationService.NotifyTripUpdate(
                    @event.AggregateId,
                    $"Chuyến đi đã bị hủy. Lý do: {@event.Reason}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TripCancelledEventHandler] Error: {ex.Message}");
            }
        }

        private async Task HandleAsync(TripTimeoutEvent @event)
        {
            System.Diagnostics.Debug.WriteLine($"[EventDispatcher] Handling TripTimeoutEvent for trip {@event.AggregateId}");
            
            try
            {
                await _notificationService.NotifyTripUpdate(
                    @event.AggregateId,
                    "Không có tài xế nhận. Yêu cầu đã hết thời gian.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TripTimeoutEventHandler] Error: {ex.Message}");
            }
        }
    }
}
