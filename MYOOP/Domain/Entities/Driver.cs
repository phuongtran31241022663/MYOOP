﻿using OOP.Domain.Enums;
using OOP.Domain.Events;
using System.Runtime.Serialization;

namespace OOP.Domain.Entities
{
    [DataContract]
    public class Driver : User
    {
        #region Properties
        [DataMember] public bool IsActive { get; private set; } = true;

        [DataMember] public DriverStatus Status { get; private set; } = DriverStatus.Active;

        private GeoLocation position = null!;
        [DataMember]
        public GeoLocation Position
        {
            get => position;
            private set => position = value ?? throw new ArgumentNullException("Vị trí không được null.");
        }

        private string licenseNumber = string.Empty;
        [DataMember]
        public string LicenseNumber
        {
            get => licenseNumber;
            private set => licenseNumber = string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException("Số giấy phép không hợp lệ.")
                : value.Trim();
        }

        private Vehicle vehicle = null!;
        [DataMember]
        public Vehicle Vehicle
        {
            get => vehicle;
            private set => vehicle = value ?? throw new ArgumentNullException("Xe không được null.");
        }

        // Tài chính
        private decimal wallet;
        [DataMember]
        public decimal Wallet
        {
            get => wallet;
            private set => wallet = value < 0
                ? throw new ArgumentException("Ví không thể âm.")
                : value;
        }

        [DataMember] public DateTime? WalletUpdatedAt { get; private set; }

        private decimal income;
        [DataMember]
        public decimal Income
        {
            get => income;
            private set => income = value < 0
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

        private List<DomainEvent>? domainEvents;
        private List<DomainEvent> DomainEventsInternal => domainEvents ??= new List<DomainEvent>();
        public IReadOnlyList<DomainEvent> DomainEvents => DomainEventsInternal.AsReadOnly();
        #endregion

        #region Valid Status Transitions
        private static readonly Dictionary<DriverStatus, HashSet<DriverStatus>> ValidTransitions = new()
        {
            // Inactive: chỉ có thể chuyển sang Active
            { DriverStatus.Inactive, new HashSet<DriverStatus> { DriverStatus.Active } },

            // Active: có thể chuyển sang OnTrip hoặc Inactive
            { DriverStatus.Active, new HashSet<DriverStatus> { DriverStatus.OnTrip, DriverStatus.Inactive } },

            // OnTrip: chỉ có thể chuyển sang Active
            { DriverStatus.OnTrip, new HashSet<DriverStatus> { DriverStatus.Active } }
        };

        private static bool CanTransition(DriverStatus from, DriverStatus to)
        {
            return ValidTransitions.TryGetValue(from, out var validTargets) && validTargets.Contains(to);
        }
        #endregion

        #region Constructors
        protected Driver() { position = null!; domainEvents = new List<DomainEvent>(); }

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
            Status = DriverStatus.Inactive;
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

                 _=> throw new InvalidOperationException("Loại xe không hỗ trợ")
            };
        }

        #region Trạng thái tài khoản
        public void Deactivate(Guid actorId)
        {
            if (Id == actorId)
                throw new InvalidOperationException("Bạn không thể tự khóa tài khoản của chính mình.");

            if (!IsActive)
                throw new InvalidOperationException("Tài khoản đã bị khóa.");

            if (Status == DriverStatus.OnTrip)
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
        public void SetActive()
        {
            if (!IsActive)
                throw new InvalidOperationException("Tài xế đã bị vô hiệu hóa.");

            if (Status == DriverStatus.Active)
                return;

            if (Status == DriverStatus.OnTrip)
                throw new InvalidOperationException("Không thể chuyển sang Sẵn sàng khi đang bận.");

            if (!CanTransition(Status, DriverStatus.Active))
                throw new InvalidOperationException($"Không thể chuyển từ trạng thái '{Status}' sang 'Sẵn sàng'.");

            var oldStatus = Status;
            Status = DriverStatus.Active;
            
            AddDomainEvent(new DriverStatusChangedEvent(Id, oldStatus, Status));
        }

        public void SetOnTrip()
        {
            if (!IsActive)
                throw new InvalidOperationException("Tài xế đã bị vô hiệu hóa.");

            if (Status != DriverStatus.Active)
                throw new InvalidOperationException("Tài xế phải ở trạng thái Sẵn sàng.");

            if (!CanTransition(Status, DriverStatus.OnTrip))
                throw new InvalidOperationException($"Không thể chuyển từ trạng thái '{Status}' sang 'Bận'.");

            var oldStatus = Status;
            Status = DriverStatus.OnTrip;
            
            AddDomainEvent(new DriverStatusChangedEvent(Id, oldStatus, Status));
        }

        public void ForceSetActive()
        {
            if (!IsActive)
                throw new InvalidOperationException("Tài xế đã bị vô hiệu hóa.");

            if (!CanTransition(Status, DriverStatus.Active))
                throw new InvalidOperationException($"Không thể chuyển từ trạng thái '{Status}' sang 'Sẵn sàng'.");

            var oldStatus = Status;
            Status = DriverStatus.Active;
            AddDomainEvent(new DriverStatusChangedEvent(Id, oldStatus, Status));
        }

        public void SetInactive()
        {
            if (!IsActive)
                throw new InvalidOperationException("Tài xế đã bị vô hiệu hóa.");

            // Idempotent: nếu đã Inactive thì không cần làm gì
            if (Status == DriverStatus.Inactive)
                return;

            if (Status == DriverStatus.OnTrip)
                throw new InvalidOperationException("Không thể ngắt kết nối khi đang chạy chuyến.");

            if (!CanTransition(Status, DriverStatus.Inactive))
                throw new InvalidOperationException($"Không thể chuyển từ trạng thái '{Status}' sang 'Inactive'.");

            var oldStatus = Status;
            Status = DriverStatus.Inactive;

            AddDomainEvent(new DriverStatusChangedEvent(Id, oldStatus, Status));
        }

        public void MarkAsInactive()
        {
            SetInactive();
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
            position = other.position;
        }

        // ── Chuyến đi ───────────────────────────────────────────────────────
        public void AddTrip()
        {
            if (Status != DriverStatus.OnTrip)
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
                DriverStatus.Active => "Sẵn sàng",
                DriverStatus.OnTrip => "Đang bận",
                DriverStatus.Inactive => "Ngoại tuyến",
                 _=> "Không xác định"
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
