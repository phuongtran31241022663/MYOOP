using OOP.Domain.Enums;
using System.Runtime.Serialization;

namespace OOP.Domain.Entities
{
    [DataContract]
    public class Payment
    {
        [DataMember]
        public Guid Id { get; init; }
        [DataMember]
        public Guid TripId { get; init; }
        // Tổng số tiền khách trả
        [DataMember]
        public decimal Amount { get; init; }

        // Hoa hồng hệ thống thu
        [DataMember]
        public decimal Commission { get; init; }
        [DataMember]
        public decimal CommissionRate { get; init; }
        // Số tiền tài xế thực nhận
        [DataMember]
        public decimal DriverIncome { get; init; }
        [DataMember]
        public PaymentStatus Status { get; private set; }
        [DataMember]
        public DateTime? PaidAt { get; private set; }
        protected Payment() { }
        public Payment(Guid tripId, decimal amount, decimal commissionRate)
        {
            if (tripId == Guid.Empty)
                throw new ArgumentException("TripId không hợp lệ.");

            if (amount <= 0)
                throw new ArgumentException("Số tiền phải lớn hơn 0.");

            if (commissionRate < 0 || commissionRate > 1)
                throw new ArgumentException("Tỉ lệ hoa hồng phải từ 0 đến 1.");

            Id = Guid.NewGuid();
            TripId = tripId;
            Amount = amount;
            CommissionRate = commissionRate;

            Commission = Math.Round(amount * commissionRate, 2);
            DriverIncome = amount - Commission;

            Status = PaymentStatus.Unpaid;
            PaidAt = null;
        }
        public void MarkPaid()
        {
            if (Status != PaymentStatus.Unpaid)
                throw new InvalidOperationException("Giao dịch đã được xử lý trước đó.");

            Status = PaymentStatus.Paid;
            PaidAt = DateTime.Now;
        }

        // FIX: MarkFailed() đã bị xóa — hệ thống chỉ hỗ trợ thanh toán tiền mặt,
        // chỉ cần 2 trạng thái Unpaid và Paid là đủ.

        public override string ToString() =>
            $"Payment {Id.ToString()[..8]} | {Status} | {Amount:N0} VNĐ" +
            $" (Tài xế nhận: {DriverIncome:N0} VNĐ)";
    }
}