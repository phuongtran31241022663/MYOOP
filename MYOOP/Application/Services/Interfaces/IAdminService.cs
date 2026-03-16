using OOP.Domain.Entities;

namespace OOP.Application.Services.Interfaces
{
    public interface IAdminService
    {
        Task<List<User>> GetAllUsers();
        Task<List<Trip>> GetAllTrips();
        Task<List<Fare>> GetFareRules();
        Task UpdateFareRule(Fare rule);
        Task ActivateUser(Guid userId);
        Task DeactivateUser(Guid targetUserId, Guid currentAdminId);
    }
}