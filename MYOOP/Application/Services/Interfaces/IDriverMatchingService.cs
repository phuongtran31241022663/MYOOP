using OOP.Domain.Entities;
using OOP.Domain.Enums;

namespace OOP.Application.Services.Interfaces
{
    public interface IDriverMatchingService
    {
        Task<Driver?> FindAvailableDriver(Location pickup, VehicleType vehicleType);

        Task<Driver?> MatchDriver(Trip trip);
    }
}