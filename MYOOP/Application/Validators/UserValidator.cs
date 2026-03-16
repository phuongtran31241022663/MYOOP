﻿using OOP.Domain.Entities;

namespace OOP.Application.Validators
{
    public static class UserValidator
    {
        // Gọi khi đăng ký hành khách mới
        public static void ValidatePassenger(Passenger passenger)
        {
            if (passenger == null)
                throw new ArgumentNullException(nameof(passenger));

            ValidateName(passenger.Name);
            ValidatePhone(passenger.Phone);

            if (!passenger.IsActive)
                throw new InvalidOperationException("Tài khoản hành khách đã bị vô hiệu hóa.");
        }

        // Gọi khi đăng ký tài xế mới
        public static void ValidateDriver(Driver driver, string licenseNumber)
        {
            if (driver == null)
                throw new ArgumentNullException(nameof(driver));

            ValidateName(driver.Name);
            ValidatePhone(driver.Phone);

            if (driver.Vehicle == null)
                throw new ArgumentException("Tài xế phải đăng ký xe.");

            if (driver.CurrentLocation == null)
                throw new ArgumentException("Tài xế phải có vị trí hiện tại.");

            if (!driver.IsActive)
                throw new InvalidOperationException("Tài khoản tài xế đã bị vô hiệu hóa.");
            if (string.IsNullOrWhiteSpace(licenseNumber))
                throw new ArgumentException("Số giấy phép lái xe không được để trống.");
            if (driver.Vehicle != null)
                ValidateVehicle(driver.Vehicle);
        }
        public static void ValidateVehicle(Vehicle vehicle)
        {
            if (vehicle == null) throw new ArgumentNullException(nameof(vehicle));

            // Kiểm tra Driver
            if (vehicle.DriverId == Guid.Empty)
                throw new ArgumentException("Xe phải thuộc về một tài xế hợp lệ.");

            // Kiểm tra Biển số
            if (string.IsNullOrWhiteSpace(vehicle.PlateNumber))
                throw new ArgumentException("Biển số xe không được để trống.");

            // Ví dụ: Check định dạng biển số VN cơ bản (Ít nhất 7 ký tự)
            if (vehicle.PlateNumber.Length < 7)
                throw new ArgumentException("Biển số xe không đúng định dạng.");

            // Kiểm tra thông tin chung
            if (string.IsNullOrWhiteSpace(vehicle.Brand))
                throw new ArgumentException("Hãng xe không được để trống.");

            if (string.IsNullOrWhiteSpace(vehicle.Model))
                throw new ArgumentException("Mẫu xe không được để trống.");

            // Kiểm tra số chỗ ngồi theo từng loại xe cụ thể
            if (vehicle is Car car)
            {
                if (car.Capacity < 2 || car.Capacity > 7)
                    throw new ArgumentException("Ô tô phải có từ 2 đến 7 chỗ.");
            }
            else if (vehicle is Motorbike mb)
            {
                if (mb.Capacity != 2)
                    throw new ArgumentException("Xe máy mặc định phải có 2 chỗ.");
            }
        }
        // Gọi khi đăng nhập — kiểm tra input trước khi query DB
        public static void ValidateLogin(string phone, string password)
        {
            if (string.IsNullOrWhiteSpace(phone))
                throw new ArgumentException("Số điện thoại không được để trống.");

            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Mật khẩu không được để trống.");

            ValidatePhone(phone);
            // Không validate độ phức tạp mật khẩu ở login — chỉ kiểm tra không rỗng
        }

        // Gọi khi cập nhật thông tin cá nhân
        public static void ValidateUserUpdate(User user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            if (!user.IsActive)
                throw new InvalidOperationException("Không thể cập nhật tài khoản đã bị vô hiệu hóa.");

            ValidateName(user.Name);
            ValidatePhone(user.Phone);
        }

        // Validate số điện thoại — dùng độc lập hoặc gọi từ các method trên
        public static void ValidatePhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                throw new ArgumentException("Số điện thoại không được để trống.");

            var digits = phone.Replace(" ", "").Replace("+", "").Replace("-", "");

            if (!digits.All(char.IsDigit))
                throw new ArgumentException("Số điện thoại chỉ được chứa chữ số.");

            // Kiểm tra đầu số Việt Nam (thường bắt đầu bằng 0)
            if (!digits.StartsWith("0"))
                throw new ArgumentException("Số điện thoại không hợp lệ (phải bắt đầu bằng số 0).");

            if (digits.Length != 10) // Hiện tại Việt Nam đã chuyển hết về 10 số
                throw new ArgumentException("Số điện thoại phải có đúng 10 chữ số.");
        }

        // Validate mật khẩu khi đăng ký hoặc đổi mật khẩu
        public static void ValidatePassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Mật khẩu không được để trống.");

            if (password.Length < 6)
                throw new ArgumentException("Mật khẩu phải có ít nhất 6 ký tự.");
        }

        // --- Private helpers ---

        private static void ValidateName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Họ tên không được để trống.");
        }
    }
}