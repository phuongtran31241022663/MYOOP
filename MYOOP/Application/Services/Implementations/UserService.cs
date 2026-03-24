using OOP.Domain.Entities;
using OOP.Domain.Enums;
using OOP.Domain.Interfaces;
using OOP.Domain.Validators;

namespace OOP.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepo;

        public UserService(IUserRepository userRepo)
        {
            _userRepo = userRepo ?? throw new ArgumentNullException(nameof(userRepo));
        }

        // ── 1. Đăng ký hành khách ─────────────────────────────────────────────

        public async Task<Passenger> RegisterPassenger(string name, string phone, string password)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Tên không được để trống.");

            UserValidator.NormalizePhone(phone);
            UserValidator.ValidatePassword(password);
            await EnsurePhoneNotExists(phone);

            var passenger = new Passenger(Guid.NewGuid(), name.Trim(), phone.Trim(), password, isActive: true);
            await _userRepo.Add(passenger);
            return passenger;
        }

        // ── 2. Đăng ký tài xế ────────────────────────────────────────────────

        public async Task<Driver> RegisterDriver(
            string fullname, string phone, string password,
            Vehicle vehicle, GeoLocation position, string licenseNumber)
        {
            if (string.IsNullOrWhiteSpace(fullname))
                throw new ArgumentException("Tên không được để trống.");

            UserValidator.NormalizePhone(phone);
            UserValidator.ValidatePassword(password);

            if (position == null)
                throw new ArgumentException("Tài xế phải có vị trí hiện tại.", nameof(position));
            if (string.IsNullOrWhiteSpace(licenseNumber))
                throw new ArgumentException("Số giấy phép không hợp lệ.", nameof(licenseNumber));

            await EnsurePhoneNotExists(phone);

            var driver = new Driver(
                Guid.NewGuid(), fullname.Trim(), phone.Trim(), password,
                isActive: true, vehicle, position, licenseNumber);

            await _userRepo.Add(driver);
            return driver;
        }

        // ── 3. Đăng nhập ─────────────────────────────────────────────────────

        public async Task<User> Login(string phone, string password)
        {
            UserValidator.NormalizePhone(phone);
            UserValidator.ValidatePassword(password);

            var user = await _userRepo.GetByPhone(phone.Trim())
                ?? throw new InvalidOperationException("Số điện thoại không tồn tại.");

            if (!user.VerifyPassword(password))
                throw new UnauthorizedAccessException("Mật khẩu không đúng.");

            return user;
        }

        // ── 4. Đăng xuất (stateless — không cần làm gì) ──────────────────────

        public void Logout(Guid userId) { }

        // ── 5. Profile ────────────────────────────────────────────────────────

        public async Task<User?> GetUserProfile(Guid userId) =>
            await _userRepo.GetById(userId);

        public async Task UpdateUserProfile(Guid userId, string name, string phone)
        {
            var user = await GetOrThrow(userId);
            name = name.Trim();
            phone = phone.Trim();

            if (user.Name != name)
                user.UpdateName(name);

            if (user.Phone != phone)
            {
                await EnsurePhoneNotExists(phone);
                user.UpdatePhone(phone);
            }

            await _userRepo.Update(user);
        }

        public async Task UpdateProfileName(Guid userId, string name)
        {
            var user = await GetOrThrow(userId);
            name = name.Trim();
            if (user.Name != name)
            {
                user.UpdateName(name);
                await _userRepo.Update(user);
            }
        }

        public async Task ChangePhone(Guid userId, string newPhone)
        {
            var user = await GetOrThrow(userId);
            newPhone = newPhone.Trim();
            if (user.Phone == newPhone) return;
            await EnsurePhoneNotExists(newPhone);
            user.UpdatePhone(newPhone);
            await _userRepo.Update(user);
        }

        // ── 6. Password ───────────────────────────────────────────────────────

        public async Task ChangePassword(Guid userId, string oldPassword, string newPassword)
        {
            var user = await GetOrThrow(userId);
            user.ChangePassword(oldPassword, newPassword);
            await _userRepo.Update(user);
        }

        public async Task ResetPassword(Guid userId, string oldPassword, string newPassword)
        {
            await ChangePassword(userId, oldPassword, newPassword);
        }

        // ── 7. Driver-specific ────────────────────────────────────────────────

        public async Task UpdateDriverVehicle(Guid driverId, Vehicle newVehicle)
        {
            var driver = await GetDriverOrThrow(driverId);
            driver.UpdateVehicle(newVehicle);
            await _userRepo.Update(driver);
        }

        public async Task UpdateDriverVehicleInfo(
            Guid driverId,
            string vehicleType,
            string plateNumber,
            string brand,
            string model,
            string color,
            int capacity)
        {
            var driver = await GetDriverOrThrow(driverId);

            Vehicle vehicle = vehicleType switch
            {
                "Motorbike" => new Motorbike(driver.Id, plateNumber, brand, model, color),
                "Car" => new Car(driver.Id, plateNumber, brand, model, color, capacity),
                _ => throw new InvalidOperationException($"Loại xe '{vehicleType}' không được hỗ trợ.")
            };

            driver.UpdateVehicle(vehicle);
            await _userRepo.Update(driver);
        }

        public async Task UpdateDriverLicense(Guid driverId, string newLicense)
        {
            if (string.IsNullOrWhiteSpace(newLicense))
                throw new ArgumentException("Số giấy phép không hợp lệ.", nameof(newLicense));
            var driver = await GetDriverOrThrow(driverId);
            driver.UpdateLicenseNumber(newLicense.Trim());
            await _userRepo.Update(driver);
        }

        public async Task UpdateDriverLocation(Guid driverId, GeoLocation location)
        {
            if (location == null) throw new ArgumentNullException(nameof(location));
            var driver = await GetDriverOrThrow(driverId);
            driver.UpdateLocation(location);
            await _userRepo.Update(driver);
        }

        public async Task TopUpDriverWallet(Guid driverId, decimal amount)
        {
            var driver = await GetDriverOrThrow(driverId);
            driver.TopUpWallet(amount);
            await _userRepo.Update(driver);
        }

        public async Task UpdateDriverStatus(Guid driverId, DriverStatus status)
        {
            var driver = await GetDriverOrThrow(driverId);

            switch (status)
            {
                case DriverStatus.Active:
                    driver.SetActive();
                    break;
                case DriverStatus.OnTrip:
                    driver.SetOnTrip();
                    break;
                case DriverStatus.Inactive:
                    driver.SetInactive();          
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(status),
                        $"Trạng thái '{status}' không hợp lệ.");
            }

            await _userRepo.Update(driver);
        }

        /// <summary>
        /// Force recover driver status to Active. Bypasses domain rules.
        /// Used for recovery from stale OnTrip state when trips are unexpectedly ended.
        /// </summary>
        public async Task ForceRecoverDriverStatus(Guid driverId)
        {
            var driver = await GetDriverOrThrow(driverId);
            driver.ForceSetActive();  // Bypass domain check
            await _userRepo.Update(driver);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private async Task EnsurePhoneNotExists(string phone)
        {
            if (await _userRepo.ExistsByPhone(phone))
                throw new InvalidOperationException($"Số điện thoại '{phone}' đã được đăng ký.");
        }

        private async Task<User> GetOrThrow(Guid userId) =>
            await _userRepo.GetById(userId)
            ?? throw new InvalidOperationException($"Không tìm thấy user '{userId}'.");

        private async Task<Driver> GetDriverOrThrow(Guid driverId)
        {
            var user = await GetOrThrow(driverId);
            return user as Driver
                ?? throw new InvalidOperationException("Người dùng không phải là tài xế.");
        }
    }
}
