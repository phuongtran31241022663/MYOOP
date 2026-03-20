using OOP.Domain.Entities;
using OOP.Domain.Enums;

namespace TestMYOOPProject
{
    /// <summary>
    /// Unit Tests: GeoLocation và Vehicle (Motorbike, Car)
    /// </summary>
    public class GeoLocationTests
    {
        #region Constructor Tests
        [Fact]
        public void Constructor_ValidData_ShouldCreateGeoLocation()
        {
            var loc = new GeoLocation("Quận 1", "123 Lê Lợi", 10.7769, 106.7009);

            Assert.Equal("Quận 1", loc.Name);
            Assert.Equal("123 Lê Lợi", loc.Address);
            Assert.Equal(10.7769, loc.Lat);
            Assert.Equal(106.7009, loc.Lng);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_EmptyName_ShouldThrowArgumentException(string? name)
        {
            var act = () => new GeoLocation(name!, "123 Lê Lợi", 10.7769, 106.7009);
            Assert.Throws<ArgumentException>(act);
        }

        [Fact]
        public void Constructor_NullAddress_ShouldDefaultToEmpty()
        {
            var loc = new GeoLocation("Quận 1", null!, 10.7769, 106.7009);
            Assert.Equal(string.Empty, loc.Address);
        }

        [Fact]
        public void DefaultConstructor_ShouldCreateEmptyLocation()
        {
            var loc = new GeoLocation();
            Assert.Equal(string.Empty, loc.Name);
            Assert.Equal(string.Empty, loc.Address);
        }
        #endregion

        #region IsSameLocation Tests
        [Fact]
        public void IsSameLocation_SameCoords_ShouldReturnTrue()
        {
            var a = new GeoLocation("A", "addr", 10.7769, 106.7009);
            var b = new GeoLocation("B", "addr2", 10.7769, 106.7009);
            Assert.True(GeoLocation.IsSameLocation(a, b));
        }

        [Fact]
        public void IsSameLocation_DifferentCoords_ShouldReturnFalse()
        {
            var a = new GeoLocation("A", "addr", 10.7769, 106.7009);
            var b = new GeoLocation("B", "addr2", 10.8000, 106.7200);
            Assert.False(GeoLocation.IsSameLocation(a, b));
        }

        [Fact]
        public void IsSameLocation_VeryCloseCoords_ShouldReturnTrue()
        {
            // Within 0.0001 threshold (~10m)
            var a = new GeoLocation("A", "addr", 10.7769, 106.7009);
            var b = new GeoLocation("B", "addr2", 10.77695, 106.70095);
            Assert.True(GeoLocation.IsSameLocation(a, b));
        }

        [Fact]
        public void IsSameLocation_SlightlyOutsideThreshold_ShouldReturnFalse()
        {
            // Use slightly larger difference to ensure outside threshold
            // 0.0002 > 0.0001 threshold, so should be false
            var a = new GeoLocation("A", "addr", 10.7769, 106.7009);
            var b = new GeoLocation("B", "addr2", 10.7771, 106.7011);
            Assert.False(GeoLocation.IsSameLocation(a, b));
        }

        [Fact]
        public void IsSameLocation_NullA_ShouldReturnFalse()
        {
            var b = new GeoLocation("B", "addr", 10.7769, 106.7009);
            Assert.False(GeoLocation.IsSameLocation(null!, b));
        }

        [Fact]
        public void IsSameLocation_NullB_ShouldReturnFalse()
        {
            var a = new GeoLocation("A", "addr", 10.7769, 106.7009);
            Assert.False(GeoLocation.IsSameLocation(a, null!));
        }

        [Fact]
        public void IsSameLocation_BothNull_ShouldReturnFalse()
        {
            Assert.False(GeoLocation.IsSameLocation(null!, null!));
        }
        #endregion

        #region ToString Tests
        [Fact]
        public void ToString_ShouldContainNameAndCoords()
        {
            var loc = new GeoLocation("Quận 1", "123 Lê Lợi", 10.7769, 106.7009);
            var str = loc.ToString();
            Assert.Contains("Quận 1", str);
            Assert.Contains("10.77690", str);
        }
        #endregion
    }

    /// <summary>
    /// Unit Tests: Vehicle - Motorbike và Car
    /// </summary>
    public class VehicleTests
    {
        private static readonly Guid ValidDriverId = Guid.NewGuid();

        #region Motorbike Tests
        [Fact]
        public void Motorbike_Constructor_ValidData_ShouldCreateMotorbike()
        {
            var bike = new Motorbike(ValidDriverId, "59A-12345", "Honda", "Wave", "Đỏ");

            Assert.NotNull(bike);
            Assert.Equal(VehicleType.Motorbike, bike.Type);
            Assert.Equal("59A-12345", bike.PlateNumber);
            Assert.Equal("Honda", bike.Brand);
            Assert.Equal("Wave", bike.Model);
            Assert.Equal("Đỏ", bike.Color);
            Assert.Equal(2, bike.Capacity);
        }

        [Fact]
        public void Motorbike_GetAverageSpeed_ShouldReturn35()
        {
            var bike = new Motorbike(ValidDriverId, "59A-12345", "Honda", "Wave", "Đỏ");
            Assert.Equal(35, bike.GetAverageSpeed());
        }

