using OOP.Domain.Entities;
using OOP.Domain.Enums;

namespace OOP.Application.Services.Interfaces
{
    public interface ITripService
    {
        Task<Trip> RequestTrip(
            Guid passengerId,
            Location pickup,
            Location destination,
            VehicleType vehicleType);

        Task AssignDriver(Guid tripId, Guid driverId);
        Task MarkArrived(Guid tripId);
        Task StartTrip(Guid tripId);
        Task CompleteTrip(Guid tripId);
        Task CancelTrip(Guid tripId, string reason);
        Task<Trip?> GetTrip(Guid tripId);
        Task<List<Trip>> GetTripHistory(Guid userId);
    }

}
