﻿namespace OOP.Domain.Enums
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
        Requested = 0,
        Matched = 1,
        Arrived = 2,
        Started = 3,
        Completed = 4,
        Cancelled = 5,
        Searching = 6,
        Timeout = 7,
        Ongoing = Started
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
