using OOP.Domain.Entities;
using OOP.Domain.Enums;

namespace OOP.Application.Validators
{
    public static class FareRuleValidator
    {
        public static void ValidateRule(Fare rule)
        {
            if (rule == null)
                throw new ArgumentNullException(nameof(rule));

            if (!Enum.IsDefined(typeof(VehicleType), rule.VehicleType))
                throw new ArgumentException("Loại xe không hợp lệ.");

            Validate(rule.BaseFare, rule.PricePerKm, rule.MinimumFare, rule.CommissionRate);
        }

        public static void Validate(
            decimal baseFare,
            decimal pricePerKm,
            decimal minimumFare,
            decimal commissionRate)
        {
            if (baseFare < 0)
                throw new ArgumentException("Giá cơ bản (mở cửa) không thể âm.");

            if (pricePerKm <= 0)
                throw new ArgumentException("Giá mỗi km phải lớn hơn 0.");

            if (minimumFare < 0)
                throw new ArgumentException("Giá tối thiểu không thể âm.");

            if (commissionRate < 0 || commissionRate > 1)
                throw new ArgumentException("Tỷ lệ hoa hồng phải từ 0 đến 1 (0% – 100%).");
        }
    }
}