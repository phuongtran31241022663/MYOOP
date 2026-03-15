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
        [DataMember]
        public Guid Id { get; private set; }
        [DataMember]
        public Guid DriverId { get; private set; }
        // Biển số xe (định danh vật lý của phương tiện)
        [DataMember]
        public string PlateNumber { get; private set; }
        [DataMember]
        public VehicleType Type { get; private set; }
        [DataMember]
        public string Brand { get; private set; }
        [DataMember]
        public string Model { get; private set; }
        [DataMember]
        public string Color { get; private set; }
        [DataMember]
        public int Capacity { get; protected set; }
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
        public void UpdateVehicleInfo(string plateNumber, string brand, string model, string color, int capacity)
        {
            // Lưu lại giá trị cũ để backup nếu validate thất bại
            var oldPlate = PlateNumber;
            var oldBrand = Brand;
            var oldModel = Model;
            var oldColor = Color;
            var oldCapacity = Capacity;

            // Gán giá trị mới
            PlateNumber = plateNumber;
            Brand = brand;
            Model = model;
            Color = color;
            Capacity = capacity;

            try
            {
                // Dùng Validator đã tạo để kiểm tra toàn bộ "diện mạo" mới của xe
                UserValidator.ValidateVehicle(this);
            }
            catch (Exception)
            {
                // Nếu dữ liệu mới sai (VD: capacity > 7), hồi phục dữ liệu cũ
                PlateNumber = oldPlate;
                Brand = oldBrand;
                Model = oldModel;
                Color = oldColor;
                Capacity = oldCapacity;
                throw; // Ném lỗi ra cho UI (WinForms) hiển thị MessageBox
            }
        }

        public override string ToString() =>
            $"{Brand} {Model} | {PlateNumber} | {Capacity} chỗ | {Type}";
    }
    [DataContract]
    public class Motorbike : Vehicle
    {
        protected Motorbike() { }
        public Motorbike(Guid driverId, string plateNumber, string brand, string model, string color)
            : base(driverId, VehicleType.Motorbike, plateNumber, brand, model, color, 2)
        {
        }
    }
    [DataContract]
    public class Car : Vehicle
    {
        protected Car() { }
        public Car(Guid driverId, string plateNumber, string brand, string model, string color, int capacity)
            : base(driverId, VehicleType.Car, plateNumber, brand, model, color, (byte)capacity)
        {
            if (capacity < 2 || capacity > 7)
                throw new ArgumentOutOfRangeException(nameof(capacity), "Số chỗ ngồi phải từ 2 đến 7.");
        }
    }
}