        [Fact]
        public void Motorbike_GetMaxPickupDistance_ShouldReturn5()
        {
            var bike = new Motorbike(ValidDriverId, "59A-12345", "Honda", "Wave", "Đỏ");
            Assert.Equal(5, bike.GetMaxPickupDistance());
        }

        [Fact]
        public void Motorbike_EmptyDriverId_ShouldThrowArgumentException()
        {
            var act = () => new Motorbike(Guid.Empty, "59A-12345", "Honda", "Wave", "Đỏ");
            Assert.Throws<ArgumentException>(act);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Motorbike_EmptyPlateNumber_ShouldThrowArgumentException(string? plate)
        {
            var act = () => new Motorbike(ValidDriverId, plate!, "Honda", "Wave", "Đỏ");
            Assert.Throws<ArgumentException>(act);
        }

        [Theory]
        [InlineData("12345")]   // 5 chars - too short
        [InlineData("ABC")]     // 3 chars - too short
        public void Motorbike_ShortPlateNumber_ShouldThrowArgumentException(string plate)
        {
            var act = () => new Motorbike(ValidDriverId, plate, "Honda", "Wave", "Đỏ");
            Assert.Throws<ArgumentException>(act);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Motorbike_EmptyBrand_ShouldThrowArgumentException(string? brand)
        {
            var act = () => new Motorbike(ValidDriverId, "59A-12345", brand!, "Wave", "Đỏ");
            Assert.Throws<ArgumentException>(act);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Motorbike_EmptyModel_ShouldThrowArgumentException(string? model)
        {
            var act = () => new Motorbike(ValidDriverId, "59A-12345", "Honda", model!, "Đỏ");
            Assert.Throws<ArgumentException>(act);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Motorbike_EmptyColor_ShouldThrowArgumentException(string? color)
        {
            var act = () => new Motorbike(ValidDriverId, "59A-12345", "Honda", "Wave", color!);
            Assert.Throws<ArgumentException>(act);
        }
        #endregion

        #region Car Tests
        [Fact]
        public void Car_Constructor_ValidData_ShouldCreateCar()
        {
            var car = new Car(ValidDriverId, "51A-99999", "Toyota", "Vios", "Trắng", 4);

            Assert.NotNull(car);
            Assert.Equal(VehicleType.Car, car.Type);
            Assert.Equal("51A-99999", car.PlateNumber);
            Assert.Equal("Toyota", car.Brand);
            Assert.Equal("Vios", car.Model);
            Assert.Equal("Trắng", car.Color);
            Assert.Equal(4, car.Capacity);
        }

        [Fact]
        public void Car_GetAverageSpeed_ShouldReturn55()
        {
            var car = new Car(ValidDriverId, "51A-99999", "Toyota", "Vios", "Trắng", 4);
            Assert.Equal(55, car.GetAverageSpeed());
        }

        [Fact]
        public void Car_GetMaxPickupDistance_ShouldReturn7()
        {
            var car = new Car(ValidDriverId, "51A-99999", "Toyota", "Vios", "Trắng", 4);
            Assert.Equal(7, car.GetMaxPickupDistance());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Car_InvalidCapacity_ShouldThrowArgumentException(int capacity)
        {
            var act = () => new Car(ValidDriverId, "51A-99999", "Toyota", "Vios", "Trắng", capacity);
            Assert.Throws<ArgumentException>(act);
        }

        [Fact]
        public void Car_EmptyDriverId_ShouldThrowArgumentException()
        {
            var act = () => new Car(Guid.Empty, "51A-99999", "Toyota", "Vios", "Trắng", 4);
            Assert.Throws<ArgumentException>(act);
        }
        #endregion

        #region UpdateVehicleInfo Tests
        [Fact]
        public void UpdateVehicleInfo_ValidData_ShouldUpdateProperties()
        {
            var bike = new Motorbike(ValidDriverId, "59A-12345", "Honda", "Wave", "Đỏ");
            bike.UpdateVehicleInfo("59B-99999", "Yamaha", "Exciter", "Xanh", 2);

            Assert.Equal("59B-99999", bike.PlateNumber);
            Assert.Equal("Yamaha", bike.Brand);
            Assert.Equal("Exciter", bike.Model);
            Assert.Equal("Xanh", bike.Color);
        }

        [Fact]
        public void UpdateVehicleInfo_EmptyPlate_ShouldThrowArgumentException()
        {
            var bike = new Motorbike(ValidDriverId, "59A-12345", "Honda", "Wave", "Đỏ");
            var act = () => bike.UpdateVehicleInfo("", "Yamaha", "Exciter", "Xanh", 2);
            Assert.Throws<ArgumentException>(act);
        }
        #endregion

        #region ToString Tests
        [Fact]
        public void Vehicle_ToString_ShouldContainBrandAndModel()
        {
            var bike = new Motorbike(ValidDriverId, "59A-12345", "Honda", "Wave", "Đỏ");
            var str = bike.ToString();
            Assert.Contains("Honda", str);
            Assert.Contains("Wave", str);
            Assert.Contains("59A-12345", str);
        }
        #endregion
    }
}
