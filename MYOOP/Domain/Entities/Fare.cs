﻿using OOP.Application.Validators;
using OOP.Domain.Enums;
using System.Runtime.Serialization;

namespace OOP.Domain.Entities
{
    [DataContract]
    public class Fare
    {
        [DataMember] public Guid Id { get; init; }
        [DataMember] public VehicleType VehicleType { get; private set; }
        [DataMember] public decimal BaseFare { get; private set; }
        [DataMember] public decimal PricePerKm { get; private set; }
        [DataMember] public decimal MinimumFare { get; private set; }
        [DataMember] public decimal CommissionRate { get; private set; }
        [DataMember] public DateTime UpdatedAt { get; private set; }

        protected Fare() { }

        public Fare(VehicleType vehicleType, decimal baseFare, decimal pricePerKm,
                    decimal minimumFare, decimal commissionRate)
        {
            FareRuleValidator.Validate(baseFare, pricePerKm, minimumFare, commissionRate);

            Id = Guid.NewGuid();
            VehicleType = vehicleType;
            BaseFare = baseFare;
            PricePerKm = pricePerKm;
            MinimumFare = minimumFare;
            CommissionRate = commissionRate;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Update(decimal baseFare, decimal pricePerKm,
                           decimal minimumFare, decimal commissionRate)
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
            if (distanceKm < 0)
                throw new ArgumentException("Khoảng cách không thể âm.", nameof(distanceKm));

            decimal total = BaseFare + ((decimal)distanceKm * PricePerKm);

            decimal fare = Math.Max(total, MinimumFare);

            return Math.Floor(fare / 1000m) * 1000m;
        }
    }
}
