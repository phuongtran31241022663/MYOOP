﻿using OOP.Domain.Enums;
using OOP.Domain.Events;
using System.Runtime.Serialization;

namespace OOP.Domain.Entities
{
    [DataContract]
    public class Driver : User
    {
        #region Properties
        // Trạng thái tài khoản - chỉ có Passenger và Driver có IsActive
        [DataMember] public bool IsActive { get; private set; } = true;

        [DataMember] public DriverStatus Status { get; private set; }

        private GeoLocation _position = null!;
        [DataMember]
        public GeoLocation Position
        {
            get => _position;
            private set => _position = value ?? throw new ArgumentNullException("Vị trí không được null.");
        }

        private string _licenseNumber = string.Empty;
        [DataMember]
        public string LicenseNumber
        {
            get => _licenseNumber;
            private set => _licenseNumber = string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException("Số giấy phép không hợp lệ.")
                : value.Trim();
        }

        private Vehicle _vehicle = null!;
        [DataMember]
        public Vehicle Vehicle
        {
            get => _vehicle;
            private set => _vehicle = value ?? throw new ArgumentNullException("Xe không được null.");
        }

        // Tài chính
        private decimal _wallet;
        [DataMember]
        public decimal Wallet
        {
            get => _wallet;
            private set => _wallet = value < 0
                ? throw new ArgumentException("Ví không thể âm.")
                : value;
        }

        [DataMember] public DateTime? WalletUpdatedAt { get; private set; }

        private decimal _income;
        [DataMember]
        public decimal Income
        {
            get => _income;
            private set => _income = value < 0
                ? throw new ArgumentException("Thu nhập không thể âm.")
                : value;
        }

        [DataMember] public int TotalTrips { get; private set; }

        // Đánh giá
        [DataMember] private int ratingCount;
        [DataMember] private decimal ratingTotal;

        [IgnoreDataMember]
        public decimal AverageRating =>
            ratingCount == 0 ? 5.0m : Math.Round(ratingTotal / ratingCount, 2);

        // Domain Events (transient - never persisted)
        private List<DomainEvent>? _domainEvents;
        private List<DomainEvent> DomainEventsInternal => _domainEvents ??= new List<DomainEvent>();
        public IReadOnlyList<DomainEvent> DomainEvents => DomainEventsInternal.AsReadOnly();
        #endregion

        #region Valid Status Transitions
        /// <summary>
        /// Dictionary chỉ cho phép chuyển trạng thái hợp lệ cho Driver
        /// Driver không thể tự set Offline - đó là trạng thái hệ thống (tắt app)
        /// </summary>
        private static readonly Dictionary<DriverStatus, HashSet<DriverStatus>> ValidTransitions = new()
        {
            // Offline: chỉ có thể chuyển sang Available (khi mở app)
            { DriverStatus.Offline, new HashSet<DriverStatus> { DriverStatus.Available } },

            // Available: có thể chuyển sang Busy hoặc Offline
            { DriverStatus.Available, new HashSet<DriverStatus> { DriverStatus.Busy, DriverStatus.Offline } },

            // Busy: chỉ có thể chuyển sang Available (khi hoàn thành chuyến)
            { DriverStatus.Busy, new HashSet<DriverStatus> { DriverStatus.Available } }
        };

        private static bool CanTransition(DriverStatus from, DriverStatus to)
        {
            return ValidTransitions.TryGetValue(from, out var validTargets) && validTargets.Contains(to);
        }
        #endregion

        #region Constructors
        protected Driver() { _position = null!; _domainEvents = new List<DomainEvent>(); }

        public Driver(
            Guid id,
            string name,
            string phone,
            string password,
            bool isActive,
            Vehicle vehicle,
            GeoLocation position,
            string licenseNumber)
            : base(id, name, phone, password)
        {
            // Properties will validate automatically via their setters
            Position = position;
            LicenseNumber = licenseNumber;
            Vehicle = CloneVehicleWithDriver(vehicle ?? throw new ArgumentNullException(nameof(vehicle)), Id);
            Status = DriverStatus.Offline; // Mặc định là offline khi tạo mới
            IsActive = isActive;
            Wallet = 0;
            Income = 0;
            TotalTrips = 0;
        }
        #endregion

