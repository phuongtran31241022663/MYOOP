using OOP.Domain.Entities;
using OOP.Domain.Enums;

namespace TestMYOOPProject
{
    /// <summary>
    /// Unit Tests: Test từng hàm/class - User Entity Property Validation
    /// </summary>
    public class UserEntityTests
    {
        #region Name Property Tests
        [Fact]
        public void Name_SetValidName_ShouldSucceed()
        {
            // Arrange & Act
            var passenger = new Passenger(Guid.NewGuid(), "Nguyen Van A", "0912345678", "password123", true);

            // Assert
            Assert.Equal("Nguyen Van A", passenger.Name);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Name_SetInvalidName_ShouldThrowArgumentException(string? invalidName)
        {
            // Arrange
            var act = () => new Passenger(Guid.NewGuid(), invalidName!, "0912345678", "password123", true);

            // Assert
            Assert.Throws<ArgumentException>(act);
        }
        #endregion

        #region Phone Property Tests
        [Theory]
        [InlineData("0912345678")]
        [InlineData("0123456789")]
        public void Phone_SetValidPhone_ShouldSucceed(string validPhone)
        {
            // Arrange & Act
            var passenger = new Passenger(Guid.NewGuid(), "Nguyen Van A", validPhone, "password123", true);

            // Assert
            Assert.Equal(validPhone, passenger.Phone);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Phone_SetEmptyPhone_ShouldThrowArgumentException(string? emptyPhone)
        {
            // Arrange
            var act = () => new Passenger(Guid.NewGuid(), "Nguyen Van A", emptyPhone!, "password123", true);

            // Assert
            Assert.Throws<ArgumentException>(act);
        }

        [Theory]
        [InlineData("123456789")]      // 9 digits
        [InlineData("12345678901")]    // 11 digits
        [InlineData("912345678")]      // 9 digits
        public void Phone_SetInvalidLength_ShouldThrowArgumentException(string invalidPhone)
        {
            // Arrange
            var act = () => new Passenger(Guid.NewGuid(), "Nguyen Van A", invalidPhone, "password123", true);

            // Assert
            Assert.Throws<ArgumentException>(act);
        }

        [Theory]
        [InlineData("1912345678")]      // Doesn't start with 0
        [InlineData("8912345678")]      // Doesn't start with 0
        public void Phone_SetPhoneNotStartingWithZero_ShouldThrowArgumentException(string invalidPhone)
        {
            // Arrange
            var act = () => new Passenger(Guid.NewGuid(), "Nguyen Van A", invalidPhone, "password123", true);

            // Assert
            Assert.Throws<ArgumentException>(act);
        }

        [Theory]
        [InlineData("091234567a")]      // Contains letter
        [InlineData("091234567!")]      // Contains special char
        public void Phone_SetPhoneWithNonDigits_ShouldThrowArgumentException(string invalidPhone)
        {
            // Arrange
            var act = () => new Passenger(Guid.NewGuid(), "Nguyen Van A", invalidPhone, "password123", true);

            // Assert
            Assert.Throws<ArgumentException>(act);
        }
        #endregion

        #region Password Property Tests
        [Fact]
        public void Password_SetValidPassword_ShouldSucceed()
        {
            // Arrange & Act
            var passenger = new Passenger(Guid.NewGuid(), "Nguyen Van A", "0912345678", "password123", true);

            // Assert
            Assert.NotNull(passenger.Password);
            Assert.NotEqual("password123", passenger.Password); // Should be hashed
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Password_SetEmptyPassword_ShouldThrowArgumentException(string? emptyPassword)
        {
            // Arrange
            var act = () => new Passenger(Guid.NewGuid(), "Nguyen Van A", "0912345678", emptyPassword!, true);

            // Assert
            Assert.Throws<ArgumentException>(act);
        }

        [Theory]
        [InlineData("12345")]    // 5 chars
        [InlineData("abc")]      // 3 chars
        [InlineData("pass")]    // 4 chars
        public void Password_SetShortPassword_ShouldThrowArgumentException(string shortPassword)
        {
            // Arrange
            var act = () => new Passenger(Guid.NewGuid(), "Nguyen Van A", "0912345678", shortPassword, true);

            // Assert
            Assert.Throws<ArgumentException>(act);
        }

        [Fact]
        public void Password_SetMinValidLength_ShouldSucceed()
        {
            // Arrange & Act
            var passenger = new Passenger(Guid.NewGuid(), "Nguyen Van A", "0912345678", "123456", true);

            // Assert
            Assert.NotNull(passenger.Password);
        }
        #endregion

        #region Password Change Tests
        [Fact]
        public void ChangePassword_WithCorrectOldPassword_ShouldSucceed()
        {
            // Arrange
            var passenger = new Passenger(Guid.NewGuid(), "Nguyen Van A", "0912345678", "oldpassword", true);
            var oldHash = passenger.Password;

            // Act
            passenger.ChangePassword("oldpassword", "newpassword123");

            // Assert
            Assert.NotEqual(oldHash, passenger.Password);
        }

        [Fact]
        public void ChangePassword_WithWrongOldPassword_ShouldThrowUnauthorizedAccessException()
        {
            // Arrange
            var passenger = new Passenger(Guid.NewGuid(), "Nguyen Van A", "0912345678", "oldpassword", true);

            // Act
            var act = () => passenger.ChangePassword("wrongpassword", "newpassword123");

            // Assert
            Assert.Throws<UnauthorizedAccessException>(act);
        }

        [Fact]
        public void ChangePassword_WithSameAsOld_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var passenger = new Passenger(Guid.NewGuid(), "Nguyen Van A", "0912345678", "password123", true);

            // Act
            var act = () => passenger.ChangePassword("password123", "password123");

            // Assert
            Assert.Throws<InvalidOperationException>(act);
        }

        [Fact]
        public void ChangePassword_WithShortNewPassword_ShouldThrowArgumentException()
        {
            // Arrange
            var passenger = new Passenger(Guid.NewGuid(), "Nguyen Van A", "0912345678", "password123", true);

            // Act
            var act = () => passenger.ChangePassword("password123", "123");

            // Assert
            Assert.Throws<ArgumentException>(act);
        }
        #endregion
    }
}
