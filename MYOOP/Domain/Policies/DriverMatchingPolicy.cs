using OOP.Domain.Entities;
using OOP.Domain.Enums;

namespace OOP.Domain.Policies
{
    /// <summary>
    /// Domain policy cho driver matching — business rules lọc và xếp hạng tài xế.
    /// Pure static, không có side effects, không phụ thuộc infrastructure.
    /// </summary>
    public static class DriverMatchingPolicy
    {
        /// <summary>
        /// Lọc tài xế đủ điều kiện nhận chuyến.
        /// </summary>
        public static IEnumerable<Driver> FilterEligibleCandidates(
            IEnumerable<Driver> drivers,
            string VehicleType,
            IEnumerable<Guid>? excludedDriverIds = null)
        {
            if (drivers == null) throw new ArgumentNullException(nameof(drivers));
            if (string.IsNullOrWhiteSpace(VehicleType))
                throw new ArgumentException("VehicleType không được để trống.", nameof(VehicleType));

            var excluded = excludedDriverIds != null
                ? new HashSet<Guid>(excludedDriverIds)
                : new HashSet<Guid>();

            return drivers.Where(d => IsEligible(d, VehicleType) && !excluded.Contains(d.Id));
        }

        /// <summary>
        /// Kiểm tra tài xế có đủ điều kiện nhận chuyến không.
        /// </summary>
        public static bool IsEligible(Driver driver, string VehicleType)
        {
            if (driver == null) throw new ArgumentNullException(nameof(driver));

            return driver.IsActive
                && driver.Status == OOP.Domain.Enums.DriverStatus.Active
                && driver.Vehicle != null
                && string.Equals(driver.Vehicle.GetVehicleType(), VehicleType, StringComparison.OrdinalIgnoreCase)
                && driver.Position != null;
        }

        // Kept for backwards compatibility — alias of IsEligible
        public static bool IsEligibleForTrip(Driver driver, string VehicleType)
            => IsEligible(driver, VehicleType);

        /// <summary>
        /// Xếp hạng tài xế theo khoảng cách tới điểm đón (gần nhất trước).
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
            string VehicleType,
            IEnumerable<Guid>? excludedDriverIds,
            Func<Driver, GeoLocation, double> distanceCalculator)
        {
            var eligible = FilterEligibleCandidates(drivers, VehicleType, excludedDriverIds);
            return RankByProximity(eligible, Pickup, distanceCalculator).FirstOrDefault();
        }
    }
}