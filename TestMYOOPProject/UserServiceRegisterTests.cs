using Moq;
using OOP.Application.Services;
using OOP.Domain.Entities;
using OOP.Domain.Enums;
using OOP.Domain.Interfaces;

namespace TestMYOOPProject
{
    /// <summary>
    /// Integration Tests: UserService - đăng ký hành khách, tài xế, đổi thông tin
    /// </summary>
    public class UserServiceRegisterTests
    {
        private readonly Mock<IUserRepository> _mockRepo;
        private readonly UserService _userService;

        public UserServiceRegisterTests()
        {
            _mockRepo = new Mock<IUserRepository>();
            _userService = new UserService(_mockRepo.Object);
        }

        #region RegisterPassenger Tests
        [Fact]
        public async Task RegisterPassenger_ValidData_ShouldReturnPassenger()
        {
            // Arrange
            _mockRepo.Setup(r => r.ExistsByPhone(It.IsAny<string>())).ReturnsAsync(false);
            _mockRepo.Setup(r => r.Add(It.IsAny<User>())).Returns(Task.CompletedTask);

            // Act
            var passenger = await _userService.RegisterPassenger("Nguyen Van A", "0912345678", "password123");

            // Assert
            Assert.NotNull(passenger);
            Assert.Equal("Nguyen Van A", passenger.Name);
            Assert.Equal("0912345678", passenger.Phone);
            Assert.Equal(UserRole.Passenger, passenger.Role);
            Assert.True(passenger.IsActive);
        }

        [Fact]
        public async Task RegisterPassenger_ShouldCallRepoAdd()
        {
            // Arrange
            _mockRepo.Setup(r => r.ExistsByPhone(It.IsAny<string>())).ReturnsAsync(false);
            _mockRepo.Setup(r => r.Add(It.IsAny<User>())).Returns(Task.CompletedTask);

            // Act
            await _userService.RegisterPassenger("Nguyen Van A", "0912345678", "password123");

            // Assert
            _mockRepo.Verify(r => r.Add(It.IsAny<User>()), Times.Once);
        }

