using OOP.Domain.Entities;

namespace OOP.Application.Builders
{
    /// <summary>
    /// Builder Pattern - Xây dựng TripRequest step-by-step.
    /// Tránh constructor nhiều tham số, dễ validate từng bước.
    /// </summary>
    public class TripRequestBuilder
    {
        private Guid? _passengerId;
        private Guid? _fareRuleId;
        private GeoLocation? _Pickup;
        private GeoLocation? _Destination;
        private string? _VehicleType;
        private double _distance;
        private decimal _fare;
        private string? _passengerName;
        private string? _passengerPhone;
        private Route? _route;

        // ─── Fluent Setters ─────────────────────────────────────────────────

        /// <summary>
        /// Set thông tin hành khách
        /// </summary>
        public TripRequestBuilder SetPassenger(Guid passengerId, string? name = null, string? phone = null)
        {
            _passengerId = passengerId;
            _passengerName = name;
            _passengerPhone = phone;
            return this;
        }

        /// <summary>
        /// Set fare rule ID
        /// </summary>
        public TripRequestBuilder SetFareRule(Guid fareRuleId)
        {
            _fareRuleId = fareRuleId;
            return this;
        }

        /// <summary>
        /// Set điểm đón
        /// </summary>
        public TripRequestBuilder SetPickup(GeoLocation location)
        {
            _Pickup = location;
            return this;
        }

        /// <summary>
        /// Set điểm đến
        /// </summary>
        public TripRequestBuilder SetDestination(GeoLocation location)
        {
            _Destination = location;
            return this;
        }

        /// <summary>
        /// Set route (lộ trình chi tiết)
        /// </summary>
        public TripRequestBuilder SetRoute(Route route)
        {
            _route = route;
            return this;
        }

        /// <summary>
        /// Set loại xe
        /// </summary>
        public TripRequestBuilder SetVehicleType(string VehicleType)
        {
            _VehicleType = VehicleType;
            return this;
        }

        /// <summary>
        /// Set khoảng cách
        /// </summary>
        public TripRequestBuilder SetDistance(double distance)
        {
            _distance = distance;
            return this;
        }

        /// <summary>
        /// Set giá tiền (fare) cho chuyến đi
        /// </summary>
        public TripRequestBuilder SetFare(decimal fare)
        {
            _fare = fare;
            return this;
        }

        /// <summary>
        /// Tự động tính khoảng cách từ route
        /// </summary>
        public TripRequestBuilder CalculateDistanceFromRoute()
        {
            if (_route != null && _route.Distance > 0)
            {
                _distance = _route.Distance;
            }
            return this;
        }

        // ─── Build Methods ─────────────────────────────────────────────────

        /// <summary>
        /// Validate tất cả thông tin trước khi build
        /// </summary>
        public TripRequestBuilder Validate()
        {
            if (!_passengerId.HasValue)
                throw new InvalidOperationException("Thiếu thông tin hành khách (PassengerId).");

            if (!_fareRuleId.HasValue)
                throw new InvalidOperationException("Thiếu FareRuleId.");

            if (_Pickup == null)
                throw new InvalidOperationException("Thiếu điểm đón (Pickup).");

            if (_Destination == null)
                throw new InvalidOperationException("Thiếu điểm đến (Destination).");

            if (string.IsNullOrWhiteSpace(_VehicleType))
                throw new InvalidOperationException("Thiếu loại xe (VehicleType).");

            if (_distance <= 0)
                throw new InvalidOperationException("Khoảng cách không hợp lệ.");

            // Validate pickup != destination
            if (GeoLocation.IsSameLocation(_Pickup, _Destination))
                throw new InvalidOperationException("Điểm đón và điểm đến không được trùng nhau.");

            return this;
        }

        /// <summary>
        /// Build Trip entity
        /// </summary>
        public Trip Build()
        {
            Validate();

            var trip = new Trip(
                _passengerId!.Value,
                _fareRuleId!.Value,
                _Pickup!,
                _Destination!,
                _VehicleType!,
                _distance,
                _fare
            );

            // Attach route nếu có (sử dụng ApplyDistance thay vì SetRoute)
            if (_route != null && _route.Distance > 0)
            {
                trip.ApplyDistance(_route.Distance);
            }

            return trip;
        }

        /// <summary>
        /// Build TripRequest DTO (nếu cần)
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
                VehicleType = _VehicleType!,
                Distance = _distance,
                Route = _route
            };
        }

        /// <summary>
        /// Reset builder về trạng thái ban đầu
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
    /// DTO chứa thông tin trip request - có thể serialize/deserialize
    /// </summary>
    public class TripRequestData
    {
        public Guid PassengerId { get; set; }
        public string? PassengerName { get; set; }
        public string? PassengerPhone { get; set; }
        public Guid FareRuleId { get; set; }
        public GeoLocation Pickup { get; set; } = null!;
        public GeoLocation Destination { get; set; } = null!;
        public string VehicleType { get; set; } = string.Empty;
        public double Distance { get; set; }
        public Route? Route { get; set; }
    }

    /// <summary>
    /// Director - Điều phối các bước tạo trip theo template có sẵn
    /// </summary>
    public class TripRequestDirector
    {
        private readonly TripRequestBuilder _builder;

        public TripRequestDirector(TripRequestBuilder builder)
        {
            _builder = builder;
        }

        /// <summary>
        /// Tạo trip request từ dữ liệu có sẵn
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
        /// Tạo trip request đơn giản (chỉ cần passenger + locations)
        /// </summary>
        public Trip BuildSimpleTrip(Guid passengerId, GeoLocation pickup, GeoLocation destination, 
            Guid fareRuleId, string vehicleType)
        {
            // Tính khoảng cách đơn giản (Haversine formula)
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
        /// Tính khoảng cách giữa 2 điểm theo Haversine formula
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
