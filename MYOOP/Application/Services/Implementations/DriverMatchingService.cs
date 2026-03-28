using OOP.Domain.Entities;
using OOP.Domain.Enums;
using OOP.Domain.Interfaces;
using OOP.Domain.Policies;
using OOP.Application.Services.Interfaces;
using System.Diagnostics;

namespace OOP.Application.Services
{
    public class DriverMatchingService : IDriverMatchingService
    {
        private readonly IDriverRepository _driverRepo;
        private readonly IUserRepository _userRepo;
        private readonly ITripRepository _tripRepo;
        private readonly IRouteService _routeService;

        public DriverMatchingService(
            IDriverRepository driverRepo,
            IUserRepository userRepo,
            ITripRepository tripRepo,
            IRouteService routeService)
        {
            _driverRepo = driverRepo ?? throw new ArgumentNullException(nameof(driverRepo));
            _userRepo = userRepo ?? throw new ArgumentNullException(nameof(userRepo));
            _tripRepo = tripRepo ?? throw new ArgumentNullException(nameof(tripRepo));
            _routeService = routeService ?? throw new ArgumentNullException(nameof(routeService));
        }

        // Tìm kiếm(chỉ đọc, không thực hiện giữ chỗ/đặt trước).

        public async Task<Driver?> FindActiveDriver(
            GeoLocation pickup,
            VehicleType VehicleType,
            IEnumerable<Guid>? excludedDriverIds = null)
        {
            if (pickup == null) throw new ArgumentNullException(nameof(pickup));

            var excluded = excludedDriverIds != null
                ? new HashSet<Guid>(excludedDriverIds)
                : new HashSet<Guid>();

            await RefreshCacheIfSupported();

            var allUsers = await _driverRepo.GetActiveDrivers(VehicleType);
            var candidates = MatchingPolicies.DriverMatchingPolicy
                .FilterEligibleCandidates(allUsers.OfType<Driver>(), VehicleType, excluded)
                .ToList();

            Debug.WriteLine($"[Tìm tài xế hoạt động] có {candidates.Count} ứng viên cho loại xe {VehicleType}");
            if (!candidates.Any()) return null;

            var tasks = candidates.Select(async d => new
            {
                Driver = d,
                Distance = await _routeService.CalculateDistanceAsync(d.Position, pickup)
            });

            var results = await Task.WhenAll(tasks);
            return results.OrderBy(r => r.Distance).FirstOrDefault()?.Driver;
        }

        /// <summary>
        /// Tìm danh sách tài xế khả dụng, loại trừ những tài xế đã bị từ chối.
        /// </summary>
        public async Task<List<Driver>> FindAvailableDrivers(
    GeoLocation pickup,
    VehicleType vehicleType,
    IEnumerable<Guid>? excludedDriverIds = null)
        {
            if (pickup == null) throw new ArgumentNullException(nameof(pickup));
            var excluded = excludedDriverIds != null
                ? new HashSet<Guid>(excludedDriverIds)
                : new HashSet<Guid>();

            await RefreshCacheIfSupported();

            var allDrivers = await _driverRepo.GetActiveDrivers(vehicleType);
            var available = allDrivers
        .Where(d => !excluded.Contains(d.Id))
        .ToList();
            Debug.WriteLine($"[Tìm tài xế khả dụng] Tìm thấy {available.Count} tài xế hoạt động.");
    return available;
        }

        // Ghép cặp (sử dụng cho luồng tự động tìm tài xế).

        public async Task<Driver?> MatchDriver(Trip trip)
        {
            if (trip == null) throw new ArgumentNullException(nameof(trip));

            var driver = await FindActiveDriver(
                trip.Pickup, trip.VehicleType, trip.RejectedDriverIds);

            if (driver == null) return null;

            driver.SetOnTrip();
            trip.AssignDriver(driver);
            await _tripRepo.Update(trip);
            await _userRepo.Update(driver);
            return driver;
        }

        // Tài xế lân cận (dùng để hiển thị trên bản đồ của hành khách).

        public async Task<List<Driver>> GetNearbyDrivers(
            GeoLocation pickup, VehicleType VehicleType, double maxKm)
        {
            if (pickup == null) throw new ArgumentNullException(nameof(pickup));
            if (maxKm <= 0) throw new ArgumentException("Bán kính phải lớn hơn 0.", nameof(maxKm));

            var allDrivers = await _driverRepo.GetActiveDrivers(VehicleType);
            var candidates = MatchingPolicies.DriverMatchingPolicy
                .FilterEligibleCandidates(allDrivers.OfType<Driver>(), VehicleType)
                .ToList();

            var tasks = candidates.Select(async d => new
            {
                Driver = d,
                Distance = await _routeService.CalculateDistanceAsync(d.Position, pickup)
            });

            var results = await Task.WhenAll(tasks);
            return results
                .Where(r => r.Distance <= maxKm)
                .OrderBy(r => r.Distance)
                .Select(r => r.Driver)
                .ToList();
        }

        // ── Helper ────────────────────────────────────────────────────────────

        private async Task RefreshCacheIfSupported()
        {
            if (_driverRepo is ICacheRefreshable cr)
                await cr.RefreshCacheAsync();
        }

        /// <summary>
        /// Gửi request tuần tự cho tài xế gần nhất.
        /// Flow: Lấy tất cả tài xế có loại xe phù hợp →
        ///       DriverMatchingPolicy loại bỏ người không đủ điều kiện (busy, locked, rejected) →
        ///       Task.WhenAll tính khoảng cách song song →
        ///       OrderBy khoảng cách →
        ///       Trả về tài xế gần nhất để caller gửi thông báo.
        /// </summary>
        public async Task<Driver?> DispatchToNearestDriver(Trip trip)
        {
            if (trip == null) throw new ArgumentNullException(nameof(trip));

            await RefreshCacheIfSupported();

            // 1. Lấy tất cả tài xế có loại xe phù hợp
            var allDrivers = await _driverRepo.GetActiveDrivers(trip.VehicleType);

            // 2. DriverMatchingPolicy: loại bỏ busy, locked, rejected
            var candidates = MatchingPolicies.DriverMatchingPolicy
                .FilterEligibleCandidates(allDrivers.OfType<Driver>(), trip.VehicleType, trip.RejectedDriverIds)
                .ToList();

            Debug.WriteLine($"[DispatchToNearest] {candidates.Count} ứng viên cho Trip {trip.Id.ToString()[..8]}");

            if (!candidates.Any()) return null;

            // 3. Task.WhenAll tính khoảng cách song song (tăng tốc độ)
            var tasks = candidates.Select(async d => new
            {
                Driver = d,
                Distance = await _routeService.CalculateDistanceAsync(d.Position, trip.Pickup)
            });

            var results = await Task.WhenAll(tasks);

            // 4. Sắp xếp: chọn người ở gần nhất
            // 5. Trả về tài xế đầu tiên (caller sẽ gửi notification lần lượt)
            var nearest = results
                .OrderBy(r => r.Distance)
                .FirstOrDefault();

            if (nearest != null)
            {
                Debug.WriteLine($"[DispatchToNearest] Tài xế gần nhất: {nearest.Driver.Name} ({nearest.Distance:F2} km)");
            }

            return nearest?.Driver;
        }
    }
}
