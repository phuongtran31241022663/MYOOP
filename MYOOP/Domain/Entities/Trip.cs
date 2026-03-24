using OOP.Domain.Enums;
using OOP.Domain.Events;
using OOP.Domain.Entities;
using System.Runtime.Serialization;

namespace OOP.Domain.Entities
{
    // Trip là Aggregate Root — tất cả thay đổi trạng thái đi qua đây
    [DataContract]
    public class Trip
    {
        #region Properties
        // ── 1. Định danh ──────────────────────────────────────────────────────
        [DataMember] public Guid Id { get; init; }

        private Guid _passengerId;
        [DataMember]
        public Guid PassengerId
        {
            get => _passengerId;
            init => _passengerId = value == Guid.Empty
                ? throw new ArgumentException("Mã hành khách không hợp lệ.")
                : value;
        }

        [DataMember] public Guid? DriverId { get; private set; }
        [DataMember] public Guid FareRuleId { get; init; }

        // ── 2. Lộ trình ───────────────────────────────────────────────────────
        private GeoLocation _pickupLocation = null!;
        [DataMember]
        public GeoLocation PickupLocation
        {
            get => _pickupLocation;
            init => _pickupLocation = value ?? throw new ArgumentException("Vị trí đón không được để trống.");
        }

        private GeoLocation _destinationLocation = null!;
        [DataMember]
        public GeoLocation DestinationLocation
        {
            get => _destinationLocation;
            init
            {
                if (value == null)
                    throw new ArgumentException("Vị trí đến không được để trống.");
                if (GeoLocation.IsSameLocation(_pickupLocation, value))
                    throw new ArgumentException("Điểm đón và điểm đến không được trùng nhau.");
                _destinationLocation = value;
            }
        }

        [DataMember] public VehicleType VehicleType { get; init; }

        // ── 3. Lộ trình chi tiết (waypoints) ───────────────────────
        [DataMember] public Route? Route { get; private set; }

        // ── 4. Kết quả chuyến ───────────────────────
        private double _distance;
        [DataMember]
        public double Distance
        {
            get => _distance;
            private set => _distance = value <= 0
                ? throw new ArgumentException("Khoảng cách không hợp lệ.")
                : value;
        }

        [DataMember] public double Duration { get; private set; }  // phút
        [DataMember] public decimal Fare { get; private set; }

        // ── 4. Trạng thái ─────────────────────────────────────────────────────
        [DataMember] public TripStatus Status { get; private set; }
        [DataMember] public string? CancelReason { get; private set; }

        // ── 5. Thời gian ─────────────────────────────────────────────────────
        [DataMember] public DateTime RequestedAt { get; init; }
        [DataMember] public DateTime? MatchedAt { get; private set; }
        [DataMember] public DateTime? ArrivedAt { get; private set; }
        [DataMember] public DateTime? StartedAt { get; private set; }
        [DataMember] public DateTime? CompletedAt { get; private set; }
        [DataMember] public DateTime? CancelledAt { get; private set; }
        [DataMember] public DateTime? TimedOutAt { get; private set; }

        [DataMember] public bool IsRated { get; private set; } = false;
        
        // ── 5. Rejected Drivers (serializable) ─────────────────────────────────
        [DataMember] private List<Guid>? _rejectedDriverIds;
        private List<Guid> RejectedDriverIdsInternal => _rejectedDriverIds ??= new List<Guid>();
        public IReadOnlyList<Guid> RejectedDriverIds => RejectedDriverIdsInternal.AsReadOnly();

        // ── 6. Domain Events (transient - never persisted) ──────────────────────
        private List<DomainEvent>? _domainEvents;
        private List<DomainEvent> DomainEventsInternal => _domainEvents ??= new List<DomainEvent>();
        public IReadOnlyList<DomainEvent> DomainEvents => DomainEventsInternal.AsReadOnly();
        #endregion
        #region Constructors
        protected Trip() { _domainEvents = new List<DomainEvent>(); }

