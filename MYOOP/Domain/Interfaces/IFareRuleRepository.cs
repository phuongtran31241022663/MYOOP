using OOP.Domain.Entities;
using OOP.Domain.Enums;

namespace OOP.Domain.Interfaces
{
    public interface IFareRuleRepository
    {
        Task<List<FareRule>> GetAll();
        Task<FareRule?> GetById(Guid id);
        Task<FareRule?> GetByVehicleType(VehicleType type);
        Task Add(FareRule rule);
        Task Update(FareRule rule);
        Task Remove(Guid id);
    }
}