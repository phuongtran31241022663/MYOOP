using OOP.Domain.Entities;
using OOP.Domain.Enums;

namespace TestMYOOPProject
{
    /// <summary>
    /// Manual Test Class - Simple smoke tests
    /// </summary>
    public static class ManualTest
    {
        public static void RunAllTests()
        {
            Console.WriteLine("--- BẮT ĐẦU KIỂM THỬ HỆ THỐNG ---");

            TestUserCreation();
            TestPasswordHashing();

            Console.WriteLine("--- KẾT THÚC KIỂM THỬ ---");
        }

        private static void TestUserCreation()
        {
            try
            {
                // 1. Arrange
                var passenger = new Passenger(
                    Guid.NewGuid(),
                    "Nguyen Van A",
                    "0912345678",
                    "password123",
                    true
                );

                // 2. Assert
                if (passenger != null && passenger.Name == "Nguyen Van A")
                {
                    Console.WriteLine("[PASS] TestUserCreation: Tạo user thành công.");
                }
                else
                {
                    Console.WriteLine("[FAIL] TestUserCreation: Tạo user thất bại.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] TestUserCreation có lỗi: {ex.Message}");
            }
        }

        private static void TestPasswordHashing()
        {
            try
            {
                // 1. Arrange
                var rawPassword = "testpassword";
                var passenger = new Passenger(
                    Guid.NewGuid(),
                    "Nguyen Van B",
                    "0922222222",
                    rawPassword,
                    true
                );

                // 2. Act & Assert
                bool verified = passenger.VerifyPassword(rawPassword);
                if (verified)
                {
                    Console.WriteLine("[PASS] TestPasswordHashing: Xác thực mật khẩu thành công.");
                }
                else
                {
                    Console.WriteLine("[FAIL] TestPasswordHashing: Xác thực mật khẩu thất bại.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] TestPasswordHashing có lỗi: {ex.Message}");
            }
        }
    }
}
