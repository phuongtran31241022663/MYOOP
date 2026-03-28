using System.Runtime.Serialization;
using OOP.Domain.Enums;

namespace OOP.Domain.Entities
{
    [DataContract]
    [KnownType(typeof(Motorbike))]
    [KnownType(typeof(Car))]
    public abstract class Vehicle
    {
        #region Properties
        [DataMember] public Guid Id { get; private set; }

        private Guid driverId;
        [DataMember]
        public Guid DriverId
        {
            get => driverId;
            private set => driverId = value == Guid.Empty
                ? throw new ArgumentException("Xe phải thuộc về tài xế hợp lệ.")
                : value;
        }

        private string plateNumber = string.Empty;
        [DataMember]
        public string PlateNumber
        {
            get => plateNumber;
            private set => plateNumber = string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException("Biển số xe không được để trống.")
                : value.Trim();
        }

        private string brand = string.Empty;
        [DataMember]
        public string Brand
        {
            get => brand;
            private set => brand = string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException("Hãng xe không được để trống.")
                : value.Trim();
        }

        private string model = string.Empty;
        [DataMember]
        public string Model
        {
            get => model;
            private set => model = string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException("Mẫu xe không được để trống.")
                : value.Trim();
        }

        private string color = string.Empty;
        [DataMember]
        public string Color
        {
            get => color;
            private set => color = string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException("Màu xe không được để trống.")
                : value.Trim();
        }

        private int capacity;
        [DataMember]
        public int Capacity
        {
            get => capacity;
            protected set => capacity = value <= 0
                ? throw new ArgumentException("Sức chứa không hợp lệ.")
                : value;
        }
        public abstract VehicleType GetVehicleType();
        public abstract bool IsCar();
        public abstract double GetMinSpeed(); // km/h
        public abstract double GetMaxSpeed(); // km/h
        public abstract double GetMaxPickupDistance(); // km
        #endregion
        #region Constructors
        protected Vehicle() { }

        protected Vehicle(
             Guid driverId,
             string plateNumber,
             string brand,
             string model,
             string color,
             int capacity)
        {
            Id = Guid.NewGuid();
            DriverId = driverId;
            PlateNumber = plateNumber;
            Brand = brand;
            Model = model;
            Color = color;
            Capacity = capacity;
        }
        #endregion
        public virtual void UpdateVehicleInfo(
             string plateNumber,
             string brand,
             string model,
             string color,
             int capacity)
        {
            PlateNumber = plateNumber;
            Brand = brand;
            Model = model;
            Color = color;
            Capacity = capacity;
        }

        public override string ToString() =>
            $"{Brand} {Model} | {PlateNumber} | {Capacity} chỗ | {GetVehicleType()}";
    }

    [DataContract]
    public class Motorbike : Vehicle
    {
        protected Motorbike() { }

        public Motorbike(Guid driverId, string plateNumber,
                       string brand, string model, string color)
          : base(driverId, plateNumber, brand, model, color, 2)
        { }

        public override VehicleType GetVehicleType() => VehicleType.Motorbike;
        public override bool IsCar() => false;

        public override double GetMinSpeed() => 25; // km/h
        public override double GetMaxSpeed() => 45; // km/h
        public override double GetMaxPickupDistance() => 5;
    }

    [DataContract]
    public class Car : Vehicle
    {
        protected Car() { }

        public Car(Guid driverId, string plateNumber,
           string brand, string model, string color, int capacity)
           : base(driverId, plateNumber, brand, model, color, capacity)
        {
            if (capacity < 4)
                throw new ArgumentException("Xe ô tô phải có ít nhất 4 chỗ ngồi.");
        }
        public override VehicleType GetVehicleType() => VehicleType.Car;
        public override bool IsCar() => true;

        public override double GetMinSpeed() => 40; // km/h
        public override double GetMaxSpeed() => 70; // km/h
        public override double GetMaxPickupDistance() => 7;
        public override void UpdateVehicleInfo(string plateNumber, string brand, string model, string color, int capacity)
        {
            if (capacity < 4)
                throw new ArgumentException("Xe ô tô phải có ít nhất 4 chỗ ngồi.");

            base.UpdateVehicleInfo(plateNumber, brand, model, color, capacity);
        }
    }
}