        public Trip(Guid passengerId, Guid fareRuleId,
      GeoLocation pickup, GeoLocation destination,
      VehicleType vehicleType, double routeDistance)
        {
            Id = Guid.NewGuid();
            PassengerId = passengerId;
            FareRuleId = fareRuleId;
            // Properties will validate automatically via their setters/initers
            PickupLocation = pickup;
            DestinationLocation = destination;
            VehicleType = vehicleType;
            Distance = routeDistance;

            Status = TripStatus.Requested;
            RequestedAt = DateTime.UtcNow;
            
            // Raise TripRequestedEvent
            AddDomainEvent(new TripRequestedEvent(
                Id,
                PassengerId,
                PickupLocation,
                DestinationLocation,
                VehicleType,
                Distance,
                Fare));
        }
        #endregion
        #region State

        // Requested → Searching
        public void MarkSearching()
        {
            if (Status != TripStatus.Requested)
                throw new InvalidOperationException(
                    "Chỉ có thể chuyển sang tìm tài xế khi chuyến đi đang ở trạng thái Yêu cầu.");

            Status = TripStatus.Searching;
            AddDomainEvent(new TripSearchingEvent(Id));
        }
        // Requested → Matched
        public void AssignDriver(Driver driver)
        {
            if (driver == null)
                throw new ArgumentNullException(nameof(driver));

            if (Status != TripStatus.Requested && Status != TripStatus.Searching)
                throw new InvalidOperationException(
                    $"Không thể gán tài xế khi trip đang ở trạng thái '{Status}'.");

            if (!driver.IsActive)
                throw new InvalidOperationException("Tài xế đã bị vô hiệu hóa.");

            if (driver.Status != DriverStatus.Available)
                throw new InvalidOperationException(
                    $"Tài xế hiện không sẵn sàng nhận chuyến (trạng thái: '{driver.Status}').");

            if (driver.Vehicle.Type != VehicleType)
                throw new InvalidOperationException("Loại xe không phù hợp với yêu cầu.");

            if (driver.Id == PassengerId)
                throw new InvalidOperationException("Tài xế không thể tự đặt chuyến cho mình.");

            DriverId = driver.Id;
            Status = TripStatus.Matched;
            MatchedAt = DateTime.UtcNow;
            
            AddDomainEvent(new TripMatchedEvent(
                Id,
                driver.Id,
                driver.Name,
                driver.Phone,
                $"{driver.Vehicle.Type} - {driver.Vehicle.PlateNumber}"));
        }

        // Matched → Arrived
        public void MarkArrived()
        {
            if (Status != TripStatus.Matched)
                throw new InvalidOperationException(
                    "Tài xế phải ở trạng thái Đã ghép trước khi Đã đến nơi.");

            Status = TripStatus.Arrived;
            ArrivedAt = DateTime.UtcNow;
            
            AddDomainEvent(new TripArrivedEvent(Id));
        }

        // Arrived → Started
        public void StartTrip()
        {
            if (Status != TripStatus.Arrived)
                throw new InvalidOperationException(
                    $"Không thể bắt đầu chuyến khi trạng thái là '{Status}'. Tài xế phải đến nơi đón trước.");

            if (DriverId == null)
                throw new InvalidOperationException("Chuyến chưa được gán tài xế.");

            Status = TripStatus.Started;
            StartedAt = DateTime.UtcNow;
            
            AddDomainEvent(new TripStartedEvent(Id));
        }

        // Started → Completed
        /// <summary>
        /// Hoàn thành chuyến đi với kết quả GPS thực tế.
        /// Lưu ý: Distance ghi đè giá trị từ constructor (route distance) bằng khoảng cách GPS thực tế.
        /// </summary>
        public void CompleteTrip(double distance, double duration, decimal fare)
        {
            if (Status != TripStatus.Started)
                throw new InvalidOperationException(
                    $"Không thể hoàn thành chuyến khi trạng thái là '{Status}'. Chuyến phải đang chạy.");

            if (distance <= 0 || duration <= 0 || fare < 0)
                throw new ArgumentException("Kết quả chuyến đi (quãng đường/thời gian/giá) không hợp lệ.");

            // Ghi đè khoảng cách từ constructor (ước tính) bằng khoảng cách GPS thực tế
            Distance = distance;
            Duration = duration;
            Fare = fare;

            Status = TripStatus.Completed;
            CompletedAt = DateTime.UtcNow;
            
            AddDomainEvent(new TripCompletedEvent(Id, distance, duration, fare, DriverId!.Value));
        }

