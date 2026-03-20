using OOP.Domain.Entities;
using OOP.Domain.Enums;

namespace TestMYOOPProject
{
    /// <summary>
    /// Unit Tests: Driver entity - trạng thái, ví, đánh giá, xe
    /// </summary>
    public class DriverEntityTests
    {
        #region Helpers
        private static Driver CreateDriver(
            string name = "Tran Van Driver",
            string phone = "0911111111",
            string password = "driver123",
            DriverStatus initialStatus = DriverStatus.Available)
        {
            var vehicle = new Motorbike(Guid.NewGuid(), "59A-12345", "Honda", "Wave", "Đỏ");
            var position = new GeoLocation("Quận 1", "123 Lê Lợi", 10.7769, 106.7009);
            var driver = new Driver(
                Guid.NewGuid(), name, phone, password, true,
                vehicle, position, "B2-123456");

            if (initialStatus == DriverStatus.Offline)
                driver.SetOffline();
            else if (initialStatus == DriverStatus.Busy)
                driver.SetBusy();

            return driver;
        }
        #endregion

        #region Constructor Tests
        [Fact]
        public void Constructor_ValidData_ShouldCreateDriver()
        {
            // Arrange
            var vehicle = new Motorbike(Guid.NewGuid(), "59A-12345", "Honda", "Wave", "Đỏ");
            var position = new GeoLocation("Quận 1", "123 Lê Lợi", 10.7769, 106.7009);

            // Act
            var driver = new Driver(
                Guid.NewGuid(), "Tran Van A", "0911111111", "password123",
                true, vehicle, position, "B2-123456");

            // Assert
            Assert.NotNull(driver);
            Assert.Equal("Tran Van A", driver.Name);
            Assert.Equal("0911111111", driver.Phone);
            Assert.Equal(DriverStatus.Available, driver.Status);
            Assert.Equal(UserRole.Driver, driver.Role);
            Assert.Equal(0, driver.TotalTrips);
            Assert.Equal(0m, driver.Wallet);
            Assert.Equal(0m, driver.Income);
            Assert.Equal(5.0m, driver.AverageRating);
        }

        [Fact]
        public void Constructor_NullVehicle_ShouldThrowArgumentNullException()
        {
            // Arrange
            var position = new GeoLocation("Quận 1", "123 Lê Lợi", 10.7769, 106.7009);

            // Act
            var act = () => new Driver(
                Guid.NewGuid(), "Tran Van A", "0911111111", "password123",
                true, null!, position, "B2-123456");

            // Assert
            Assert.Throws<ArgumentNullException>(act);
        }

