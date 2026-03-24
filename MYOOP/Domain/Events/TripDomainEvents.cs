using OOP.Domain.Entities;
using OOP.Domain.Enums;

namespace OOP.Domain.Events
{
    // Domain Events - sự kiện xảy ra trong Aggregate
    public abstract class DomainEvent
    {
        public Guid AggregateId { get; }
        public DateTime OccurredAt { get; }
        
        protected DomainEvent(Guid aggregateId)
        {
            AggregateId = aggregateId;
            OccurredAt = DateTime.UtcNow;
        }
    }

    public class TripRequestedEvent : DomainEvent
    {
        public Guid PassengerId { get; }
        public GeoLocation PickupLocation { get; }
        public GeoLocation DestinationLocation { get; }
        public VehicleType VehicleType { get; }
        public double EstimatedDistance { get; }
        public decimal EstimatedFare { get; }

        public TripRequestedEvent(
            Guid tripId,
            Guid passengerId,
            GeoLocation pickup,
            GeoLocation destination,
            VehicleType vehicleType,
            double estimatedDistance,
            decimal estimatedFare) : base(tripId)
        {
            PassengerId = passengerId;
            PickupLocation = pickup;
            DestinationLocation = destination;
            VehicleType = vehicleType;
            EstimatedDistance = estimatedDistance;
            EstimatedFare = estimatedFare;
        }
    }

    public class TripSearchingEvent : DomainEvent
    {
        public TripSearchingEvent(Guid tripId) : base(tripId) { }
    }

    public class TripMatchedEvent : DomainEvent
    {
        public Guid DriverId { get; }
        public string DriverName { get; }
        public string DriverPhone { get; }
        public string VehicleInfo { get; }

        public TripMatchedEvent(
            Guid tripId,
            Guid driverId,
            string driverName,
            string driverPhone,
            string vehicleInfo) : base(tripId)
        {
            DriverId = driverId;
            DriverName = driverName;
            DriverPhone = driverPhone;
            VehicleInfo = vehicleInfo;
        }
    }

    public class TripArrivedEvent : DomainEvent
    {
        public TripArrivedEvent(Guid tripId) : base(tripId) { }
    }

    public class TripStartedEvent : DomainEvent
    {
        public TripStartedEvent(Guid tripId) : base(tripId) { }
    }

    public class TripCompletedEvent : DomainEvent
    {
        public double ActualDistance { get; }
        public double Duration { get; }
        public decimal Fare { get; }
        public Guid DriverId { get; }

        public TripCompletedEvent(
            Guid tripId,
            double actualDistance,
            double duration,
            decimal fare,
            Guid driverId) : base(tripId)
        {
            ActualDistance = actualDistance;
            Duration = duration;
            Fare = fare;
            DriverId = driverId;
        }
    }

    public class TripCancelledEvent : DomainEvent
    {
        public string Reason { get; }
        public TripCancelledEvent(Guid tripId, string reason) : base(tripId)
        {
            Reason = reason;
        }
    }

    public class TripTimeoutEvent : DomainEvent
    {
        public TripTimeoutEvent(Guid tripId) : base(tripId) { }
    }

    // Driver Events
    public class DriverLocationUpdatedEvent : DomainEvent
    {
        public GeoLocation Location { get; }
        public DriverLocationUpdatedEvent(Guid driverId, GeoLocation location) : base(driverId)
        {
            Location = location;
        }
    }

    public class DriverStatusChangedEvent : DomainEvent
    {
        public DriverStatus NewStatus { get; }
        public DriverStatus OldStatus { get; }
        
        public DriverStatusChangedEvent(Guid driverId, DriverStatus oldStatus, DriverStatus newStatus) : base(driverId)
        {
            OldStatus = oldStatus;
            NewStatus = newStatus;
        }
    }
}