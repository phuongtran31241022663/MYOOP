using OOP.Domain.Enums;
using System.Runtime.Serialization;

namespace OOP.Domain.Entities
{
    [DataContract]
    public class Fare
    {
        #region Properties
        [DataMember] public Guid Id { get; init; }

        [DataMember] public VehicleType VehicleType { get; private set; }

        private decimal _baseFare;
        [DataMember]
        public decimal BaseFare
        {
            get => _baseFare;
            private set => _baseFare = value < 0
                ? throw new ArgumentException("Giá cơ bản (mở cửa) không thể âm.")
                : value;
        }

        private decimal _pricePerKm;
        [DataMember]
        public decimal PricePerKm
        {
            get => _pricePerKm;
            private set => _pricePerKm = value <= 0
                ? throw new ArgumentException("Giá mỗi km phải lớn hơn 0.")
                : value;
        }

        private decimal _commissionRate;
        [DataMember]
        public decimal CommissionRate
        {
            get => _commissionRate;
            private set => _commissionRate = value < 0 || value > 1
                ? throw new ArgumentException("Tỷ lệ hoa hồng phải từ 0 đến 1 (0% – 100%).")
                : value;
        }

        [DataMember] public DateTime UpdatedAt { get; private set; }
        #endregion
        #region Constructors
        protected Fare() { }

        public Fare(VehicleType vehicleType, decimal baseFare, decimal pricePerKm, decimal commissionRate)
        {
            Id = Guid.NewGuid();
            // Properties will validate automatically via their setters
            VehicleType = vehicleType;
            BaseFare = baseFare;
            PricePerKm = pricePerKm;
            CommissionRate = commissionRate;
            UpdatedAt = DateTime.UtcNow;
        }
        #endregion
        public void UpdateRule(decimal baseFare, decimal pricePerKm, decimal commissionRate)
        {
            // Properties will validate automatically via their setters
            BaseFare = baseFare;
            PricePerKm = pricePerKm;
            CommissionRate = commissionRate;
            UpdatedAt = DateTime.UtcNow;
        }

        public decimal CalculateFare(double distanceKm)
        {
            if (distanceKm < 0)
                throw new ArgumentException("Khoảng cách không hợp lệ.", nameof(distanceKm));

            decimal fare = BaseFare + ((decimal)distanceKm * PricePerKm);

            return Math.Floor(fare / 1000m) * 1000m;
        }
    }
}