        [Fact]
        public void Constructor_NullPosition_ShouldThrowArgumentNullException()
        {
            // Arrange
            var vehicle = new Motorbike(Guid.NewGuid(), "59A-12345", "Honda", "Wave", "Đỏ");

            // Act
            var act = () => new Driver(
                Guid.NewGuid(), "Tran Van A", "0911111111", "password123",
                true, vehicle, null!, "B2-123456");

            // Assert
            Assert.Throws<ArgumentNullException>(act);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_InvalidLicenseNumber_ShouldThrowArgumentException(string? license)
        {
            // Arrange
            var vehicle = new Motorbike(Guid.NewGuid(), "59A-12345", "Honda", "Wave", "Đỏ");
            var position = new GeoLocation("Quận 1", "123 Lê Lợi", 10.7769, 106.7009);

            // Act
            var act = () => new Driver(
                Guid.NewGuid(), "Tran Van A", "0911111111", "password123",
                true, vehicle, position, license!);

            // Assert
            Assert.Throws<ArgumentException>(act);
        }
        #endregion

        #region Status Transition Tests
        [Fact]
        public void SetBusy_WhenAvailable_ShouldSucceed()
        {
            var driver = CreateDriver();
            driver.SetBusy();
            Assert.Equal(DriverStatus.Busy, driver.Status);
        }

        [Fact]
        public void SetBusy_WhenOffline_ShouldThrowInvalidOperationException()
        {
            var driver = CreateDriver(initialStatus: DriverStatus.Offline);
            var act = () => driver.SetBusy();
            Assert.Throws<InvalidOperationException>(act);
        }

        [Fact]
        public void SetBusy_WhenAlreadyBusy_ShouldThrowInvalidOperationException()
        {
            var driver = CreateDriver();
            driver.SetBusy();
            var act = () => driver.SetBusy();
            Assert.Throws<InvalidOperationException>(act);
        }

        [Fact]
        public void SetAvailable_WhenOffline_ShouldSucceed()
        {
            var driver = CreateDriver(initialStatus: DriverStatus.Offline);
            driver.SetAvailable();
            Assert.Equal(DriverStatus.Available, driver.Status);
        }

        [Fact]
        public void SetAvailable_WhenBusy_ShouldThrowInvalidOperationException()
        {
            var driver = CreateDriver();
            driver.SetBusy();
            var act = () => driver.SetAvailable();
            Assert.Throws<InvalidOperationException>(act);
        }

        [Fact]
        public void SetOffline_WhenAvailable_ShouldSucceed()
        {
            var driver = CreateDriver();
            driver.SetOffline();
            Assert.Equal(DriverStatus.Offline, driver.Status);
        }

        [Fact]
        public void SetOffline_WhenBusy_ShouldThrowInvalidOperationException()
        {
            var driver = CreateDriver();
            driver.SetBusy();
            var act = () => driver.SetOffline();
            Assert.Throws<InvalidOperationException>(act);
        }
        #endregion

        #region Deactivate Tests
        [Fact]
        public void Deactivate_WhenBusy_ShouldThrowInvalidOperationException()
        {
            var driver = CreateDriver();
            driver.SetBusy();
            var actorId = Guid.NewGuid();
            var act = () => driver.Deactivate(actorId);
            Assert.Throws<InvalidOperationException>(act);
        }

        [Fact]
        public void Deactivate_WhenAvailable_ShouldSucceed()
        {
            var driver = CreateDriver();
            var actorId = Guid.NewGuid();
            driver.Deactivate(actorId);
            Assert.False(driver.IsActive);
        }
        #endregion

        #region Wallet Tests
        [Fact]
        public void TopUpWallet_PositiveAmount_ShouldIncreaseWallet()
        {
            var driver = CreateDriver();
            driver.TopUpWallet(100_000m);
            Assert.Equal(100_000m, driver.Wallet);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1000)]
        public void TopUpWallet_NonPositiveAmount_ShouldThrowArgumentException(decimal amount)
        {
            var driver = CreateDriver();
            var act = () => driver.TopUpWallet(amount);
            Assert.Throws<ArgumentException>(act);
        }

        [Fact]
        public void PayCommission_ValidData_ShouldDeductWalletAndAddIncome()
        {
            var driver = CreateDriver();
            driver.TopUpWallet(50_000m);

            driver.PayCommission(100_000m, 20_000m);

            Assert.Equal(30_000m, driver.Wallet);   // 50000 - 20000
            Assert.Equal(80_000m, driver.Income);   // 100000 - 20000
        }

        [Fact]
        public void PayCommission_InsufficientWallet_ShouldThrowInvalidOperationException()
        {
            var driver = CreateDriver();
            // Wallet = 0, commission = 20000
            var act = () => driver.PayCommission(100_000m, 20_000m);
            Assert.Throws<InvalidOperationException>(act);
        }

        [Fact]
        public void PayCommission_NegativeFare_ShouldThrowArgumentException()
        {
            var driver = CreateDriver();
            driver.TopUpWallet(50_000m);
            var act = () => driver.PayCommission(-1m, 0m);
            Assert.Throws<ArgumentException>(act);
        }

        [Fact]
        public void PayCommission_CommissionGreaterThanFare_ShouldThrowArgumentException()
        {
            var driver = CreateDriver();
            driver.TopUpWallet(50_000m);
            var act = () => driver.PayCommission(10_000m, 20_000m);
            Assert.Throws<ArgumentException>(act);
        }
        #endregion

        #region Rating Tests
        [Fact]
        public void AverageRating_NoRatings_ShouldReturnFive()
        {
            var driver = CreateDriver();
            Assert.Equal(5.0m, driver.AverageRating);
        }

        [Fact]
        public void UpdateRating_ValidScore_ShouldUpdateAverage()
        {
            var driver = CreateDriver();
            driver.UpdateRating(4);
            driver.UpdateRating(2);
            // (4+2)/2 = 3.0
            Assert.Equal(3.0m, driver.AverageRating);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(6)]
        [InlineData(-1)]
        public void UpdateRating_InvalidScore_ShouldThrowArgumentException(int score)
        {
            var driver = CreateDriver();
            var act = () => driver.UpdateRating(score);
            Assert.Throws<ArgumentException>(act);
        }

