using OOP.Domain.Entities;
using OOP.Domain.Enums;

namespace OOP.Domain.Policies
{
    public class MatchingPolicies
    {
        /// <summary>
        /// Domain policy cho driver matching — business rules lọc và xếp hạng tài xế.
        /// Pure static, không có side effects, không phụ thuộc infrastructure.
        /// </summary>
        public static class DriverMatchingPolicy
        {
            /// <summary>
            /// Lọc tài xế đủ điều kiện nhận chuyến dựa trên loại xe và danh sách loại trừ.
            /// </summary>
            public static IEnumerable<Driver> FilterEligibleCandidates(
                IEnumerable<Driver> drivers,
                VehicleType VehicleType,
                IEnumerable<Guid>? excludedDriverIds = null)
            {
                if (drivers == null) throw new ArgumentNullException(nameof(drivers));

                var excluded = excludedDriverIds != null
                    ? new HashSet<Guid>(excludedDriverIds)
                    : new HashSet<Guid>();

                return drivers.Where(d => IsEligible(d, VehicleType) && !excluded.Contains(d.Id));
            }

            /// <summary>
            /// Kiểm tra tài xế có đủ điều kiện (Active, đúng loại xe, có vị trí).
            /// </summary>
            public static bool IsEligible(Driver driver, VehicleType VehicleType)
            {
                if (driver == null) throw new ArgumentNullException(nameof(driver));

                return driver.IsActive
                    && driver.Status == OOP.Domain.Enums.DriverStatus.Available
                    && driver.Vehicle != null
                    && driver.Vehicle.GetVehicleType() == VehicleType
                    && driver.Position != null;
            }

            /// <summary>
            /// Xếp hạng tài xế theo khoảng cách tới điểm đón (gần nhất đứng đầu).
            /// </summary>
            public static IEnumerable<T> RankByProximity<T>(
                IEnumerable<T> candidates,
                GeoLocation Pickup,
                Func<T, GeoLocation, double> distanceCalculator)
            {
                if (candidates == null) throw new ArgumentNullException(nameof(candidates));
                if (Pickup == null) throw new ArgumentNullException(nameof(Pickup));
                if (distanceCalculator == null) throw new ArgumentNullException(nameof(distanceCalculator));

                return candidates
                    .Select(c => (Candidate: c, Distance: distanceCalculator(c, Pickup)))
                    .OrderBy(x => x.Distance)
                    .Select(x => x.Candidate);
            }

            /// <summary>
            /// Tìm tài xế phù hợp nhất (đủ điều kiện + gần nhất).
            /// </summary>
            public static Driver? FindBestMatch(
                IEnumerable<Driver> drivers,
                GeoLocation Pickup,
                VehicleType VehicleType,
                IEnumerable<Guid>? excludedDriverIds,
                Func<Driver, GeoLocation, double> distanceCalculator)
            {
                var eligible = FilterEligibleCandidates(drivers, VehicleType, excludedDriverIds);
                return RankByProximity(eligible, Pickup, distanceCalculator).FirstOrDefault();
            }
        }
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

                // 1. Chuyến phải đang trong trạng thái đang tìm
                if (trip.Status is not (TripStatus.Searching))
                    return false;

                // 2. Chuyến chưa được gán cho ai
                if (trip.DriverId.HasValue)
                    return false;

                // 3. Kiểm tra loại xe
                if (driver.Vehicle == null || driver.Vehicle.GetVehicleType() != trip.VehicleType)
                    return false;

                // 4. Tài xế chưa từng từ chối chuyến này
                if (trip.RejectedDriverIds.Contains(driver.Id))
                    return false;

                // 5. Chuyến không được quá cũ (trong vòng 3 phút)
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

                if (driver.Status != DriverStatus.Available)
                {
                    string statusVn = driver.Status == DriverStatus.OnTrip ? "Đang có chuyến" : "Nghỉ";
                    return (false, $"Bạn không ở trạng thái sẵn sàng (Hiện tại: {statusVn}).");
                }

                if (driver.Vehicle == null)
                    return (false, "Tài xế chưa có xe.");

                if (driver.Vehicle.GetVehicleType() != trip.VehicleType)
                    return (false, $"Loại xe không phù hợp. Yêu cầu: {trip.VehicleType}, Xe của bạn: {driver.Vehicle.GetVehicleType()}.");

                if (trip.RejectedDriverIds.Contains(driver.Id))
                    return (false, "Bạn đã từ chối chuyến đi này trước đó.");

                if (trip.Status is not (TripStatus.Searching))
                    return (false, "Chuyến đi này không còn khả dụng hoặc đã có người nhận.");

                return (true, null);
            }
        }
    }
}
