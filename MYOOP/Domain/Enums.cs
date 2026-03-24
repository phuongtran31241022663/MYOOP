namespace OOP.Domain.Enums
{
    /// <summary>
    /// Trạng thái làm việc của tài xế trong hệ thống
    /// </summary>
    public enum DriverStatus
    {
        Inactive = 0, // Không làm việc 
        Offline = 0,   // Alias for Inactive
        Available = 1, // Đang hoạt động (alias for Active)
        Active = 1, // Đang hoạt động
        Busy = 2,   // Alias for OnTrip
        OnTrip = 2 // Đang bận chạy
    }

    /// <summary>
    /// Loại phương tiện trong hệ thống
    /// </summary>
    public enum VehicleType
    {
        Motorbike = 0,
        Car = 1
    }

    /// <summary>
    /// Trạng thái của một chuyến đi (Trip)
    /// </summary>
    public enum TripStatus
    {
        Requested = 0,   // Đã yêu cầu (Khách hàng vừa bấm đặt xe)
        Searching = 1,   // Đang tìm tài xế (Hệ thống đang quét tìm xe gần đây)
        Matched = 2,     // Đã tìm thấy tài xế (Tài xế đã chấp nhận chuyến)
        Arrived = 3,     // Tài xế đã đến (Đã có mặt tại điểm đón)
        Started = 4,     // Đã bắt đầu (Khách đã lên xe, chuyến đi đang diễn ra)
        Completed = 5,   // Đã hoàn thành (Kết thúc hành trình an toàn)
        Cancelled = 6,   // Đã hủy (Chuyến đi bị hủy bởi khách hoặc tài xế)
        Timeout = 7     // Hết thời gian chờ (Không tìm được tài xế trong thời gian quy định)
    }

    /// <summary>
    /// Bộ điều khiển luồng trạng thái (State Machine) cho Chuyến đi
    /// Đảm bảo logic chuyển đổi trạng thái hợp lệ.
    /// </summary>
    public static class TripStateMachine
    {
        // Định nghĩa các bước chuyển trạng thái hợp lệ
        private static readonly Dictionary<TripStatus, HashSet<TripStatus>> ValidTransitions = new()
        {
            // Requested -> Searching, Cancelled
            [TripStatus.Requested] = [TripStatus.Searching, TripStatus.Cancelled],
            // Searching -> Matched, Cancelled, Timeout
            [TripStatus.Searching] = [TripStatus.Matched, TripStatus.Cancelled, TripStatus.Timeout],
            // Matched -> Arrived, Cancelled
            [TripStatus.Matched] = [TripStatus.Arrived, TripStatus.Cancelled],
            // Arrived -> Started
            [TripStatus.Arrived] = [TripStatus.Started],
            // Started -> Completed
            [TripStatus.Started] = [TripStatus.Completed],
            // Các trạng thái kết thúc (Final States): Không thể chuyển được nữa
            [TripStatus.Completed] = [],
            [TripStatus.Cancelled] = [],
            [TripStatus.Timeout] = []
        };
        public static bool CanTransition(TripStatus from, TripStatus to)
        {
            return ValidTransitions.TryGetValue(from, out var validTargets) && validTargets.Contains(to);
        }
    }
}
