﻿using OOP.Domain.Enums;
using System.Runtime.Serialization;

namespace OOP.Domain.Entities
{
    [DataContract]
    public class Payment
    {
        #region Properties
        [DataMember] public Guid Id { get; init; }

        private Guid _tripId;
        [DataMember]
        public Guid TripId
        {
            get => _tripId;
            init => _tripId = value == Guid.Empty
                ? throw new ArgumentException("TripId không hợp lệ.")
                : value;
        }

        private decimal _amount;
        [DataMember]
        public decimal Amount
        {
            get => _amount;
            init => _amount = value <= 0
                ? throw new ArgumentException("Số tiền phải lớn hơn 0.")
                : value;
        }

        [DataMember] public decimal Commission { get; init; }

        private decimal _commissionRate;
        [DataMember]
        public decimal CommissionRate
        {
            get => _commissionRate;
            init => _commissionRate = value < 0 || value > 1
                ? throw new ArgumentException("Tỉ lệ hoa hồng phải từ 0 đến 1.")
                : value;
        }

        [DataMember] public decimal DriverIncome { get; init; }
        [DataMember] public PaymentStatus Status { get; private set; }
        [DataMember] public DateTime? PaidAt { get; private set; }
        #endregion
        #region Constructors
        protected Payment() { }

        public Payment(Guid tripId, decimal amount, decimal commissionRate)
        {
            // Properties will validate automatically via their setters
            Id = Guid.NewGuid();
            TripId = tripId;
            Amount = amount;
            CommissionRate = commissionRate;
            Commission = Math.Round(amount * commissionRate, 2);
            DriverIncome = amount - Commission;
            Status = PaymentStatus.Unpaid;
            PaidAt = null;
        }
        #endregion
        public void MarkPaid()
        {
            if (Status != PaymentStatus.Unpaid)
                throw new InvalidOperationException("Giao dịch đã được xử lý trước đó.");

            Status = PaymentStatus.Paid;
            PaidAt = DateTime.UtcNow;
        }
        public override string ToString() =>
            $"Payment {Id.ToString()[..8]} | {Status} | {Amount:N0} VNĐ" +
            $" (Tài xế nhận: {DriverIncome:N0} VNĐ)";
    }
}