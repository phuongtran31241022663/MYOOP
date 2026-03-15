using OOP.Domain.Entities;

namespace OOP.Application.Services.Interfaces
{
    public interface IUserService
    {
        // --- Quản lý Profile ---

        Task<User?> GetUserProfile(Guid userId);

        Task UpdateUserProfile(Guid userId, string name, string phone);

        Task ResetPassword(Guid userId, string newPassword);

        Task DeactivateUser(Guid userId);
    }
}