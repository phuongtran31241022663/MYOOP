using System.Runtime.Serialization;

namespace OOP.Domain.Entities
{
    [DataContract]
    public class Fare
    {
        #region Properties
        [DataMember] public Guid Id { get; init; }

        [DataMember] public string VehicleType { get; private set; }

        /// <summary>
        /// Alias for VehicleType - for backward compatibility
        /// </summary>
        public string VehicleTypeName => VehicleType;

        private decimal baseFare;
        [DataMember]
        public decimal BaseFare
        {
            get => baseFare;
            private set => baseFare = value < 0
                ? throw new ArgumentException("Giá cơ bản không thể âm.")
                : value;
        }

        private decimal pricePerKm;
        [DataMember]
        public decimal PricePerKm
        {
            get => pricePerKm;
            private set => pricePerKm = value <= 0
                ? throw new ArgumentException("Giá mỗi km phải lớn hơn 0.")
                : value;
        }

        private decimal commissionRate;
        [DataMember]
        public decimal CommissionRate
        {
            get => commissionRate;
            private set => commissionRate = value < 0 || value > 1
                ? throw new ArgumentException("Tỷ lệ hoa hồng phải từ 0 đến 1 (0% – 100%).")
                : value;
        }

        [DataMember] public DateTime UpdatedAt { get; private set; }
        #endregion
        #region Constructors
        protected Fare() { }

        public Fare(string vehicleType, decimal baseFare, decimal pricePerKm, decimal commissionRate)
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