        private static Vehicle CloneVehicleWithDriver(Vehicle vehicle, Guid driverId)
        {
            return vehicle switch
            {
                Motorbike m => new Motorbike(
                    driverId,
                    m.PlateNumber,
                    m.Brand,
                    m.Model,
                    m.Color),

                Car c => new Car(
                    driverId,
                    c.PlateNumber,
                    c.Brand,
                    c.Model,
                    c.Color,
                    c.Capacity),

                _ => throw new InvalidOperationException("Loại xe không hỗ trợ")
            };
        }

        #region Trạng thái tài khoản
        public void Deactivate(Guid actorId)
        {
            if (Id == actorId)
                throw new InvalidOperationException("Bạn không thể tự khóa tài khoản của chính mình.");

            if (!IsActive)
                throw new InvalidOperationException("Tài khoản đã bị khóa.");

            if (Status == DriverStatus.Busy)
                throw new InvalidOperationException("Không thể khóa tài xế đang chạy.");

            IsActive = false;
        }

        public void Activate()
        {
            if (IsActive)
                throw new InvalidOperationException("Tài khoản đang hoạt động.");

            IsActive = true;
        }
        #endregion

        #region Trạng thái lái xe - Có validation
        /// <summary>
        /// Chuyển sang trạng thái Available (rảnh, nhận chuyến được)
        /// Idempotent: nếu đã Available thì không làm gì cả.
        /// </summary>
        public void SetAvailable()
        {
            if (!IsActive)
                throw new InvalidOperationException("Tài xế đã bị vô hiệu hóa.");

            // Already available — no-op, no throw (idempotent)
            if (Status == DriverStatus.Available)
                return;

            if (Status == DriverStatus.Busy)
                throw new InvalidOperationException("Không thể chuyển sang Sẵn sàng khi đang bận.");

            if (!CanTransition(Status, DriverStatus.Available))
                throw new InvalidOperationException($"Không thể chuyển từ trạng thái '{Status}' sang 'Sẵn sàng'.");

            var oldStatus = Status;
            Status = DriverStatus.Available;
            
            AddDomainEvent(new DriverStatusChangedEvent(Id, oldStatus, Status));
        }

        /// <summary>
        /// Chuyển sang trạng thái Busy (đang chạy chuyến, không nhận chuyến mới)
        /// </summary>
        public void SetBusy()
        {
            if (!IsActive)
                throw new InvalidOperationException("Tài xế đã bị vô hiệu hóa.");

            if (Status != DriverStatus.Available)
                throw new InvalidOperationException("Tài xế phải ở trạng thái Sẵn sàng.");

            if (!CanTransition(Status, DriverStatus.Busy))
                throw new InvalidOperationException($"Không thể chuyển từ trạng thái '{Status}' sang 'Bận'.");

            var oldStatus = Status;
            Status = DriverStatus.Busy;
            
            AddDomainEvent(new DriverStatusChangedEvent(Id, oldStatus, Status));
        }

        /// <summary>
        /// Đánh dấu tài xế ngoại tuyến (tắt app)
        /// Chỉ có hệ thống mới được gọi phương thức này - tài xế không thể tự set offline
        /// </summary>
        public void MarkAsOffline()
        {
            if (Status == DriverStatus.Busy)
                throw new InvalidOperationException("Không thể ngoại tuyến khi đang trong chuyến đi.");

            if (Status == DriverStatus.Offline)
                throw new InvalidOperationException("Tài xế đã ở trạng thái Ngoại tuyến.");

            if (!CanTransition(Status, DriverStatus.Offline))
                throw new InvalidOperationException($"Không thể chuyển từ trạng thái '{Status}' sang 'Ngoại tuyến'.");

            var oldStatus = Status;
            Status = DriverStatus.Offline;
            
            AddDomainEvent(new DriverStatusChangedEvent(Id, oldStatus, Status));
        }

        #endregion

