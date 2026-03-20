using OOP.Domain.Enums;
using System.Runtime.Serialization;

namespace OOP.Domain.Entities
{
    [DataContract]
    [KnownType(typeof(Motorbike))]
    [KnownType(typeof(Car))]
    public abstract class Vehicle
    {
        #region Properties
        [DataMember] public Guid Id { get; private set; }

        private Guid _driverId;
        [DataMember]
        public Guid DriverId
        {
            get => _driverId;
            private set => _driverId = value == Guid.Empty
                ? throw new ArgumentException("Xe phải thuộc về tài xế hợp lệ.")
                : value;
        }

        private string _plateNumber = string.Empty;
        [DataMember]
        public string PlateNumber
        {
            get => _plateNumber;
            private set => _plateNumber = string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException("Biển số xe không được để trống.")
                : value.Length < 7
                    ? throw new ArgumentException("Biển số xe không đúng định dạng.")
                    : value.Trim();
        }

        [DataMember] public VehicleType Type { get; private set; }

        private string _brand = string.Empty;
        [DataMember]
        public string Brand
        {
            get => _brand;
            private set => _brand = string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException("Hãng xe không được để trống.")
                : value.Trim();
        }

        private string _model = string.Empty;
        [DataMember]
        public string Model
        {
            get => _model;
            private set => _model = string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException("Mẫu xe không được để trống.")
                : value.Trim();
        }

        private string _color = string.Empty;
        [DataMember]
        public string Color
        {
            get => _color;
            private set => _color = string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException("Màu xe không được để trống.")
                : value.Trim();
        }

        private int _capacity;
        [DataMember]
        public int Capacity
        {
            get => _capacity;
            protected set => _capacity = value <= 0
                ? throw new ArgumentException("Sức chứa không hợp lệ.")
                : value;
        }

        public abstract double GetAverageSpeed(); // km/h
        public abstract double GetMaxPickupDistance(); // km
        #endregion
        #region Constructors
        protected Vehicle() { }

        protected Vehicle(
             Guid driverId,
             VehicleType type,
             string plateNumber,
             string brand,
             string model,
             string color,
             int capacity)
        {
            Id = Guid.NewGuid();
            // Properties will validate automatically via their setters
            DriverId = driverId;
            Type = type;
            PlateNumber = plateNumber;
            Brand = brand;
            Model = model;
            Color = color;
            Capacity = capacity;
        }
        #endregion
        public void UpdateVehicleInfo(
             string plateNumber,
             string brand,
             string model,
             string color,
             int capacity)
        {
            // Properties will validate automatically via their setters
            PlateNumber = plateNumber;
            Brand = brand;
            Model = model;
            Color = color;
            Capacity = capacity;
        }

        public override string ToString() =>
            $"{Brand} {Model} | {PlateNumber} | {Capacity} chỗ | {Type}";
    }

    [DataContract]
    public class Motorbike : Vehicle
    {
        protected Motorbike() { }

        public Motorbike(Guid driverId, string plateNumber,
                       string brand, string model, string color)
          : base(driverId, VehicleType.Motorbike, plateNumber, brand, model, color, 2)
        { }

        public override double GetAverageSpeed() => 35;
        public override double GetMaxPickupDistance() => 5;
    }

    [DataContract]
    public class Car : Vehicle
    {
        protected Car() { }

        public Car(Guid driverId, string plateNumber,
               string brand, string model, string color, int capacity)
        : base(driverId, VehicleType.Car, plateNumber, brand, model, color, capacity)
        { }

        public override double GetAverageSpeed() => 55;
        public override double GetMaxPickupDistance() => 7;
    }
}