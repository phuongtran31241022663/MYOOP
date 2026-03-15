using OOP.Domain.Entities;
using OOP.Domain.Enums;

namespace OOP.Application.Services.Interfaces
{
    public interface IFareRuleService
    {
        Task<decimal> CalculateFare(Trip trip);

        Task<FareRule?> GetFareRule(VehicleType vehicleType);
    }
}