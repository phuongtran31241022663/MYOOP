using OOP.Domain.Entities;
namespace OOP.Domain.Interfaces
{
    public interface IUserRepository
    {
        Task<List<User>> GetAll();
        Task<User?> GetById(Guid userId);
        Task<User?> GetByPhone(string phone);
        Task<bool> ExistsByPhone(string phone);
        Task Add(User user);
        Task Update(User user);
        Task Remove(Guid userId);
    }
}