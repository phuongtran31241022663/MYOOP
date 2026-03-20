using Moq;
using OOP.Domain.Entities;
using OOP.Domain.Enums;
using OOP.Domain.Interfaces;
using OOP.Application.Services;

namespace TestMYOOPProject
{
    /// <summary>
    /// Integration Tests: Test giữa các module - UserService.Login() validation
    /// </summary>
    public class UserServiceLoginTests
    {
        private readonly UserService _userService;
        private readonly Mock<IUserRepository> _mockRepo;

        public UserServiceLoginTests()
        {
            _mockRepo = new Mock<IUserRepository>();
            _userService = new UserService(_mockRepo.Object);
        }

        #region Phone Validation Tests
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Login_WithEmptyPhone_ShouldThrowArgumentException(string? emptyPhone)
        {
            // Arrange & Act
            var act = () => _userService.Login(emptyPhone!, "password123");

            // Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(act);
            Assert.Contains("điện thoại", ex.Message);
            Assert.Contains("không được để trống", ex.Message);
        }

        [Theory]
        [InlineData("091234567a")]
        [InlineData("091234567!")]
        public async Task Login_WithPhoneContainingLetters_ShouldThrowArgumentException(string invalidPhone)
        {
            // Arrange & Act
            var act = () => _userService.Login(invalidPhone, "password123");

            // Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(act);
            Assert.Contains("chỉ được chứa chữ số", ex.Message);
        }

        [Theory]
        [InlineData("1912345678")]
        [InlineData("8912345678")]
        public async Task Login_WithPhoneNotStartingWithZero_ShouldThrowArgumentException(string invalidPhone)
        {
            // Arrange & Act
            var act = () => _userService.Login(invalidPhone, "password123");

            // Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(act);
            Assert.Contains("bắt đầu bằng 0", ex.Message);
        }

        [Theory]
        [InlineData("091234567")]   // 9 digits
        [InlineData("09123456789")] // 11 digits
        public async Task Login_WithInvalidPhoneLength_ShouldThrowArgumentException(string invalidPhone)
        {
            // Arrange & Act
            var act = () => _userService.Login(invalidPhone, "password123");

            // Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(act);
            Assert.Contains("10 chữ số", ex.Message);
        }
        #endregion

        #region Password Validation Tests
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Login_WithEmptyPassword_ShouldThrowArgumentException(string? emptyPassword)
        {
            // Arrange & Act
            var act = () => _userService.Login("0912345678", emptyPassword!);

            // Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(act);
            Assert.Contains("Mật khẩu", ex.Message);
            Assert.Contains("không được để trống", ex.Message);
        }

        [Theory]
        [InlineData("12345")]    // 5 chars
        [InlineData("abc")]     // 3 chars
        [InlineData("pass")]    // 4 chars
        public async Task Login_WithShortPassword_ShouldThrowArgumentException(string shortPassword)
        {
            // Arrange & Act
            var act = () => _userService.Login("0912345678", shortPassword);

            // Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(act);
            Assert.Contains("ít nhất 6 ký tự", ex.Message);
        }
        #endregion

        #region Valid Input Tests (Integration with Repository)
        [Fact]
        public async Task Login_WithValidCredentials_ShouldReturnUser()
        {
            // Arrange
            var phone = "0912345678";
            var password = "password123";
            var passenger = new Passenger(Guid.NewGuid(), "Nguyen Van A", phone, password, true);
            
            _mockRepo.Setup(r => r.GetByPhone(phone))
                .ReturnsAsync(passenger);

            // Act
            var result = await _userService.Login(phone, password);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(phone, result.Phone);
            Assert.Equal("Nguyen Van A", result.Name);
        }

        [Fact]
        public async Task Login_WithNonExistentPhone_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var phone = "0999999999";
            _mockRepo.Setup(r => r.GetByPhone(phone))
                .ReturnsAsync((User?)null);

            // Act
            var act = () => _userService.Login(phone, "password123");

            // Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(act);
            Assert.Contains("không tồn tại", ex.Message);
        }

        [Fact]
        public async Task Login_WithWrongPassword_ShouldThrowUnauthorizedAccessException()
        {
            // Arrange
            var phone = "0912345678";
            var correctPassword = "correctpassword";
            var wrongPassword = "wrongpassword";
            var passenger = new Passenger(Guid.NewGuid(), "Nguyen Van A", phone, correctPassword, true);
            
            _mockRepo.Setup(r => r.GetByPhone(phone))
                .ReturnsAsync(passenger);

            // Act
            var act = () => _userService.Login(phone, wrongPassword);

            // Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(act);
        }

        [Fact]
        public async Task Login_WithTrimmedPhone_ShouldWork()
        {
            // Arrange
            var phone = "0912345678";
            var trimmedPhone = "  0912345678  ";
            var password = "password123";
            var passenger = new Passenger(Guid.NewGuid(), "Nguyen Van A", phone, password, true);
            
            _mockRepo.Setup(r => r.GetByPhone(phone))
                .ReturnsAsync(passenger);

            // Act
            var result = await _userService.Login(trimmedPhone, password);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(phone, result.Phone);
        }
        #endregion
    }
}
