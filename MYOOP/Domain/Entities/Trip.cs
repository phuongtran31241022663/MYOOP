using OOP.Domain.Enums;
using OOP.Domain.Events;
using OOP.Domain.Entities;
using System.Runtime.Serialization;

namespace OOP.Domain.Entities
{
    // Trip là Aggregate Root
    [DataContract]
    public class Trip
    {
        #region Properties
        // ── 1. Định danh ──────────────────────────────────────────────────────
        [DataMember] public Guid Id { get; init; }

        private Guid passengerId;
        [DataMember]
        public Guid PassengerId
        {
            get => passengerId;
            init => passengerId = value == Guid.Empty
                ? throw new ArgumentException("Mã hành khách không hợp lệ.")
                : value;
        }

        [DataMember] public Guid? DriverId { get; private set; }
        [DataMember] public Guid FareRuleId { get; init; }

        // ── 2. Lộ trình ───────────────────────────────────────────────────────
        private GeoLocation pickup = null!;
        [DataMember]
        public GeoLocation Pickup
        {
            get => pickup;
            init => pickup = value ?? throw new ArgumentException("Vị trí đón không được để trống.");
        }

        private GeoLocation destination = null!;
        [DataMember]
        public GeoLocation Destination
        {
            get => destination;
            init
            {
                if (value == null)
                    throw new ArgumentException("Vị trí đến không được để trống.");
                if (GeoLocation.IsSameLocation(pickup, value))
                    throw new ArgumentException("Điểm đón và điểm đến không được trùng nhau.");
                destination = value;
            }
        }

        [DataMember] public VehicleType VehicleType { get; init; }

        // ── 3. Lộ trình chi tiết (waypoints) ───────────────────────
        [DataMember] public Route? Route { get; private set; }

