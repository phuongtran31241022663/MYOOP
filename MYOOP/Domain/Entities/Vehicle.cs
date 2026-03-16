using OOP.Application.Validators;
using OOP.Domain.Enums;
using System.Runtime.Serialization;

namespace OOP.Domain.Entities
{
    [DataContract]
    [KnownType(typeof(Motorbike))]
    [KnownType(typeof(Car))]
    public abstract class Vehicle
    {
        [DataMember] public Guid Id { get; private set; }
        [DataMember] public Guid DriverId { get; private set; }
        [DataMember] public string PlateNumber { get; private set; }
        [DataMember] public VehicleType Type { get; private set; }
        [DataMember] public string Brand { get; private set; }
        [DataMember] public string Model { get; private set; }
        [DataMember] public string Color { get; private set; }
        [DataMember] public int Capacity { get; protected set; }

        protected Vehicle() { }

        public Vehicle(
            Guid driverId,
            VehicleType type,
            string plateNumber,
            string brand,
            string model,
            string color,
            byte capacity)
        {
            Id = Guid.NewGuid();
            DriverId = driverId;
            Type = type;
            PlateNumber = plateNumber;
            Brand = brand;
            Model = model;
            Color = color;
            Capacity = capacity;
            UserValidator.ValidateVehicle(this);
        }

        public void UpdateVehicleInfo(string plateNumber, string brand,
                                      string model, string color, int capacity)
        {
            // Snapshot current state for rollback
            var snapshot = (PlateNumber, Brand, Model, Color, Capacity);

            PlateNumber = plateNumber;
            Brand = brand;
            Model = model;
            Color = color;
            Capacity = capacity;

            try
            {
                UserValidator.ValidateVehicle(this);
            }
            catch
            {
                (PlateNumber, Brand, Model, Color, Capacity) = snapshot;
                throw;
            }
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
    }

    [DataContract]
    public class Car : Vehicle
    {
        protected Car() { }

        public Car(Guid driverId, string plateNumber,
                   string brand, string model, string color, int capacity)
            : base(driverId, VehicleType.Car, plateNumber, brand, model, color,
                   ValidateCapacity(capacity))
        { }

        /// <summary>
        /// Validates and converts capacity before passing to the base constructor.
        /// Throws <see cref="ArgumentOutOfRangeException"/> for values outside [2, 7],
        /// and <see cref="OverflowException"/> for values that cannot fit in a byte
        /// (capacity > 255), even though the business rule prevents reaching that.
        /// </summary>
        private static byte ValidateCapacity(int capacity)
        {
            if (capacity < 2 || capacity > 7)
                throw new ArgumentOutOfRangeException(
                    nameof(capacity), capacity, "Số chỗ ngồi phải từ 2 đến 7.");

            return checked((byte)capacity);
        }
    }
}
