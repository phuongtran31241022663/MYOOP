using OOP.Domain.Entities;
using OOP.Domain.Enums;

namespace OOP.Domain.Policies
{
    /// <summary>
    /// Domain policy cho trip matching — business rules lọc chuyến cho tài xế.
    /// Pure static, không có side effects.
    /// </summary>
    public static class TripMatchingPolicy
    {
        /// <summary>
        /// Lọc các chuyến mà tài xế có thể nhận.
        /// Rules: trip đang Requested/Searching + đúng loại xe + chưa bị reject bởi driver này.
        /// </summary>
        public static IEnumerable<Trip> FilterActiveTripsForDriver(
            IEnumerable<Trip> trips,
            Driver driver)
        {
            if (trips == null) throw new ArgumentNullException(nameof(trips));
            if (driver == null) throw new ArgumentNullException(nameof(driver));

            if (driver.Vehicle == null)
                return Enumerable.Empty<Trip>();

            return trips
                .Where(t => IsTripActiveForDriver(t, driver))
                .OrderBy(t => t.RequestedAt); // FIFO
        }

        /// <summary>
        /// Kiểm tra chuyến có sẵn sàng cho tài xế cụ thể không.
        /// </summary>
        public static bool IsTripActiveForDriver(Trip trip, Driver driver)
        {
            if (trip == null) throw new ArgumentNullException(nameof(trip));
            if (driver == null) throw new ArgumentNullException(nameof(driver));

            // Trip must be in Requested/Searching status
            if (trip.Status is not (TripStatus.Requested or TripStatus.Searching))
                return false;

            // Trip must not be assigned to any driver
            if (trip.DriverId.HasValue)
                return false;

            // Driver must have correct vehicle type
            if (driver.Vehicle == null)
                return false;
            if (!string.Equals(driver.Vehicle.GetVehicleType(), trip.VehicleType, StringComparison.OrdinalIgnoreCase))
                return false;

            // Driver must not have rejected this trip before
            if (trip.RejectedDriverIds.Contains(driver.Id))
                return false;

            // Trip must not be too old (within last 3 minutes)
            if (trip.RequestedAt < DateTime.UtcNow.AddMinutes(-3))
                return false;

            return true;
        }

        /// <summary>
        /// Kiểm tra tài xế có thể nhận chuyến này không.
        /// Trả về (canAccept, reasonIfNot).
        /// </summary>
        public static (bool CanAccept, string? Reason) CanDriverAcceptTrip(Driver driver, Trip trip)
        {
            if (driver == null) throw new ArgumentNullException(nameof(driver));
            if (trip == null) throw new ArgumentNullException(nameof(trip));

            if (!driver.IsActive)
                return (false, "Tài xế đã bị vô hiệu hóa.");

            if (driver.Status != DriverStatus.Active)
            {
                string reason = driver.Status switch
                {
                    DriverStatus.OnTrip => "Tài xế hiện đang có chuyến, không thể nhận thêm.",
                    DriverStatus.Inactive => "Tài xế đang ngoại tuyến. Vui lòng bấm Online trước.",
                    _ => $"Tài xế không ở trạng thái sẵn sàng (trạng thái: '{driver.Status}')."
                };
                return (false, reason);
            }

            if (driver.Vehicle == null)
                return (false, "Tài xế chưa có xe.");

            if (!string.Equals(driver.Vehicle.GetVehicleType(), trip.VehicleType, StringComparison.OrdinalIgnoreCase))
                return (false,
                    $"Loại xe không phù hợp. Yêu cầu: {trip.VehicleType}, " +
                    $"xe của bạn: {driver.Vehicle.GetVehicleType()}.");

            if (trip.RejectedDriverIds.Contains(driver.Id))
                return (false, "Bạn đã từ chối chuyến đi này trước đó.");

            if (trip.Status is not (TripStatus.Requested or TripStatus.Searching))
                return (false,
                    $"Chuyến đi không ở trạng thái có thể nhận (trạng thái: '{trip.Status}').");

            return (true, null);
        }
    }
}