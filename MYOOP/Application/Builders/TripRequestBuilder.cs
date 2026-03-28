using OOP.Domain.Entities;
using OOP.Domain.Enums;

namespace OOP.Application.Builders
{
    /// <summary>
    /// Builder Pattern - build TripRequest step-by-step.
    /// Avoids a large constructor and validates along the way.
    /// </summary>
    public class TripRequestBuilder
    {
        private Guid? _passengerId;
        private Guid? _fareRuleId;
        private GeoLocation? _Pickup;
        private GeoLocation? _Destination;
        private VehicleType? _VehicleType;
        private double _distance;
        private decimal _fare;
        private string? _passengerName;
        private string? _passengerPhone;
        private Route? _route;

        // Fluent setters

        /// <summary>
        /// Set passenger info.
        /// </summary>
        public TripRequestBuilder SetPassenger(Guid passengerId, string? name = null, string? phone = null)
        {
            _passengerId = passengerId;
            _passengerName = name;
            _passengerPhone = phone;
            return this;
        }

        /// <summary>
        /// Set fare rule ID.
        /// </summary>
        public TripRequestBuilder SetFareRule(Guid fareRuleId)
        {
            _fareRuleId = fareRuleId;
            return this;
        }

        /// <summary>
        /// Set pickup location.
        /// </summary>
        public TripRequestBuilder SetPickup(GeoLocation location)
        {
            _Pickup = location;
            return this;
        }

        /// <summary>
        /// Set destination.
        /// </summary>
        public TripRequestBuilder SetDestination(GeoLocation location)
        {
            _Destination = location;
            return this;
        }

        /// <summary>
        /// Set route details.
        /// </summary>
        public TripRequestBuilder SetRoute(Route route)
        {
            _route = route;
            return this;
        }

        /// <summary>
        /// Set vehicle type.
        /// </summary>
        public TripRequestBuilder SetVehicleType(VehicleType VehicleType)
        {
            _VehicleType = VehicleType;
            return this;
        }

        /// <summary>
        /// Set distance.
        /// </summary>
        public TripRequestBuilder SetDistance(double distance)
        {
            _distance = distance;
            return this;
        }

        /// <summary>
        /// Set fare.
        /// </summary>
        public TripRequestBuilder SetFare(decimal fare)
        {
            _fare = fare;
            return this;
        }

        /// <summary>
        /// Auto-calculate distance from route.
        /// </summary>
        public TripRequestBuilder CalculateDistanceFromRoute()
        {
            if (_route != null && _route.Distance > 0)
            {
                _distance = _route.Distance;
            }
            return this;
        }

        // Build Methods

        /// <summary>
        /// Validate required info before build.
        /// </summary>
        public TripRequestBuilder Validate()
        {
            if (!_passengerId.HasValue)
                throw new InvalidOperationException("Missing PassengerId.");

            if (!_fareRuleId.HasValue)
                throw new InvalidOperationException("Missing FareRuleId.");

            if (_Pickup == null)
                throw new InvalidOperationException("Missing Pickup.");

            if (_Destination == null)
                throw new InvalidOperationException("Missing Destination.");

            if (!_VehicleType.HasValue)
                throw new InvalidOperationException("Missing VehicleType.");

            if (_distance <= 0)
                throw new InvalidOperationException("Invalid distance.");

            if (GeoLocation.IsSameLocation(_Pickup, _Destination))
                throw new InvalidOperationException("Pickup and destination must be different.");

            return this;
        }

        /// <summary>
        /// Build Trip entity.
        /// </summary>
        public Trip Build()
        {
            Validate();

            var trip = new Trip(
                _passengerId!.Value,
                _fareRuleId!.Value,
                _Pickup!,
                _Destination!,
                _VehicleType!.Value,
                _distance,
                _fare
            );

            return trip;
        }

        /// <summary>
        /// Build TripRequest DTO.
        /// </summary>
        public TripRequestData BuildRequestData()
        {
            Validate();

            return new TripRequestData
            {
                PassengerId = _passengerId!.Value,
                PassengerName = _passengerName,
                PassengerPhone = _passengerPhone,
                FareRuleId = _fareRuleId!.Value,
                Pickup = _Pickup!,
                Destination = _Destination!,
                VehicleType = _VehicleType!.Value,
                Distance = _distance,
                Route = _route
            };
        }

        /// <summary>
        /// Reset builder to initial state.
        /// </summary>
        public TripRequestBuilder Reset()
        {
            _passengerId = null;
            _fareRuleId = null;
            _Pickup = null;
            _Destination = null;
            _VehicleType = null;
            _distance = 0;
            _passengerName = null;
            _passengerPhone = null;
            _route = null;
            return this;
        }
    }

    /// <summary>
    /// DTO for trip request data.
    /// </summary>
    public class TripRequestData
    {
        public Guid PassengerId { get; set; }
        public string? PassengerName { get; set; }
        public string? PassengerPhone { get; set; }
        public Guid FareRuleId { get; set; }
        public GeoLocation Pickup { get; set; } = null!;
        public GeoLocation Destination { get; set; } = null!;
        public VehicleType VehicleType { get; set; }
        public double Distance { get; set; }
        public Route? Route { get; set; }
    }

    /// <summary>
    /// Director - coordinates trip creation with templates.
    /// </summary>
    public class TripRequestDirector
    {
        private readonly TripRequestBuilder _builder;

        public TripRequestDirector(TripRequestBuilder builder)
        {
            _builder = builder;
        }

        /// <summary>
        /// Build trip from request data.
        /// </summary>
        public Trip BuildFromData(TripRequestData data)
        {
            return _builder
                .SetPassenger(data.PassengerId, data.PassengerName, data.PassengerPhone)
                .SetFareRule(data.FareRuleId)
                .SetPickup(data.Pickup)
                .SetDestination(data.Destination)
                .SetVehicleType(data.VehicleType)
                .SetRoute(data.Route!)
                .CalculateDistanceFromRoute()
                .Build();
        }

        /// <summary>
        /// Build simple trip (passenger + locations only).
        /// </summary>
        public Trip BuildSimpleTrip(Guid passengerId, GeoLocation pickup, GeoLocation destination,
            Guid fareRuleId, VehicleType vehicleType)
        {
            // Simple distance calculation (Haversine)
            double distance = CalculateHaversineDistance(pickup, destination);

            return _builder
                .SetPassenger(passengerId)
                .SetFareRule(fareRuleId)
                .SetPickup(pickup)
                .SetDestination(destination)
                .SetVehicleType(vehicleType)
                .SetDistance(distance)
                .Build();
        }

        /// <summary>
        /// Calculate distance using Haversine formula.
        /// </summary>
        private static double CalculateHaversineDistance(GeoLocation from, GeoLocation to)
        {
            const double R = 6371; // Earth radius in km
            double lat1 = from.Lat * Math.PI / 180;
            double lat2 = to.Lat * Math.PI / 180;
            double dLat = (to.Lat - from.Lat) * Math.PI / 180;
            double dLon = (to.Lng - from.Lng) * Math.PI / 180;

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1) * Math.Cos(lat2) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }
    }
}
