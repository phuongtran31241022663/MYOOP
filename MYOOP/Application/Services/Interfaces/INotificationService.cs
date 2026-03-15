namespace OOP.Application.Services.Interfaces
{
    public interface INotificationService
    {
        Task NotifyPassenger(Guid passengerId, string message);

        Task NotifyDriver(Guid driverId, string message);

        Task NotifyTripUpdate(Guid tripId, string message);
    }
}