        #region Cập nhật thông tin
        public void UpdateVehicle(Vehicle newVehicle)
        {
            if (newVehicle == null)
                throw new ArgumentNullException(nameof(newVehicle));

            Vehicle = CloneVehicleWithDriver(newVehicle, Id);
        }
        public void UpdateLicenseNumber(string newLicense)
        {
            if (string.IsNullOrWhiteSpace(newLicense))
                throw new ArgumentException("Số giấy phép không hợp lệ.");

            LicenseNumber = newLicense.Trim();
        }
        #endregion

        // ── Vị trí ───────────────────────────────────────────────────────────
        public void UpdateLocation(GeoLocation location)
        {
            Position = location ?? throw new ArgumentNullException(nameof(location));
            
            AddDomainEvent(new DriverLocationUpdatedEvent(Id, location));
        }

        /// <summary>
        /// Syncs state from another Driver object (e.g., after refresh from database).
        /// Used to update local driver state after operations like completing a trip.
        /// </summary>
        public void SyncFrom(Driver other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            Status = other.Status;
            IsActive = other.IsActive;
            Wallet = other.Wallet;
            TotalTrips = other.TotalTrips;
            ratingTotal = other.ratingTotal;
            ratingCount = other.ratingCount;
            _position = other._position;
        }

        // ── Chuyến đi ───────────────────────────────────────────────────────
        public void AddTrip()
        {
            if (Status != DriverStatus.Busy)
                throw new InvalidOperationException("Chỉ được cộng chuyến khi đang chạy.");

            TotalTrips++;
        }
        public void UpdateRating(int newScore, int? oldScore = null)
        {
            if (newScore < 1 || newScore > 5) throw new ArgumentException("Sao từ 1-5.");

            if (oldScore.HasValue)
            {
                // Validate that oldScore was in valid range
                if (oldScore.Value < 1 || oldScore.Value > 5)
                    throw new ArgumentException("Sao cũ không hợp lệ.");

                // Trường hợp cập nhật đánh giá cũ
                ratingTotal = ratingTotal - oldScore.Value + newScore;
            }
            else
            {
                // Trường hợp đánh giá mới hoàn toàn
                ratingCount++;
                ratingTotal += newScore;
            }
        }
        // ── Tài chính ───────────────────────────────────────────────────────
        public void TopUpWallet(decimal amount)
        {
            if (amount <= 0)
                throw new ArgumentException("Số tiền nạp phải lớn hơn 0.", nameof(amount));

            Wallet += amount;
            WalletUpdatedAt = DateTime.UtcNow;
        }

        public void PayCommission(decimal fare, decimal commissionRate)
        {
            if (fare < 0)
                throw new ArgumentException("Cước phí không thể âm.", nameof(fare));

            if (commissionRate < 0 || commissionRate > 1)
                throw new ArgumentException("Tỷ lệ hoa hồng phải từ 0 đến 1.", nameof(commissionRate));

            var commission = Math.Round(fare * commissionRate, 2);

            if (Wallet < commission)
                throw new InvalidOperationException("Số dư ví không đủ để trả hoa hồng.");

            Wallet -= commission;
            Income += fare - commission;
            WalletUpdatedAt = DateTime.UtcNow;
        }
        public override string GetInfo()
        {
            string tinhTrang = Status switch
            {
                DriverStatus.Available => "Sẵn sàng",
                DriverStatus.Busy => "Đang bận",
                DriverStatus.Offline => "Ngoại tuyến",
                _ => "Không xác định"
            };

            return $"{base.GetInfo()}\nTrạng thái: {tinhTrang}" +
                   $"\nXe: {Vehicle.Model} (Biển số: {Vehicle.PlateNumber})" +
                   $"\nĐánh giá: {AverageRating}⭐ | Tổng chuyến: {TotalTrips} | Ví: {Wallet:N0} VNĐ | Thu nhập: {Income:N0} VNĐ";
        }

        // ── Domain Events ─────────────────────────────────────────────────────
        private void AddDomainEvent(DomainEvent domainEvent)
        {
            DomainEventsInternal.Add(domainEvent);
        }

        public void ClearDomainEvents()
        {
            DomainEventsInternal.Clear();
        }
    }
}
