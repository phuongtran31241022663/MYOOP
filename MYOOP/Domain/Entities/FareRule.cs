using OOP.Application.Validators;
using OOP.Domain.Enums;
using System.Runtime.Serialization;

namespace OOP.Domain.Entities
{
    [DataContract]
    public class FareRule
    {
        [DataMember]
        public Guid Id { get; init; }
        [DataMember]
        public VehicleType VehicleType { get; private set; }

        [DataMember]
        public decimal BaseFare { get; private set; }

        [DataMember]
        public decimal PricePerKm { get; private set; }
        [DataMember]
        public decimal MinimumFare { get; private set; }

        [DataMember]
        public decimal CommissionRate { get; private set; }
        [DataMember]
        public DateTime UpdatedAt { get; private set; }
        protected FareRule() { }
        public FareRule(VehicleType vehicleType, decimal baseFare, decimal pricePerKm, decimal minimumFare, decimal commissionRate)
        {
            FareRuleValidator.Validate(baseFare, pricePerKm, minimumFare, commissionRate);

            Id = Guid.NewGuid(); // FIX: Id was never assigned
            VehicleType = vehicleType;
            BaseFare = baseFare;
            PricePerKm = pricePerKm;
            MinimumFare = minimumFare;
            CommissionRate = commissionRate;
            UpdatedAt = DateTime.UtcNow;
        }
        public void Update(decimal baseFare, decimal pricePerKm, decimal minimumFare, decimal commissionRate)
        {
            FareRuleValidator.Validate(baseFare, pricePerKm, minimumFare, commissionRate);

            BaseFare = baseFare;
            PricePerKm = pricePerKm;
            MinimumFare = minimumFare;
            CommissionRate = commissionRate;
            UpdatedAt = DateTime.UtcNow;
        }
        public decimal CalculateFare(double distanceKm)
        {
            if (distanceKm < 0) throw new ArgumentException("Khoảng cách không thể âm.");

            // Đặc cách cho chuyến đi siêu ngắn
            if (distanceKm < 0.5) return 10000m;

            // Tính toán dựa trên đơn giá
            decimal total = BaseFare + ((decimal)distanceKm * PricePerKm);

            // Lấy giá trị lớn nhất giữa giá tính toán và giá sàn hệ thống
            decimal fare = Math.Max(total, MinimumFare);

            // Làm tròn về đơn vị nghìn đồng gần nhất
            return Math.Floor(fare / 1000m) * 1000m;
        }
    }
}