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
    public enum PaymentStatus
    {
        Unpaid,
        Paid
    }
}