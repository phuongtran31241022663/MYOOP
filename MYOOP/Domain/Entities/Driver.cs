﻿using OOP.Domain.Enums;
using System.Runtime.Serialization;

namespace OOP.Domain.Entities
{
    [DataContract]
    public class Driver : User
    {
        [DataMember] public DriverStatus Status { get; private set; }
        [DataMember] public Location CurrentLocation { get; private set; }
        [DataMember] public string LicenseNumber { get; private set; }
        [DataMember] public Vehicle Vehicle { get; private set; }

        // Tài chính
        [DataMember] public decimal Wallet { get; private set; }
        [DataMember] public decimal Income { get; private set; }
        [DataMember] public int TotalTrips { get; private set; }

        // Đánh giá
        [DataMember] private int ratingCount;
        [DataMember] private decimal ratingTotal;

        [IgnoreDataMember]
        public decimal AverageRating =>
            ratingCount == 0 ? 5.0m : Math.Round(ratingTotal / ratingCount, 2);

        protected Driver() { }

        public Driver(
            Guid id,
            string name,
            string phone,
            string hashedPassword,
            bool isActive,
            Vehicle vehicle,
            Location currentLocation,
            string licenseNumber)
            : base(id, name, phone, hashedPassword, isActive, UserRole.Driver)
        {
            Vehicle = vehicle ?? throw new ArgumentNullException(nameof(vehicle));
            CurrentLocation = currentLocation ?? throw new ArgumentNullException(nameof(currentLocation));
            LicenseNumber = licenseNumber;

            Status = DriverStatus.Available;
            Wallet = 0;
            Income = 0;
            TotalTrips = 0;
        }

        // ── Trạng thái ────────────────────────────────────────────────────────
        public void SetAvailable() => Status = DriverStatus.Available;

        public void SetBusy()
        {
            if (Status != DriverStatus.Available)
                throw new InvalidOperationException("Tài xế phải ở trạng thái Available.");

            Status = DriverStatus.Busy;
        }

        public void SetOffline()
        {
            if (Status == DriverStatus.Busy)
                throw new InvalidOperationException("Không thể offline khi đang trong chuyến đi.");

            Status = DriverStatus.Offline;
        }

        // ── Vị trí ───────────────────────────────────────────────────────────
        public void UpdateLocation(Location location)
        {
            CurrentLocation = location ?? throw new ArgumentNullException(nameof(location));
        }

        // ── Chuyến đi ────────────────────────────────────────────────────────
        public void AddTrip() => TotalTrips++;

        public void UpdateRating(int score)
        {
            if (score < 1 || score > 5)
                throw new ArgumentException("Điểm đánh giá phải từ 1 đến 5.", nameof(score));

            ratingCount++;
            ratingTotal += score;
        }

        // ── Tài chính ────────────────────────────────────────────────────────
        public void TopUpWallet(decimal amount)
        {
            if (amount <= 0)
                throw new ArgumentException("Số tiền nạp phải lớn hơn 0.", nameof(amount));

            Wallet += amount;
        }

        public void PayCommission(decimal fare, decimal commission)
        {
            if (fare < 0)
                throw new ArgumentException("Cước phí không thể âm.", nameof(fare));

            if (commission < 0)
                throw new ArgumentException("Hoa hồng không hợp lệ.", nameof(commission));

            if (fare < commission)
                throw new ArgumentException("Cước phí không thể nhỏ hơn hoa hồng.");

            if (Wallet < commission)
                throw new InvalidOperationException("Số dư ví không đủ để trả hoa hồng.");

            Wallet -= commission;
            Income += fare - commission;
        }

        public override string GetInfo() =>
            $"{base.GetInfo()}\nXe: {Vehicle.Model} ({Vehicle.PlateNumber})" +
            $"\nĐánh giá: {AverageRating}⭐ | Tổng chuyến: {TotalTrips} | Ví: {Wallet:N0} VNĐ";
    }
}