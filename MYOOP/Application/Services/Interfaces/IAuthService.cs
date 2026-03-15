using OOP.Domain.Entities;

namespace OOP.Application.Services.Interfaces
{
    public interface IAuthService
    {
        Task<Passenger> RegisterPassenger(
            string fullname,
            string phone,
            string password);

        Task<Driver> RegisterDriver(
            string fullname,
            string phone,
            string password,
            Vehicle vehicle,
            Location location,
string licenseNumber
            );

        Task<User> Login(string phone, string password);

        void Logout(Guid userId);

        Task ResetPassword(Guid userId, string newPassword);
    }
}