        [Fact]
        public async Task RegisterPassenger_DuplicatePhone_ShouldThrowInvalidOperationException()
        {
            // Arrange
            _mockRepo.Setup(r => r.ExistsByPhone("0912345678")).ReturnsAsync(true);

            // Act
            var act = () => _userService.RegisterPassenger("Nguyen Van A", "0912345678", "password123");

            // Assert
            await Assert.ThrowsAsync<InvalidOperationException>(act);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task RegisterPassenger_EmptyName_ShouldThrowArgumentException(string? name)
        {
            // Act
            var act = () => _userService.RegisterPassenger(name!, "0912345678", "password123");

            // Assert
            await Assert.ThrowsAsync<ArgumentException>(act);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task RegisterPassenger_EmptyPhone_ShouldThrowArgumentException(string? phone)
        {
            // Act
            var act = () => _userService.RegisterPassenger("Nguyen Van A", phone!, "password123");

            // Assert
            await Assert.ThrowsAsync<ArgumentException>(act);
        }

        [Theory]
        [InlineData("1912345678")]  // doesn't start with 0
        [InlineData("091234567")]   // 9 digits
        [InlineData("09123456789")] // 11 digits
        public async Task RegisterPassenger_InvalidPhone_ShouldThrowArgumentException(string phone)
        {
            // Act
            var act = () => _userService.RegisterPassenger("Nguyen Van A", phone, "password123");

            // Assert
            await Assert.ThrowsAsync<ArgumentException>(act);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task RegisterPassenger_EmptyPassword_ShouldThrowArgumentException(string? password)
        {
            // Act
            var act = () => _userService.RegisterPassenger("Nguyen Van A", "0912345678", password!);

            // Assert
            await Assert.ThrowsAsync<ArgumentException>(act);
        }

        [Theory]
        [InlineData("12345")]   // 5 chars
        [InlineData("abc")]     // 3 chars
        public async Task RegisterPassenger_ShortPassword_ShouldThrowArgumentException(string password)
        {
            // Act
            var act = () => _userService.RegisterPassenger("Nguyen Van A", "0912345678", password);

            // Assert
            await Assert.ThrowsAsync<ArgumentException>(act);
        }

        [Fact]
        public async Task RegisterPassenger_NameWithWhitespace_ShouldBeTrimmed()
        {
            // Arrange
            _mockRepo.Setup(r => r.ExistsByPhone(It.IsAny<string>())).ReturnsAsync(false);
            _mockRepo.Setup(r => r.Add(It.IsAny<User>())).Returns(Task.CompletedTask);

            // Act
            var passenger = await _userService.RegisterPassenger("  Nguyen Van A  ", "0912345678", "password123");

            // Assert
            Assert.Equal("Nguyen Van A", passenger.Name);
        }
        #endregion

        #region RegisterDriver Tests
        [Fact]
        public async Task RegisterDriver_ValidData_ShouldReturnDriver()
        {
            // Arrange
            var vehicle = new Motorbike(Guid.NewGuid(), "59A-12345", "Honda", "Wave", "Đỏ");
            var position = new GeoLocation("Quận 1", "123 Lê Lợi", 10.7769, 106.7009);

            _mockRepo.Setup(r => r.ExistsByPhone(It.IsAny<string>())).ReturnsAsync(false);
            _mockRepo.Setup(r => r.Add(It.IsAny<User>())).Returns(Task.CompletedTask);

            // Act
            var driver = await _userService.RegisterDriver(
                "Tran Van Driver", "0911111111", "driver123",
                vehicle, position, "B2-123456");

            // Assert
            Assert.NotNull(driver);
            Assert.Equal("Tran Van Driver", driver.Name);
            Assert.Equal("0911111111", driver.Phone);
            Assert.Equal(UserRole.Driver, driver.Role);
            Assert.Equal(DriverStatus.Available, driver.Status);
        }

        [Fact]
        public async Task RegisterDriver_DuplicatePhone_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var vehicle = new Motorbike(Guid.NewGuid(), "59A-12345", "Honda", "Wave", "Đỏ");
            var position = new GeoLocation("Quận 1", "123 Lê Lợi", 10.7769, 106.7009);

            _mockRepo.Setup(r => r.ExistsByPhone("0911111111")).ReturnsAsync(true);

            // Act
            var act = () => _userService.RegisterDriver(
                "Tran Van Driver", "0911111111", "driver123",
                vehicle, position, "B2-123456");

            // Assert
            await Assert.ThrowsAsync<InvalidOperationException>(act);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task RegisterDriver_EmptyName_ShouldThrowArgumentException(string? name)
        {
            var vehicle = new Motorbike(Guid.NewGuid(), "59A-12345", "Honda", "Wave", "Đỏ");
            var position = new GeoLocation("Quận 1", "123 Lê Lợi", 10.7769, 106.7009);

            var act = () => _userService.RegisterDriver(name!, "0911111111", "driver123", vehicle, position, "B2-123456");

            await Assert.ThrowsAsync<ArgumentException>(act);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task RegisterDriver_EmptyPhone_ShouldThrowArgumentException(string? phone)
        {
            var vehicle = new Motorbike(Guid.NewGuid(), "59A-12345", "Honda", "Wave", "Đỏ");
            var position = new GeoLocation("Quận 1", "123 Lê Lợi", 10.7769, 106.7009);

            var act = () => _userService.RegisterDriver("Tran Van Driver", phone!, "driver123", vehicle, position, "B2-123456");

            await Assert.ThrowsAsync<ArgumentException>(act);
        }

        [Fact]
        public async Task RegisterDriver_NullPosition_ShouldThrowArgumentException()
        {
            var vehicle = new Motorbike(Guid.NewGuid(), "59A-12345", "Honda", "Wave", "Đỏ");

            var act = () => _userService.RegisterDriver(
                "Tran Van Driver", "0911111111", "driver123",
                vehicle, null!, "B2-123456");

            await Assert.ThrowsAsync<ArgumentException>(act);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task RegisterDriver_EmptyLicense_ShouldThrowArgumentException(string? license)
        {
            var vehicle = new Motorbike(Guid.NewGuid(), "59A-12345", "Honda", "Wave", "Đỏ");
            var position = new GeoLocation("Quận 1", "123 Lê Lợi", 10.7769, 106.7009);

            var act = () => _userService.RegisterDriver(
                "Tran Van Driver", "0911111111", "driver123",
                vehicle, position, license!);

            await Assert.ThrowsAsync<ArgumentException>(act);
        }
        #endregion

        #region ResetPassword Tests
        [Fact]
        public async Task ResetPassword_ValidData_ShouldSucceed()
        {
            // Arrange
            var passenger = new Passenger(Guid.NewGuid(), "Nguyen Van A", "0912345678", "oldpassword", true);
            _mockRepo.Setup(r => r.GetById(passenger.Id)).ReturnsAsync(passenger);
            _mockRepo.Setup(r => r.Update(It.IsAny<User>())).Returns(Task.CompletedTask);

            // Act
            await _userService.ResetPassword(passenger.Id, "oldpassword", "newpassword123");

            // Assert
            Assert.True(passenger.VerifyPassword("newpassword123"));
        }

        [Fact]
        public async Task ResetPassword_UserNotFound_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _mockRepo.Setup(r => r.GetById(userId)).ReturnsAsync((User?)null);

            // Act
            var act = () => _userService.ResetPassword(userId, "oldpassword", "newpassword123");

            // Assert
            await Assert.ThrowsAsync<InvalidOperationException>(act);
        }

        [Fact]
        public async Task ResetPassword_WrongOldPassword_ShouldThrowUnauthorizedAccessException()
        {
            // Arrange
            var passenger = new Passenger(Guid.NewGuid(), "Nguyen Van A", "0912345678", "correctpassword", true);
            _mockRepo.Setup(r => r.GetById(passenger.Id)).ReturnsAsync(passenger);

            // Act
            var act = () => _userService.ResetPassword(passenger.Id, "wrongpassword", "newpassword123");

            // Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(act);
        }
        #endregion

        #region UpdateProfileName Tests
        [Fact]
        public async Task UpdateProfileName_ValidName_ShouldUpdateName()
        {
            // Arrange
            var passenger = new Passenger(Guid.NewGuid(), "Nguyen Van A", "0912345678", "password123", true);
            _mockRepo.Setup(r => r.GetById(passenger.Id)).ReturnsAsync(passenger);
            _mockRepo.Setup(r => r.Update(It.IsAny<User>())).Returns(Task.CompletedTask);

            // Act
            await _userService.UpdateProfileName(passenger.Id, "Nguyen Van B");

            // Assert
            Assert.Equal("Nguyen Van B", passenger.Name);
        }

        [Fact]
        public async Task UpdateProfileName_SameName_ShouldNotCallUpdate()
        {
            // Arrange
            var passenger = new Passenger(Guid.NewGuid(), "Nguyen Van A", "0912345678", "password123", true);
            _mockRepo.Setup(r => r.GetById(passenger.Id)).ReturnsAsync(passenger);

            // Act
            await _userService.UpdateProfileName(passenger.Id, "Nguyen Van A"); // same name

            // Assert
            _mockRepo.Verify(r => r.Update(It.IsAny<User>()), Times.Never);
        }
        #endregion

        #region ChangePhone Tests
        [Fact]
        public async Task ChangePhone_ValidNewPhone_ShouldUpdatePhone()
        {
            // Arrange
            var passenger = new Passenger(Guid.NewGuid(), "Nguyen Van A", "0912345678", "password123", true);
            _mockRepo.Setup(r => r.GetById(passenger.Id)).ReturnsAsync(passenger);
            _mockRepo.Setup(r => r.ExistsByPhone("0999999999")).ReturnsAsync(false);
            _mockRepo.Setup(r => r.Update(It.IsAny<User>())).Returns(Task.CompletedTask);

            // Act
            await _userService.ChangePhone(passenger.Id, "0999999999");

            // Assert
            Assert.Equal("0999999999", passenger.Phone);
        }

        [Fact]
        public async Task ChangePhone_SamePhone_ShouldNotCallUpdate()
        {
            // Arrange
            var passenger = new Passenger(Guid.NewGuid(), "Nguyen Van A", "0912345678", "password123", true);
            _mockRepo.Setup(r => r.GetById(passenger.Id)).ReturnsAsync(passenger);

            // Act
            await _userService.ChangePhone(passenger.Id, "0912345678"); // same phone

            // Assert
            _mockRepo.Verify(r => r.Update(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task ChangePhone_DuplicatePhone_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var passenger = new Passenger(Guid.NewGuid(), "Nguyen Van A", "0912345678", "password123", true);
            _mockRepo.Setup(r => r.GetById(passenger.Id)).ReturnsAsync(passenger);
            _mockRepo.Setup(r => r.ExistsByPhone("0999999999")).ReturnsAsync(true);

            // Act
            var act = () => _userService.ChangePhone(passenger.Id, "0999999999");

            // Assert
            await Assert.ThrowsAsync<InvalidOperationException>(act);
        }
        #endregion

        #region GetUserProfile Tests
        [Fact]
        public async Task GetUserProfile_ExistingUser_ShouldReturnUser()
        {
            // Arrange
            var passenger = new Passenger(Guid.NewGuid(), "Nguyen Van A", "0912345678", "password123", true);
            _mockRepo.Setup(r => r.GetById(passenger.Id)).ReturnsAsync(passenger);

            // Act
            var result = await _userService.GetUserProfile(passenger.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(passenger.Id, result.Id);
        }

        [Fact]
        public async Task GetUserProfile_NonExistingUser_ShouldReturnNull()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _mockRepo.Setup(r => r.GetById(userId)).ReturnsAsync((User?)null);

            // Act
            var result = await _userService.GetUserProfile(userId);

            // Assert
            Assert.Null(result);
        }
        #endregion

        #region UpdateDriverVehicle Tests
        [Fact]
        public async Task UpdateDriverVehicle_ValidVehicle_ShouldSucceed()
        {
            // Arrange
            var vehicle = new Motorbike(Guid.NewGuid(), "59A-12345", "Honda", "Wave", "Đỏ");
            var position = new GeoLocation("Quận 1", "123 Lê Lợi", 10.7769, 106.7009);
            var driver = new Driver(Guid.NewGuid(), "Tran Van Driver", "0911111111", "driver123",
                true, vehicle, position, "B2-123456");

            var newVehicle = new Motorbike(Guid.NewGuid(), "59B-99999", "Yamaha", "Exciter", "Xanh");

            _mockRepo.Setup(r => r.GetById(driver.Id)).ReturnsAsync(driver);
            _mockRepo.Setup(r => r.Update(It.IsAny<User>())).Returns(Task.CompletedTask);

            // Act
            await _userService.UpdateDriverVehicle(driver.Id, newVehicle);

            // Assert
            Assert.Equal("59B-99999", driver.Vehicle.PlateNumber);
        }

        [Fact]
        public async Task UpdateDriverVehicle_NonDriverUser_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var passenger = new Passenger(Guid.NewGuid(), "Nguyen Van A", "0912345678", "password123", true);
            _mockRepo.Setup(r => r.GetById(passenger.Id)).ReturnsAsync(passenger);

            var newVehicle = new Motorbike(Guid.NewGuid(), "59B-99999", "Yamaha", "Exciter", "Xanh");

            // Act
            var act = () => _userService.UpdateDriverVehicle(passenger.Id, newVehicle);

            // Assert
            await Assert.ThrowsAsync<InvalidOperationException>(act);
        }
        #endregion

        #region UpdateDriverLicense Tests
        [Fact]
        public async Task UpdateDriverLicense_ValidLicense_ShouldSucceed()
        {
            // Arrange
            var vehicle = new Motorbike(Guid.NewGuid(), "59A-12345", "Honda", "Wave", "Đỏ");
            var position = new GeoLocation("Quận 1", "123 Lê Lợi", 10.7769, 106.7009);
            var driver = new Driver(Guid.NewGuid(), "Tran Van Driver", "0911111111", "driver123",
                true, vehicle, position, "B2-123456");

            _mockRepo.Setup(r => r.GetById(driver.Id)).ReturnsAsync(driver);
            _mockRepo.Setup(r => r.Update(It.IsAny<User>())).Returns(Task.CompletedTask);

            // Act
            await _userService.UpdateDriverLicense(driver.Id, "C1-999888");

            // Assert
            Assert.Equal("C1-999888", driver.LicenseNumber);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task UpdateDriverLicense_EmptyLicense_ShouldThrowArgumentException(string? license)
        {
            // Arrange
            var vehicle = new Motorbike(Guid.NewGuid(), "59A-12345", "Honda", "Wave", "Đỏ");
            var position = new GeoLocation("Quận 1", "123 Lê Lợi", 10.7769, 106.7009);
            var driver = new Driver(Guid.NewGuid(), "Tran Van Driver", "0911111111", "driver123",
                true, vehicle, position, "B2-123456");

            _mockRepo.Setup(r => r.GetById(driver.Id)).ReturnsAsync(driver);

            // Act
            var act = () => _userService.UpdateDriverLicense(driver.Id, license!);

            // Assert
            await Assert.ThrowsAsync<ArgumentException>(act);
        }
        #endregion
    }
}
