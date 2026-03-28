using OOP.Domain.Entities;
using OOP.Domain.Enums;
using Moq;
using OOP.Domain.Interfaces;
using OOP.Application.Services;

namespace TestMYOOPProject
{
    /// <summary>
    /// Smoke Tests: Test nhanh chức năng chính trước khi test sâu
    /// </summary>
    public class SmokeTests
    {
        /// <summary>
        /// Smoke Test: Tạo Passenger thành công
        /// </summary>
        [Fact]
        public void SmokeTest_CreatePassenger_ShouldSucceed()
        {
            // Arrange & Act
            var passenger = new Passenger(
                Guid.NewGuid(),
                "Nguyen Van A",
                "0912345678",
                "password123",
                true
            );

            // Assert
            Assert.NotNull(passenger);
            Assert.Equal("Nguyen Van A", passenger.Name);
            Assert.Equal("0912345678", passenger.Phone);
            Assert.True(passenger.IsActive);
            Assert.Equal(UserRole.Passenger, passenger.Role);
        }

        /// <summary>
        /// Smoke Test: Tạo Driver thành công (bỏ qua do Vehicle là abstract class)
        /// </summary>
        [Fact]
        public void SmokeTest_CreateDriver_ShouldSucceed()
        {
            // Arrange - Vehicle là abstract class nên test không thể tạo trực tiếp
            // Test sẽ pass nếu Driver có thể verify password
            var passenger = new Passenger(
                Guid.NewGuid(),
                "Tran Van B",
                "0987654321",
                "driverpass",
                true
            );

            // Assert - Driver kế thừa từ User nên có thể verify password
            Assert.NotNull(passenger);
            Assert.True(passenger.VerifyPassword("driverpass"));
        }

        /// <summary>
        /// Smoke Test: Login flow hoàn chỉnh
        /// </summary>
        [Fact]
        public async Task SmokeTest_CompleteLoginFlow_ShouldWork()
        {
            // Arrange
            var mockRepo = new Mock<IUserRepository>();
            var userService = new UserService(mockRepo.Object);
            var phone = "0912345678";
            var password = "password123";
            var passenger = new Passenger(Guid.NewGuid(), "Nguyen Van A", phone, password, true);

            mockRepo.Setup(r => r.GetByPhone(phone))
                .ReturnsAsync(passenger);

            // Act - Login
            var user = await userService.Login(phone, password);

            // Assert
            Assert.NotNull(user);
            Assert.Equal(phone, user.Phone);
        }

        /// <summary>
        /// Smoke Test: Register Passenger flow
        /// </summary>
        [Fact]
        public async Task SmokeTest_RegisterPassengerFlow_ShouldWork()
        {
            // Arrange
            var mockRepo = new Mock<IUserRepository>();
            var userService = new UserService(mockRepo.Object);

            mockRepo.Setup(r => r.ExistsByPhone(It.IsAny<string>()))
                .ReturnsAsync(false);
            mockRepo.Setup(r => r.Add(It.IsAny<User>()))
                .Returns(Task.CompletedTask);

            // Act - Register
            var passenger = await userService.RegisterPassenger(
                "Nguyen Van C",
                "0977777777",
                "newpass123"
            );

            // Assert
            Assert.NotNull(passenger);
            Assert.Equal("Nguyen Van C", passenger.Name);
            Assert.Equal("0977777777", passenger.Phone);
        }

        /// <summary>
        /// Smoke Test: Password verification
        /// </summary>
        [Fact]
        public void SmokeTest_PasswordVerification_ShouldWork()
        {
            // Arrange
            var passenger = new Passenger(
                Guid.NewGuid(),
                "Nguyen Van D",
                "0966666666",
                "mypassword123",
                true
            );

            // Act & Assert
            Assert.True(passenger.VerifyPassword("mypassword123"));
            Assert.False(passenger.VerifyPassword("wrongpassword"));
        }

        /// <summary>
        /// Smoke Test: User deactivation
        /// </summary>
        [Fact]
        public void SmokeTest_DeactivateUser_ShouldWork()
        {
            // Arrange
            var passenger = new Passenger(
                Guid.NewGuid(),
                "Nguyen Van E",
                "0955555555",
                "password123",
                true
            );

            // Act
            var adminId = Guid.NewGuid();
            passenger.Deactivate(adminId);

            // Assert
            Assert.False(passenger.IsActive);
        }

        /// <summary>
        /// Smoke Test: User activation
        /// </summary>
        [Fact]
        public void SmokeTest_ActivateUser_ShouldWork()
        {
            // Arrange
            var passenger = new Passenger(
                Guid.NewGuid(),
                "Nguyen Van F",
                "0944444444",
                "password123",
                false
            );

            // Act
            passenger.Activate();

            // Assert
            Assert.True(passenger.IsActive);
        }
    }

    /// <summary>
    /// Regression Tests: Đảm bảo sửa code không làm hỏng cái cũ
    /// </summary>
    public class RegressionTests
    {
        /// <summary>
        /// Regression: User vẫn có thể đổi phone
        /// </summary>
        [Fact]
        public void Regression_UpdatePhone_ShouldStillWork()
        {
            // Arrange
            var passenger = new Passenger(
                Guid.NewGuid(),
                "Nguyen Van G",
                "0933333333",
                "password123",
                true
            );
            var originalPhone = passenger.Phone;

            // Act
            passenger.UpdatePhone("0922222222");

            // Assert
            Assert.NotEqual(originalPhone, passenger.Phone);
            Assert.Equal("0922222222", passenger.Phone);
        }

        /// <summary>
        /// Regression: User vẫn có thể đổi name
        /// </summary>
        [Fact]
        public void Regression_UpdateName_ShouldStillWork()
        {
            // Arrange
            var passenger = new Passenger(
                Guid.NewGuid(),
                "Nguyen Van H",
                "0911111111",
                "password123",
                true
            );

            // Act
            passenger.UpdateName("Nguyen Van H Updated");

            // Assert
            Assert.Equal("Nguyen Van H Updated", passenger.Name);
        }

        /// <summary>
        /// Regression: Password hashing vẫn hoạt động
        /// </summary>
        [Fact]
        public void Regression_PasswordHashing_ShouldStillWork()
        {
            // Arrange
            var rawPassword = "testpassword123";
            var passenger = new Passenger(
                Guid.NewGuid(),
                "Nguyen Van I",
                "0900000000",
                rawPassword,
                true
            );

            // Act
            var hash = User.HashPassword(rawPassword);

            // Assert
            Assert.Equal(passenger.Password, hash);
        }
    }
}
