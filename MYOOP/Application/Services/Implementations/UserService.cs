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

        // -- 1. �ang k� h�nh kh�ch ---------------------------------------------

        public async Task<Passenger> RegisterPassenger(string name, string phone, string password)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("T�n kh�ng du?c d? tr?ng.");

            DomainValidators.UserValidator.NormalizePhone(phone);
            DomainValidators.UserValidator.ValidatePassword(password);
            await EnsurePhoneNotExists(phone);

            var passenger = new Passenger(Guid.NewGuid(), name.Trim(), phone.Trim(), password, isActive: true);
            await _userRepo.Add(passenger);
            return passenger;
        }

        // -- 2. �ang k� t�i x? ------------------------------------------------

        public async Task<Driver> RegisterDriver(
            string fullname, string phone, string password,
            Vehicle vehicle, GeoLocation position, string licenseNumber)
        {
            if (string.IsNullOrWhiteSpace(fullname))
                throw new ArgumentException("T�n kh�ng du?c d? tr?ng.");

            DomainValidators.UserValidator.NormalizePhone(phone);
            DomainValidators.UserValidator.ValidatePassword(password);

            if (position == null)
                throw new ArgumentException("T�i x? ph?i c� v? tr� hi?n t?i.", nameof(position));
            if (string.IsNullOrWhiteSpace(licenseNumber))
                throw new ArgumentException("S? gi?y ph�p kh�ng h?p l?.", nameof(licenseNumber));

            await EnsurePhoneNotExists(phone);

            // Validate vehicle data
            var vehicleValidator = new DomainValidators.VehicleValidator();
            var vehicleErrors = vehicleValidator.Validate(vehicle.PlateNumber, vehicle.Brand, vehicle.Model, vehicle.Color, vehicle.Capacity, vehicle.IsCar());
            if (vehicleErrors.Any())
                throw new ArgumentException($"D? li?u xe kh�ng h?p l?: {string.Join(", ", vehicleErrors)}");

            var driver = new Driver(
                Guid.NewGuid(), fullname.Trim(), phone.Trim(), password,
                isActive: true, vehicle, position, licenseNumber);

            await _userRepo.Add(driver);
            return driver;
        }

        // -- 3. �ang nh?p -----------------------------------------------------

        public async Task<User> Login(string phone, string password)
        {
            DomainValidators.UserValidator.NormalizePhone(phone);
            DomainValidators.UserValidator.ValidatePassword(password);

            var user = await _userRepo.GetByPhone(phone.Trim())
                ?? throw new InvalidOperationException("S? di?n tho?i kh�ng t?n t?i.");

            if (!user.VerifyPassword(password))
                throw new UnauthorizedAccessException("M?t kh?u kh�ng d�ng.");

            return user;
        }

        // -- 4. �ang xu?t (stateless � kh�ng c?n l�m g�) ----------------------

        public void Logout(Guid userId) { }

        // -- 5. Profile --------------------------------------------------------

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

        // -- 6. Password -------------------------------------------------------

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

        // -- 7. Driver-specific ------------------------------------------------

        public async Task UpdateDriverVehicle(Guid driverId, Vehicle newVehicle)
        {
            var driver = await GetDriverOrThrow(driverId);

            // Validate vehicle data
            var vehicleValidator = new DomainValidators.VehicleValidator();
            var vehicleErrors = vehicleValidator.Validate(newVehicle.PlateNumber, newVehicle.Brand, newVehicle.Model, newVehicle.Color, newVehicle.Capacity, newVehicle.IsCar());
            if (vehicleErrors.Any())
                throw new ArgumentException($"Dữ liệu xe không hợp lệ: {string.Join(", ", vehicleErrors)}");

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

            // Validate vehicle data
            var vehicleValidator = new DomainValidators.VehicleValidator();
            var vehicleErrors = vehicleValidator.Validate(plateNumber, brand, model, color, capacity, vehicleType == "Car");
            if (vehicleErrors.Any())
                throw new ArgumentException($"Dữ liệu xe không hợp lệ: {string.Join(", ", vehicleErrors)}");

            Vehicle vehicle = vehicleType switch
            {
                "Motorbike" => new Motorbike(driver.Id, plateNumber, brand, model, color),
                "Car" => new Car(driver.Id, plateNumber, brand, model, color, capacity),
                _ => throw new InvalidOperationException($"Lo?i xe '{vehicleType}' kh�ng du?c h? tr?.")
            };

            driver.UpdateVehicle(vehicle);
            await _userRepo.Update(driver);
        }

        public async Task UpdateDriverLicense(Guid driverId, string newLicense)
        {
            if (string.IsNullOrWhiteSpace(newLicense))
                throw new ArgumentException("S? gi?y ph�p kh�ng h?p l?.", nameof(newLicense));
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
                case DriverStatus.Available:
                    driver.SetAvailable();
                    break;
                case DriverStatus.OnTrip:
                    driver.SetOnTrip();
                    break;
                case DriverStatus.Offline:
                    driver.SetOffline();          
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(status),
                        $"Tr?ng th�i '{status}' kh�ng h?p l?.");
            }

            await _userRepo.Update(driver);
        }

        /// <summary>
        /// Force recover driver status to Active (bypass status transition guard).
        /// Used for recovery from stale OnTrip state when trips are unexpectedly ended.
        /// </summary>
        public async Task ForceRecoverDriverStatus(Guid driverId)
        {
            var driver = await GetDriverOrThrow(driverId);
            driver.ForceSetAvailable();
            await _userRepo.Update(driver);
        }

        // -- Helpers -----------------------------------------------------------

        private async Task EnsurePhoneNotExists(string phone)
        {
            if (await _userRepo.ExistsByPhone(phone))
                throw new InvalidOperationException($"S? di?n tho?i '{phone}' d� du?c dang k�.");
        }

        private async Task<User> GetOrThrow(Guid userId) =>
            await _userRepo.GetById(userId)
            ?? throw new InvalidOperationException($"Kh�ng t�m th?y user '{userId}'.");

        private async Task<Driver> GetDriverOrThrow(Guid driverId)
        {
            var user = await GetOrThrow(driverId);
            return user as Driver
                ?? throw new InvalidOperationException("Ngu?i d�ng kh�ng ph?i l� t�i x?.");
        }
    }
}