        [Fact]
        public void UpdateRating_WithOldScore_ShouldReplaceOldScore()
        {
            var driver = CreateDriver();
            driver.UpdateRating(4);   // ratingTotal=4, ratingCount=1
            driver.UpdateRating(2, 4); // replace 4 with 2 → ratingTotal=2, ratingCount=1
            Assert.Equal(2.0m, driver.AverageRating);
        }
        #endregion

        #region AddTrip Tests
        [Fact]
        public void AddTrip_WhenBusy_ShouldIncrementTotalTrips()
        {
            var driver = CreateDriver();
            driver.SetBusy();
            driver.AddTrip();
            Assert.Equal(1, driver.TotalTrips);
        }

        [Fact]
        public void AddTrip_WhenNotBusy_ShouldThrowInvalidOperationException()
        {
            var driver = CreateDriver();
            var act = () => driver.AddTrip();
            Assert.Throws<InvalidOperationException>(act);
        }
        #endregion

        #region UpdateLocation Tests
        [Fact]
        public void UpdateLocation_ValidLocation_ShouldUpdatePosition()
        {
            var driver = CreateDriver();
            var newLocation = new GeoLocation("Quận 3", "456 Võ Văn Tần", 10.7800, 106.6900);
            driver.UpdateLocation(newLocation);
            Assert.Equal("Quận 3", driver.Position.Name);
        }

        [Fact]
        public void UpdateLocation_NullLocation_ShouldThrowArgumentNullException()
        {
            var driver = CreateDriver();
            var act = () => driver.UpdateLocation(null!);
            Assert.Throws<ArgumentNullException>(act);
        }
        #endregion

        #region UpdateVehicle Tests
        [Fact]
        public void UpdateVehicle_ValidVehicle_ShouldSucceed()
        {
            var driver = CreateDriver();
            var newVehicle = new Motorbike(Guid.NewGuid(), "59B-99999", "Yamaha", "Exciter", "Xanh");
            driver.UpdateVehicle(newVehicle);
            Assert.Equal("59B-99999", driver.Vehicle.PlateNumber);
        }

        [Fact]
        public void UpdateVehicle_NullVehicle_ShouldThrowArgumentNullException()
        {
            var driver = CreateDriver();
            var act = () => driver.UpdateVehicle(null!);
            Assert.Throws<ArgumentNullException>(act);
        }
        #endregion

        #region UpdateLicenseNumber Tests
        [Fact]
        public void UpdateLicenseNumber_ValidLicense_ShouldSucceed()
        {
            var driver = CreateDriver();
            driver.UpdateLicenseNumber("C1-999888");
            Assert.Equal("C1-999888", driver.LicenseNumber);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void UpdateLicenseNumber_InvalidLicense_ShouldThrowArgumentException(string? license)
        {
            var driver = CreateDriver();
            var act = () => driver.UpdateLicenseNumber(license!);
            Assert.Throws<ArgumentException>(act);
        }
        #endregion

        #region Domain Events Tests
        [Fact]
        public void SetBusy_ShouldRaiseDomainEvent()
        {
            var driver = CreateDriver();
            driver.SetBusy();
            Assert.Single(driver.DomainEvents);
        }

        [Fact]
        public void ClearDomainEvents_ShouldRemoveAllEvents()
        {
            var driver = CreateDriver();
            driver.SetBusy();
            driver.ClearDomainEvents();
            Assert.Empty(driver.DomainEvents);
        }

        [Fact]
        public void UpdateLocation_ShouldRaiseDomainEvent()
        {
            var driver = CreateDriver();
            var newLocation = new GeoLocation("Quận 5", "789 Trần Hưng Đạo", 10.7500, 106.6800);
            driver.UpdateLocation(newLocation);
            Assert.Contains(driver.DomainEvents, e => e is OOP.Domain.Events.DriverLocationUpdatedEvent);
        }
        #endregion

        #region GetInfo Tests
        [Fact]
        public void GetInfo_ShouldContainDriverDetails()
        {
            var driver = CreateDriver();
            var info = driver.GetInfo();
            Assert.Contains("Driver", info);
            Assert.Contains("Wave", info);
        }
        #endregion
    }
}
