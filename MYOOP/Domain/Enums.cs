namespace OOP.Domain.Enums
{
    public enum UserRole
    {
        Passenger,
        Driver,
        Admin
    }
    public enum DriverStatus
    {
        Busy,
        Available,
        Offline
    }
    public enum TripStatus
    {
        Requested,
        Matched,
        Arrived,
        Ongoing,
        Completed,
        Cancelled
    }
    public enum VehicleType
    {
        Motorbike,
        Car
    }
    // FIX: Chỉ giữ Unpaid và Paid — hệ thống chỉ hỗ trợ thanh toán tiền mặt,
    // không có giao dịch điện tử nên trạng thái Failed không có ý nghĩa.
    public enum PaymentStatus
    {
        Unpaid,
        Paid
    }
}