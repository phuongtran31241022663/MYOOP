using OOP.Application.Services.Interfaces;
using OOP.Application.Validators;
using OOP.Domain.Entities;
using OOP.Domain.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace OOP.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepo;

        public AuthService(IUserRepository userRepo)
        {
            _userRepo = userRepo ?? throw new ArgumentNullException(nameof(userRepo));
        }

        // --- 1. Đăng ký hành khách ---
        public async Task<Passenger> RegisterPassenger(
    string fullname,
    string phone,
    string password)
        { 
            UserValidator.ValidatePhone(phone);
            UserValidator.ValidatePassword(password);

            if (string.IsNullOrWhiteSpace(fullname))
                throw new ArgumentException("Tên không được để trống.");
            phone = phone.Trim();

            await EnsurePhoneNotExists(phone);

            var passenger = new Passenger(
                Guid.NewGuid(),
                fullname.Trim(),
                phone,
                HashPassword(password),
                true
            );

            UserValidator.ValidatePassenger(passenger);

            await _userRepo.Add(passenger);

            return passenger;
        }

        // --- 2. Đăng ký tài xế ---
       public async Task<Driver> RegisterDriver(
    string fullname,
    string phone,
    string password,
    Vehicle vehicle,
    Location location,
    string licenseNumber)
        {
            UserValidator.ValidatePhone(phone);
            UserValidator.ValidatePassword(password);

            if (string.IsNullOrWhiteSpace(fullname))
                throw new ArgumentException("Tên không được để trống.");

            if (vehicle == null)
                throw new ArgumentNullException(nameof(vehicle));

            if (location == null)
                throw new ArgumentNullException(nameof(location));

            phone = phone.Trim();

            await EnsurePhoneNotExists(phone);

            var driver = new Driver(
                Guid.NewGuid(),
                fullname.Trim(),
                phone,
                HashPassword(password),
                true,
                vehicle,
                location,
                licenseNumber
            );
            UserValidator.ValidateDriver(driver, licenseNumber);
            await _userRepo.Add(driver);

            return driver;
        }

        // --- 3. Đăng nhập ---
        public async Task<User> Login(string phone, string password)
        {
            UserValidator.ValidateLogin(phone, password);

            var user = await _userRepo.GetByPhone(phone.Trim())
                       ?? throw new InvalidOperationException("Số điện thoại không tồn tại.");

            if (!user.VerifyPassword(HashPassword(password)))
                throw new UnauthorizedAccessException("Mật khẩu không đúng.");

            if (!user.IsActive)
                throw new InvalidOperationException("Tài khoản đã bị khóa.");

            return user;
        }

        // --- 4. Đăng xuất ---
        public void Logout(Guid userId)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("UserId không hợp lệ.");

            // TODO: Nếu dùng JWT/session token, invalidate token tại đây.
            Console.WriteLine($"User {userId} đã đăng xuất.");
        }

        // --- 5. Đặt lại mật khẩu ---
        public async Task ResetPassword(Guid userId, string newPassword)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("UserId không hợp lệ.");

            UserValidator.ValidatePassword(newPassword);

            var user = await _userRepo.GetById(userId)
                       ?? throw new InvalidOperationException("Người dùng không tồn tại.");

            user.UpdatePassword(HashPassword(newPassword));

            await _userRepo.Update(user);
        }

        // --- Helpers ---

        public static string HashPassword(string rawPassword)
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(rawPassword);
            byte[] hashBytes = SHA256.HashData(inputBytes);
            return Convert.ToBase64String(hashBytes);
        }

        private async Task EnsurePhoneNotExists(string phone)
        {
            if (await _userRepo.ExistsByPhone(phone))
                throw new InvalidOperationException("Số điện thoại đã được đăng ký.");
        }
    }
}