        // ── 4. Kết quả chuyến ───────────────────────
        private double distance;
        [DataMember]
        public double Distance
        {
            get => distance;
            private set => distance = value <= 0
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

        [DataMember] public bool IsPaid { get; private set; } = false;
        [DataMember] public bool IsRated { get; private set; } = false;

        // ── 5. Rejected Drivers (serializable) ─────────────────────────────────
        [DataMember] private List<Guid>? rejectedDriverIds;
        private List<Guid> RejectedDriverIdsInternal => rejectedDriverIds ??= new List<Guid>();
        public IReadOnlyList<Guid> RejectedDriverIds => RejectedDriverIdsInternal.AsReadOnly();

        // ── 6. Domain Events (transient - never persisted) ──────────────────────
        private List<DomainEvent>? domainEvents;
        private List<DomainEvent> DomainEventsInternal => domainEvents ??= new List<DomainEvent>();
        public IReadOnlyList<DomainEvent> DomainEvents => DomainEventsInternal.AsReadOnly();
        #endregion
        #region Constructors
        protected Trip() { domainEvents = new List<DomainEvent>(); }

        public Trip(Guid passengerId, Guid fareRuleId,
      GeoLocation pickup, GeoLocation destination,
      VehicleType vehicleType, double routeDistance, decimal fare)
        {
            Id = Guid.NewGuid();
            PassengerId = passengerId;
            FareRuleId = fareRuleId;
            Pickup = pickup;
            Destination = destination;
            VehicleType = vehicleType;
            Distance = routeDistance;
            Fare = fare;
            Status = TripStatus.Requested;
            RequestedAt = DateTime.UtcNow;
            AddEvent(new TripRequestedEvent(
                Id,
                PassengerId,
                Pickup,
                Destination,
                VehicleType,
                Distance,
                Fare));
            Status = TripStatus.Searching;
            AddEvent(new TripSearchingEvent(Id));
        }
        #endregion
        #region State Methods

        // ── Fare ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Thiết lập giá tiền cho chuyến đi.
        /// Giá được tính khi khách hàng đặt chuyến và không thay đổi.
        /// </summary>
        public void SetFare(decimal fare)
        {
            EnsureNotFinished(nameof(SetFare));
            if (fare <= 0)
                throw new ArgumentException("Giá tiền phải lớn hơn 0.");
            Fare = fare;
        }

        // Requested → Searching
        public void MarkSearching()
        {
            EnsureTransition(TripStatus.Searching);

            // Reset assignment when re-dispatching from Matched -> Searching
            DriverId = null;
            MatchedAt = null;
            Status = TripStatus.Searching;
            AddEvent(new TripSearchingEvent(Id));
        }
        // Requested → Searching → Matched
        public void AssignDriver(Driver driver)
        {
            if (driver == null)
                throw new ArgumentNullException(nameof(driver));

            EnsureTransition(TripStatus.Matched);

            if (!driver.IsActive)
                throw new InvalidOperationException("Tài xế đã bị vô hiệu hóa.");
            if (driver.Vehicle == null)
                throw new InvalidOperationException("Tài xế chưa có xe.");
            if (driver.Vehicle.GetVehicleType() != VehicleType)
                throw new InvalidOperationException("Loại xe không phù hợp với yêu cầu.");
            if (driver.Id == PassengerId)
                throw new InvalidOperationException("Tài xế không thể tự đặt chuyến cho mình.");

            DriverId = driver.Id;
            Status = TripStatus.Matched;
            MatchedAt = DateTime.UtcNow;

            AddEvent(new TripMatchedEvent(
                Id,
                driver.Id,
                driver.Name,
                driver.Phone,
                $"{driver.Vehicle.GetVehicleType()} - {driver.Vehicle?.PlateNumber}"));
        }

        // Matched → Arrived
        public void MarkArrived()
        {
            EnsureTransition(TripStatus.Arrived);

            Status = TripStatus.Arrived;
            ArrivedAt = DateTime.UtcNow;

            AddEvent(new TripArrivedEvent(Id));
        }

        // Arrived → Started
        public void StartTrip()
        {
            EnsureTransition(TripStatus.Started);

            if (DriverId == null)
                throw new InvalidOperationException("Chuyến chưa được gán tài xế.");

            Status = TripStatus.Started;
            StartedAt = DateTime.UtcNow;

            AddEvent(new TripStartedEvent(Id));
        }
        // Started → Completed (với IsPaid = true cho thanh toán tiền mặt)
        /// <summary>
        /// Hoàn thành chuyến đi với kết quả GPS thực tế.
        /// Lưu ý: Distance ghi đè giá trị từ constructor (route distance) bằng khoảng cách GPS thực tế.
        /// </summary>
        public void CompleteTrip(double distance, double duration, decimal fare)
        {
            EnsureTransition(TripStatus.Completed);
            if (distance <= 0 || duration <= 0 || fare < 0)
                throw new ArgumentException("Kết quả chuyến đi (quãng đường/thời gian/giá) không hợp lệ.");

            Distance = distance;
            Duration = duration;
            Fare = fare;

            IsPaid = true; // Thanh toán tiền mặt được xác nhận
            Status = TripStatus.Completed;
            CompletedAt = DateTime.UtcNow;

            AddEvent(new TripCompletedEvent(Id, distance, duration, fare, DriverId!.Value));
        }

        public void CancelTrip(string reason)
        {
            EnsureTransition(TripStatus.Cancelled);

            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Cần cung cấp lý do hủy chuyến.");

            Status = TripStatus.Cancelled;
            CancelReason = reason;
            CancelledAt = DateTime.UtcNow;

            AddEvent(new TripCancelledEvent(Id, reason));
        }

        // Searching → Timeout
        public void TimeoutTrip()
        {
            EnsureTransition(TripStatus.Timeout);

            Status = TripStatus.Timeout;
            TimedOutAt = DateTime.UtcNow;

            AddEvent(new TripTimeoutEvent(Id));
        }
        #endregion
        #region Apply Result

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
        #endregion
        #region State Helpers
        private void EnsureTransition(TripStatus target)
        {
            if (!TripStateMachine.CanTransition(Status, target))
                throw new InvalidOperationException(
                    $"Chuyển đổi trạng thái không hợp lệ: {Status} → {target}");
        }
        private void EnsureNotFinished(string action)
        {
            if (Status == TripStatus.Completed
                || Status == TripStatus.Cancelled
                || Status == TripStatus.Timeout)
                throw new InvalidOperationException(
                    $"{action} không thể gọi trên chuyến đi đã kết thúc (Trạng thái: {Status}).");
        }
        #endregion
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
        #region Events

        // ── Domain Events ─────────────────────────────────────────────────────
        private void AddEvent(DomainEvent e)
        {
            Console.WriteLine($"[SỰ KIỆN] {e.GetType().Name} đã kích hoạt cho chuyến xe {Id}");
            DomainEventsInternal.Add(e);
        }

        public void ClearDomainEvents()
        {
            domainEvents?.Clear();
        }
        #endregion
        public override string ToString() =>
            $"Trip {Id.ToString()[..8]} | {Status} | " +
            $"{Pickup.Name} → {Destination.Name} | {Fare:N0} VNĐ";
    }
}
