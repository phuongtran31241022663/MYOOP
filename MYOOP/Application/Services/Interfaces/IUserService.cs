using OOP.Domain.Entities;
using OOP.Domain.Enums;

public interface IUserService
{
    Task<Passenger> RegisterPassenger(string name, string phone, string password);

    Task<Driver> RegisterDriver(string name, string phone, string password,
                         Vehicle vehicle, GeoLocation defaultLocation, string license);
    Task<User> Login(string phone, string password);
    Task<User?> GetUserProfile(Guid userId);

    Task UpdateProfileName(Guid userId, string name);
    Task ChangePhone(Guid userId, string newPhone);
    Task ChangePassword(Guid userId, string oldPassword, string newPassword);
    Task UpdateUserProfile(Guid userId, string name, string phone);

    Task UpdateDriverVehicle(Guid driverId, Vehicle vehicle);
    Task UpdateDriverVehicleInfo(
        Guid driverId,
        string vehicleType,
        string plateNumber,
        string brand,
        string model,
        string color,
        int capacity);
    Task UpdateDriverLicense(Guid driverId, string license);
    Task UpdateDriverLocation(Guid driverId, GeoLocation location);
    Task TopUpDriverWallet(Guid driverId, decimal amount);
    Task UpdateDriverStatus(Guid driverId, DriverStatus status);

    /// <summary>
    /// Force recover driver status to Active (bypass status transition guard).
    /// Used for recovery from stale OnTrip state when trips are unexpectedly ended.
    /// </summary>
    Task ForceRecoverDriverStatus(Guid driverId);
}
