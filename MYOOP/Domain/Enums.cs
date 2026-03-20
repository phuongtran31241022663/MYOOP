﻿using System.Collections.Generic;

namespace OOP.Domain.Enums
{
    public enum DriverStatus
    {
        Offline,    // không nhận chuyến (tắt app)
        Available, // rảnh, nhận chuyến được
        Busy       // đang chạy chuyến (không nhận chuyến mới)
    }

    public enum TripStatus
    {
        Requested = 0,   // tạo trip
        Searching = 1,   // đang tìm tài xế
        Matched = 2,     // đã có tài xế
        Arrived = 3,     // tài xế đã đến
        Started = 4,     // bắt đầu chuyến đi
        Completed = 5,   // hoàn thành
        Cancelled = 6,   // bị hủy (global exit)
        Timeout = 7     // hết thời gian tìm tài xế
    }

    /// <summary>
    /// Dictionary chỉ cho phép chuyển trạng thái hợp lệ cho Trip
    /// </summary>
    public static class TripStatusTransitions
    {
        public static readonly Dictionary<TripStatus, HashSet<TripStatus>> ValidTransitions = new()
        {
            // Requested có thể chuyển sang Searching, Matched, Cancelled, Timeout
            { TripStatus.Requested, new HashSet<TripStatus> { TripStatus.Searching, TripStatus.Matched, TripStatus.Cancelled, TripStatus.Timeout } },

            // Searching có thể chuyển sang Matched, Cancelled, Timeout
            { TripStatus.Searching, new HashSet<TripStatus> { TripStatus.Matched, TripStatus.Cancelled, TripStatus.Timeout } },

            // Matched có thể chuyển sang Arrived, Cancelled
            { TripStatus.Matched, new HashSet<TripStatus> { TripStatus.Arrived, TripStatus.Cancelled } },

            // Arrived có thể chuyển sang Started, Cancelled
            { TripStatus.Arrived, new HashSet<TripStatus> { TripStatus.Started, TripStatus.Cancelled } },

            // Started có thể chuyển sang Completed, Cancelled
            { TripStatus.Started, new HashSet<TripStatus> { TripStatus.Completed, TripStatus.Cancelled } },

            // Completed, Cancelled, Timeout là trạng thái cuối, không chuyển được nữa
            { TripStatus.Completed, new HashSet<TripStatus>() },
            { TripStatus.Cancelled, new HashSet<TripStatus>() },
            { TripStatus.Timeout, new HashSet<TripStatus>() }
        };

        public static bool CanTransition(TripStatus from, TripStatus to)
        {
            return ValidTransitions.TryGetValue(from, out var validTargets) && validTargets.Contains(to);
        }
    }

    public enum MatchResult
    {
        Success,
        Timeout,
        NoDriverAvailable
    }
    public enum VehicleType
    {
        Motorbike,
        Car
    }
    public enum PaymentStatus
    {
        Unpaid,
        Paid
    }
}
