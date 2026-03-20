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
        // --- 1. Đăng ký hành khách ---
        public async Task<Passenger> RegisterPassenger(
    string name,
    string phone,
    string password)
        {
            // Validate name
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Tên không được để trống.");

            // Use centralized validator
            UserValidator.ValidatePhone(phone);
            UserValidator.ValidatePassword(password);

            // Cross-entity validation: kiểm tra phone đã tồn tại chưa
            await EnsurePhoneNotExists(phone);

            // Business rules (phone format, password length tối thiểu) sẽ được Entity tự validate
            var passenger = new Passenger(
                Guid.NewGuid(),
                name.Trim(),
                phone.Trim(),
                password,
                true // isActive = true mặc định
            );
            await _userRepo.Add(passenger);

            return passenger;
        }

        // --- 2. Đăng ký tài xế ---
        public async Task<Driver> RegisterDriver(
       string fullname, string phone, string password,
       Vehicle vehicle, GeoLocation position, string licenseNumber)
        {
            // Service chỉ validate input cơ bản - business rules để Entity tự validate
            if (string.IsNullOrWhiteSpace(phone))
                throw new ArgumentException("Số điện thoại không được để trống.");

            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Mật khẩu không được để trống.");

            if (string.IsNullOrWhiteSpace(fullname))
                throw new ArgumentException("Tên không được để trống.");

            if (position == null)
                throw new ArgumentException("Tài xế phải có vị trí hiện tại.");

            if (string.IsNullOrWhiteSpace(licenseNumber))
                throw new ArgumentException("Số giấy phép không hợp lệ.");

            // Cross-entity validation: kiểm tra phone đã tồn tại chưa
            await EnsurePhoneNotExists(phone);

            // Business rules (phone format, password length, license format) sẽ được Entity tự validate
            var driverId = Guid.NewGuid();
            var driver = new Driver(
    driverId,
    fullname.Trim(),
    phone.Trim(),
    password,
    true, // isActive = true mặc định
    vehicle,  
    position,
    licenseNumber
);
            await _userRepo.Add(driver);
            return driver;
        }
        // --- 3. Đăng nhập ---
        public async Task<User> Login(string phone, string password)
        {
            // Use centralized validator
            UserValidator.ValidatePhone(phone);
            UserValidator.ValidatePassword(password);
            
            string trimmedPhone = phone.Trim();
            
            // Find user by phone
            var user = await _userRepo.GetByPhone(trimmedPhone);
            if (user == null)
                throw new InvalidOperationException("Số điện thoại không tồn tại.");
            
            if (!user.VerifyPassword(password))
                throw new UnauthorizedAccessException("Mật khẩu không đúng.");
            return user;
        }

        // --- 4. Đăng xuất ---
        public void Logout(Guid userId)
        {
        }

        // --- 5. Đặt lại mật khẩu ---
        public async Task ResetPassword(Guid userId, string oldPassword, string newPassword)
        {
            var user = await _userRepo.GetById(userId)
                       ?? throw new InvalidOperationException("Người dùng không tồn tại.");

            user.ChangePassword(oldPassword, newPassword);

            await _userRepo.Update(user);
        }
        // --- Profile ---

        public async Task<User?> GetUserProfile(Guid userId)
        {
            return await _userRepo.GetById(userId);
        }
        public async Task UpdateProfileName(Guid userId, string name)
        {
            var user = await GetOrThrow(userId);

            if (user.Name != name.Trim())
            {
                user.UpdateName(name.Trim());
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
        public async Task ChangePassword(Guid userId, string oldPassword, string newPassword)
        {
            var user = await GetOrThrow(userId);
            user.ChangePassword(oldPassword, newPassword);
            await _userRepo.Update(user);
        }
        public async Task UpdateDriverVehicle(Guid driverId, Vehicle newVehicle)
        {
            var user = await GetOrThrow(driverId);

            if (user is not Driver driver)
                throw new InvalidOperationException("Người dùng không phải là tài xế.");

            driver.UpdateVehicle(newVehicle);

            await _userRepo.Update(driver);
        }
        public async Task UpdateDriverLicense(Guid driverId, string newLicenseNumber)
        {
            var user = await GetOrThrow(driverId);

            if (user is not Driver driver)
                throw new InvalidOperationException("Người dùng không phải là tài xế.");

            if (string.IsNullOrWhiteSpace(newLicenseNumber))
                throw new ArgumentException("Số giấy phép không hợp lệ.");

            driver.UpdateLicenseNumber(newLicenseNumber.Trim());

            await _userRepo.Update(driver);
        }

        public async Task UpdateDriverLocation(Guid driverId, GeoLocation location)
        {
            var user = await GetOrThrow(driverId);

            if (user is not Driver driver)
                throw new InvalidOperationException("Người dùng không phải là tài xế.");

            driver.UpdateLocation(location);
            await _userRepo.Update(driver);
        }

        public async Task UpdateDriverStatus(Guid driverId, DriverStatus status)
        {
            var user = await GetOrThrow(driverId);

            if (user is not Driver driver)
                throw new InvalidOperationException("Người dùng không phải là tài xế.");

            // Sử dụng các phương thức mới với validation
            switch (status)
            {
                case DriverStatus.Available:
                    driver.SetAvailable();
                    break;
                case DriverStatus.Busy:
                    driver.SetBusy();
                    break;
                case DriverStatus.Offline:
                    // Driver không thể tự set offline - chỉ hệ thống mới được phép
                    throw new InvalidOperationException("Tài xế không thể tự ngắt kết nối. Vui lòng đóng ứng dụng.");
                default:
                    throw new ArgumentException($"Trạng thái '{status}' không hợp lệ.");
            }
            await _userRepo.Update(driver);
        }

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
        
        private async Task EnsurePhoneNotExists(string phone)
        {
            if (await _userRepo.ExistsByPhone(phone))
                throw new InvalidOperationException($"Số điện thoại '{phone}' đã được đăng ký.");
        }

        private async Task<User> GetOrThrow(Guid userId)
        {
            return await _userRepo.GetById(userId)
                   ?? throw new KeyNotFoundException($"Không tìm thấy user với Id '{userId}'.");
        }
    }
}
