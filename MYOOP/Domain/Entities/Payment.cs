﻿using System.Runtime.Serialization;

namespace OOP.Domain.Entities
{
    [DataContract]
    public class Payment
    {
        #region Properties
        [DataMember] public Guid Id { get; init; }

        private Guid tripId;
        [DataMember]
        public Guid TripId
        {
            get => tripId;
            init => tripId = value == Guid.Empty
                ? throw new ArgumentException("TripId không hợp lệ.")
                : value;
        }

        private decimal amount;
        [DataMember]
        public decimal Amount
        {
            get => amount;
            init => amount = value <= 0
                ? throw new ArgumentException("Số tiền phải lớn hơn 0.")
                : value;
        }

        [DataMember] public decimal Commission { get; init; }

        private decimal commissionRate;
        [DataMember]
        public decimal CommissionRate
        {
            get => commissionRate;
            init => commissionRate = value < 0 || value > 1
                ? throw new ArgumentException("Tỉ lệ hoa hồng phải từ 0 đến 1.")
                : value;
        }

        [DataMember] public decimal DriverIncome { get; init; }
        [DataMember] public bool IsPaid { get; private set; }
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
            IsPaid = false;
            PaidAt = null;
        }
        #endregion
        public void MarkPaid()
        {
            if (IsPaid)
                throw new InvalidOperationException("Giao dịch đã được xử lý trước đó.");

            IsPaid = true;
            PaidAt = DateTime.UtcNow;
        }
    }
}