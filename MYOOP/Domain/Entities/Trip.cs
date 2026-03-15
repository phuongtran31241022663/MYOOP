using OOP.Application.Validators;
using OOP.Domain.Enums;
using System.Runtime.Serialization;

namespace OOP.Domain.Entities
{
    // Trip là Aggregate Root — tất cả thay đổi trạng thái đi qua đây
    [DataContract]
    public class Trip
    {
        // --- 1. Định danh ---
        [DataMember]
        public Guid Id { get; init; }
        [DataMember]
        public Guid PassengerId { get; init; }
        [DataMember]
        public Guid? DriverId { get; private set; }
        [DataMember]
        public Guid FareRuleId { get; init; }
        // --- 2. Lộ trình ---
        [DataMember]
        public Location PickupLocation { get; init; }

        [DataMember]
        public Location DestinationLocation { get; init; }
        [DataMember]
        public VehicleType VehicleType { get; init; }
        // --- 3. Kết quả chuyến (gán sau khi hoàn thành) ---
        [DataMember]
        public double Distance { get; private set; }    // km
        [DataMember]
        public double Duration { get; private set; }    // phút
        [DataMember]
        public decimal Fare { get; private set; }
        // --- 4. Trạng thái ---
        [DataMember]
        public TripStatus Status { get; private set; }
        [DataMember]
        public string? CancelReason { get; private set; }
        // --- 5. Thời gian ---
        [DataMember]
        public DateTime RequestedAt { get; init; }
        [DataMember]
        public DateTime? MatchedAt { get; private set; }

        [DataMember]
        public DateTime? ArrivedAt { get; private set; }

        [DataMember]
        public DateTime? StartedAt { get; private set; }

        [DataMember]
        public DateTime? CompletedAt { get; private set; }
        [DataMember]
        public DateTime? CancelledAt { get; private set; }
        [DataMember]
        public bool IsRated { get; private set; } = false;
        protected Trip() { }
        public Trip(
      Guid passengerId,
      Guid fareRuleId,
      Location pickupLocation,
      Location destinationLocation,
      VehicleType vehicleType,
      double routeDistance)
        {
            TripValidator.ValidateRequest(pickupLocation, destinationLocation, vehicleType, routeDistance);

            if (passengerId == Guid.Empty)
                throw new ArgumentException("PassengerId không hợp lệ.");

            if (fareRuleId == Guid.Empty)
                throw new ArgumentException("FareRuleId không hợp lệ.");

            Id = Guid.NewGuid();
            PassengerId = passengerId;
            FareRuleId = fareRuleId;
            PickupLocation = pickupLocation;
            DestinationLocation = destinationLocation;
            VehicleType = vehicleType;
            Distance = routeDistance;
            Status = TripStatus.Requested;
            RequestedAt = DateTime.Now;
        }
        // --- Thay đổi trạng thái ---
        // Requested → Matched
        public void AssignDriver(Guid driverId)
        {
            if (Status != TripStatus.Requested)
                throw new InvalidOperationException("Chỉ có thể assign driver khi trip đang ở trạng thái Requested.");

            if (driverId == Guid.Empty)
                throw new ArgumentException("DriverId không hợp lệ.");

            DriverId = driverId;
            Status = TripStatus.Matched;
            MatchedAt = DateTime.Now;
        }
        // Matched → Arrived
        public void MarkArrived()
        {
            if (Status != TripStatus.Matched)
                throw new InvalidOperationException("Driver phải ở trạng thái Matched trước khi Arrived.");

            Status = TripStatus.Arrived;
            ArrivedAt = DateTime.Now;
        }
        // Arrived → Ongoing
        public void StartTrip()
        {
            TripValidator.ValidateStart(this);

            Status = TripStatus.Ongoing;
            StartedAt = DateTime.Now;
        }
        // Ongoing → Completed
        public void CompleteTrip(double distance, double duration, decimal fare)
        {
            TripValidator.ValidateCompletion(this, distance, fare);

            Distance = distance;
            Duration = duration;
            Fare = fare;
            Status = TripStatus.Completed;
            CompletedAt = DateTime.Now;
        }
        // Bất kỳ trạng thái nào (trừ Completed/Cancelled) → Cancelled
        public void CancelTrip(string reason)
        {
            TripValidator.ValidateCancellation(this);

            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Lý do hủy không được để trống.");

            CancelReason = reason;
            Status = TripStatus.Cancelled;
            CancelledAt = DateTime.Now;
        }
        // --- Ghi nhận kết quả sau chuyến---
        public void ApplyFare(decimal fare)
        {
            if (fare < 0)
                throw new ArgumentException("Cước phí không thể âm.");

            Fare = fare;
        }

        public void ApplyDistance(double distance)
        {
            if (distance <= 0)
                throw new ArgumentException("Khoảng cách phải lớn hơn 0.");

            Distance = distance;
        }

        public void ApplyDuration(double duration)
        {
            if (duration <= 0)
                throw new ArgumentException("Thời gian phải lớn hơn 0.");

            Duration = duration;
        }
        public void MarkAsRated()
        {
            if (Status != TripStatus.Completed)
                throw new InvalidOperationException("Chỉ chuyến đi hoàn thành mới được đánh giá.");

            if (IsRated)
                throw new InvalidOperationException("Chuyến đi này đã được đánh giá rồi.");

            IsRated = true;
        }
        public override string ToString() =>
            $"Trip {Id.ToString()[..8]} | {Status} | {PickupLocation.Label} → {DestinationLocation.Label}" +
            $" | {Fare:N0} VNĐ";
    }
}