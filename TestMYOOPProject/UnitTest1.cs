using OOP
using OOP.Domain.Enums;
using System;
using System.Diagnostics;
using System.Net.NetworkInformation;

namespace OOP.Tests
{
    public static class ManualTest
    {
        public static void RunAllTests()
        {
            Console.WriteLine("--- BẮT ĐẦU KIỂM THỬ HỆ THỐNG ---");

            TestTripStatusFlow();
            TestFareCalculation();

            Console.WriteLine("--- KẾT THÚC KIỂM THỬ ---");
        }

        private static void TestTripStatusFlow()
        {
            try
            {
                // 1. Arrange
                var pickup = new Location("A", "Q1", 10.7, 106.6);
                var dest = new Location("B", "Q5", 10.8, 106.7);
                var trip = new Trip(Guid.NewGuid(), Guid.NewGuid(), pickup, dest, VehicleType.Car, 5.0);

                // 2. Act
                trip.AssignDriver(Guid.NewGuid());
                trip.MarkArrived();
                trip.StartTrip();

                // 3. Assert (Tự viết logic kiểm tra)
                if (trip.Status == TripStatus.Active)
                {
                    Console.WriteLine("[PASS] TestTripStatusFlow: Trạng thái chuyển sang Active thành công.");
                }
                else
                {
                    Console.WriteLine($"[FAIL] TestTripStatusFlow: Trạng thái mong đợi Active nhưng nhận được {trip.Status}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] TestTripStatusFlow có lỗi: {ex.Message}");
            }
        }

        private static void TestFareCalculation()
        {
            // Logic tương tự để test giá tiền...
            // Ví dụ: if (fare == 50000) { ... }
        }
    }
}