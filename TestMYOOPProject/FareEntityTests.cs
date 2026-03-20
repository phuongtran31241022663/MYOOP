using OOP.Domain.Entities;
using OOP.Domain.Enums;

namespace TestMYOOPProject
{
    /// <summary>
    /// Unit Tests: Fare entity - tạo, tính giá, validate
    /// </summary>
    public class FareEntityTests
    {
        #region Constructor Tests
        [Fact]
        public void Constructor_ValidData_ShouldCreateFare()
        {
            var fare = new Fare(VehicleType.Motorbike, 10_000m, 5_000m, 0.2m);

            Assert.NotNull(fare);
            Assert.NotEqual(Guid.Empty, fare.Id);
            Assert.Equal(VehicleType.Motorbike, fare.VehicleType);
            Assert.Equal(10_000m, fare.BaseFare);
            Assert.Equal(5_000m, fare.PricePerKm);
            Assert.Equal(0.2m, fare.CommissionRate);
        }

        [Fact]
        public void Constructor_NegativeBaseFare_ShouldThrowArgumentException()
        {
            var act = () => new Fare(VehicleType.Motorbike, -1m, 5_000m, 0.2m);
            Assert.Throws<ArgumentException>(act);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1000)]
        public void Constructor_NonPositivePricePerKm_ShouldThrowArgumentException(decimal pricePerKm)
        {
            var act = () => new Fare(VehicleType.Motorbike, 10_000m, pricePerKm, 0.2m);
            Assert.Throws<ArgumentException>(act);
        }

        [Theory]
        [InlineData(-0.01)]
        [InlineData(1.01)]
        [InlineData(2.0)]
        public void Constructor_InvalidCommissionRate_ShouldThrowArgumentException(decimal rate)
        {
            var act = () => new Fare(VehicleType.Motorbike, 10_000m, 5_000m, rate);
            Assert.Throws<ArgumentException>(act);
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(0.2)]
        [InlineData(1.0)]
        public void Constructor_ValidCommissionRate_ShouldSucceed(decimal rate)
        {
            var fare = new Fare(VehicleType.Motorbike, 10_000m, 5_000m, rate);
            Assert.Equal(rate, fare.CommissionRate);
        }

        [Fact]
        public void Constructor_ZeroBaseFare_ShouldSucceed()
        {
            // BaseFare = 0 is allowed (no opening fee)
            var fare = new Fare(VehicleType.Motorbike, 0m, 5_000m, 0.2m);
            Assert.Equal(0m, fare.BaseFare);
        }
        #endregion

        #region CalculateFare Tests
        [Fact]
        public void CalculateFare_ShortDistance_ShouldReturnBasePlusPerKm()
        {
            // BaseFare=10000, PricePerKm=5000, distance=2km
            // raw = 10000 + 2*5000 = 20000 → floor to 1000 = 20000
            var fare = new Fare(VehicleType.Motorbike, 10_000m, 5_000m, 0.2m);
            var result = fare.CalculateFare(2.0);
            Assert.Equal(20_000m, result);
        }

        [Fact]
        public void CalculateFare_LongerDistance_ShouldCalculateCorrectly()
        {
            // BaseFare=15000, PricePerKm=8000, distance=5km
            // raw = 15000 + 5*8000 = 55000 → floor to 1000 = 55000
            var fare = new Fare(VehicleType.Car, 15_000m, 8_000m, 0.25m);
            var result = fare.CalculateFare(5.0);
            Assert.Equal(55_000m, result);
        }

        [Fact]
        public void CalculateFare_ZeroDistance_ShouldReturnBaseFare()
        {
            // distance=0 → raw = 10000 + 0 = 10000 → floor = 10000
            var fare = new Fare(VehicleType.Motorbike, 10_000m, 5_000m, 0.2m);
            var result = fare.CalculateFare(0.0);
            Assert.Equal(10_000m, result);
        }

        [Fact]
        public void CalculateFare_NegativeDistance_ShouldThrowArgumentException()
        {
            // Arrange
            var fare = new Fare(VehicleType.Motorbike, 10_000m, 5_000m, 0.2m);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => fare.CalculateFare(-1.0));
        }

        [Fact]
        public void CalculateFare_ResultShouldBeRoundedDownToNearest1000()
        {
            // BaseFare=10000, PricePerKm=3000, distance=1.5km
            // raw = 10000 + 1.5*3000 = 14500 → floor(14500/1000)*1000 = 14000
            var fare = new Fare(VehicleType.Motorbike, 10_000m, 3_000m, 0.2m);
            var result = fare.CalculateFare(1.5);
            Assert.Equal(14_000m, result);
        }
        #endregion

        #region UpdateRule Tests
        [Fact]
        public void UpdateRule_ValidData_ShouldUpdateProperties()
        {
            var fare = new Fare(VehicleType.Motorbike, 10_000m, 5_000m, 0.2m);
            fare.UpdateRule(12_000m, 6_000m, 0.25m);

            Assert.Equal(12_000m, fare.BaseFare);
            Assert.Equal(6_000m, fare.PricePerKm);
            Assert.Equal(0.25m, fare.CommissionRate);
        }

        [Fact]
        public void UpdateRule_NegativeBaseFare_ShouldThrowArgumentException()
        {
            var fare = new Fare(VehicleType.Motorbike, 10_000m, 5_000m, 0.2m);
            var act = () => fare.UpdateRule(-1m, 5_000m, 0.2m);
            Assert.Throws<ArgumentException>(act);
        }

        [Fact]
        public void UpdateRule_ZeroPricePerKm_ShouldThrowArgumentException()
        {
            var fare = new Fare(VehicleType.Motorbike, 10_000m, 5_000m, 0.2m);
            var act = () => fare.UpdateRule(10_000m, 0m, 0.2m);
            Assert.Throws<ArgumentException>(act);
        }

        [Fact]
        public void UpdateRule_InvalidCommissionRate_ShouldThrowArgumentException()
        {
            var fare = new Fare(VehicleType.Motorbike, 10_000m, 5_000m, 0.2m);
            var act = () => fare.UpdateRule(10_000m, 5_000m, 1.5m);
            Assert.Throws<ArgumentException>(act);
        }

        [Fact]
        public void UpdateRule_ShouldUpdateUpdatedAt()
        {
            var fare = new Fare(VehicleType.Motorbike, 10_000m, 5_000m, 0.2m);
            var before = DateTime.UtcNow.AddSeconds(-1);
            fare.UpdateRule(12_000m, 6_000m, 0.25m);
            var after = DateTime.UtcNow.AddSeconds(1);

            Assert.InRange(fare.UpdatedAt, before, after);
        }
        #endregion

        #region Validate Tests
        [Fact]
        public void Validate_ValidFare_ShouldNotThrow()
        {
            var fare = new Fare(VehicleType.Motorbike, 10_000m, 5_000m, 0.2m);
            var exception = Record.Exception(() => fare.Validate());
            Assert.Null(exception);
        }
        #endregion
    }
}