        // Bất kỳ trạng thái nào (trừ Completed/Cancelled) → Cancelled
        public void CancelTrip(string reason)
        {
            if (Status == TripStatus.Completed)
                throw new InvalidOperationException("Không thể hủy chuyến đã hoàn thành.");

            if (Status == TripStatus.Cancelled)
                throw new InvalidOperationException("Chuyến đã được hủy trước đó.");

            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Cần cung cấp lý do hủy chuyến.");

            Status = TripStatus.Cancelled;
            CancelReason = reason;
            CancelledAt = DateTime.UtcNow;
            
            AddDomainEvent(new TripCancelledEvent(Id, reason));
        }

        // Searching/Requested → Timeout
        public void TimeoutTrip()
        {
            if (Status != TripStatus.Searching && Status != TripStatus.Requested)
                throw new InvalidOperationException(
                    "Chỉ có thể timeout khi trip đang ở trạng thái Requested/Searching.");

            Status = TripStatus.Timeout;
            TimedOutAt = DateTime.UtcNow;
            
            AddDomainEvent(new TripTimeoutEvent(Id));
        }
        #endregion

        // ── Ghi nhận kết quả trước khi hoàn thành ────────────────────────────

        public void ApplyFare(decimal fare)
        {
            EnsureNotFinished(nameof(ApplyFare));

            if (fare < 0)
                throw new ArgumentException("Cước phí không hợp lệ.", nameof(fare));

            Fare = fare;
        }

        public void ApplyDistance(double distance)
        {
            EnsureNotFinished(nameof(ApplyDistance));

            if (distance <= 0)
                throw new ArgumentException("Khoảng cách phải lớn hơn 0.", nameof(distance));

            Distance = distance;
        }

        public void ApplyDuration(double duration)
        {
            EnsureNotFinished(nameof(ApplyDuration));

            if (duration <= 0)
                throw new ArgumentException("Thời gian phải lớn hơn 0.", nameof(duration));

            Duration = duration;
        }

        private void EnsureNotFinished(string callerName)
        {
            if (Status == TripStatus.Completed
                || Status == TripStatus.Cancelled
                || Status == TripStatus.Timeout)
                throw new InvalidOperationException(
                    $"{callerName} không thể gọi trên chuyến đi đã kết thúc (Status: {Status}).");
        }

        public void MarkAsRated()
        {
            if (Status != TripStatus.Completed)
                throw new InvalidOperationException(
                    "Chỉ chuyến đi hoàn thành mới được đánh giá.");

            if (IsRated)
                throw new InvalidOperationException("Chuyến đi này đã được đánh giá rồi.");

            IsRated = true;
        }

        public void AddRejectedDriver(Guid driverId)
        {
            if (driverId == Guid.Empty)
                throw new ArgumentException("DriverId không hợp lệ.", nameof(driverId));

            if (!RejectedDriverIdsInternal.Contains(driverId))
                RejectedDriverIdsInternal.Add(driverId);
        }

        // ── Domain Events ─────────────────────────────────────────────────────
        private void AddDomainEvent(DomainEvent domainEvent)
        {
            DomainEventsInternal.Add(domainEvent);
        }

        public void ClearDomainEvents()
        {
            _domainEvents?.Clear();
        }

        public override string ToString() =>
            $"Trip {Id.ToString()[..8]} | {Status} | " +
            $"{PickupLocation.Name} → {DestinationLocation.Name} | {Fare:N0} VNĐ";
    }
}
