using OOP.Domain.Entities;
using OOP.Domain.Enums;

namespace OOP.Domain.Interfaces
{
    /// <summary>
    /// Repository interface for Driver-specific operations.
    /// Provides better separation of concerns by isolating driver-related
    /// data access from general user operations.
    /// </summary>
    public interface IDriverRepository
    {
        /// <summary>
        /// Gets all active drivers filtered by vehicle type.
        /// </summary>
        /// <param name="VehicleType">The vehicle type to filter by (e.g., "Motorbike", "Car").</param>
        /// <returns>List of active drivers matching the vehicle type.</returns>
        Task<List<Driver>> GetActiveDrivers(VehicleType VehicleType);

        /// <summary>
        /// Tries to reserve an active driver for a trip.
        /// </summary>
        /// <param name="VehicleType">The vehicle type required.</param>
        /// <returns>The reserved driver if available, null otherwise.</returns>
        Task<Driver?> TryReserveDriver(VehicleType VehicleType);

        /// <summary>
        /// Updates the location of a driver without overwriting other fields.
        /// </summary>
        /// <param name="driverId">The driver ID.</param>
        /// <param name="location">The new location.</param>
        Task UpdateDriverLocation(Guid driverId, GeoLocation location);
    }
}
