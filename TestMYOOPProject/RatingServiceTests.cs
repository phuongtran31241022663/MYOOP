using Moq;
using OOP.Application.Services;
using OOP.Domain.Entities;
using OOP.Domain.Enums;
using OOP.Domain.Interfaces;

namespace TestMYOOPProject
{
    /// <summary>
    /// Integration Tests: RatingService - tạo đánh giá, validate
    /// </summary>
    public class RatingServiceTests
    {
        private readonly Mock<IRatingRepository> _mockRatingRepo;
        private readonly Mock<IUserRepository> _mockUserRepo;
        private readonly Mock<ITripRepository> _mockTripRepo;
        private readonly RatingService _ratingService;

        public RatingServiceTests()
        {
            _mockRatingRepo = new Mock<IRatingRepository>();
            _mockUserRepo = new Mock<IUserRepository>();
            _mockTripRepo = new Mock<ITripRepository>();
            _ratingService = new RatingService(
                _mockRatingRepo.Object,
                _mockUserRepo.Object,
                _mockTripRepo.Object);
        }

        #region Helpers
        private static Driver CreateDriver()
        {
            var vehicle = new Motorbike(Guid.NewGuid(), "59A-12345", "Honda", "Wave", "Đỏ");
            var position = new GeoLocation("Quận 1", "123 Lê Lợi", 10.7769, 106.7009);
            return new Driver(
                Guid.NewGuid(), "Tran Van Driver", "0911111111", "driver123",
                true, vehicle, position, "B2-123456");
        }

        private static Trip CreateCompletedTrip(Guid passengerId, Guid driverId)
        {
            var pickup = new GeoLocation("Điểm đón", "Điểm đón address", 10.7769, 106.7009);
            var dest = new GeoLocation("Điểm đến", "Điểm đến address", 10.8000, 106.7200);
            var trip = new Trip(passengerId, Guid.NewGuid(), pickup, dest, VehicleType.Motorbike, 5.0);

            // Simulate full trip lifecycle
            trip.MarkSearching();
            var vehicle = new Motorbike(driverId, "59A-12345", "Honda", "Wave", "Đỏ");
            var position = new GeoLocation("Quận 1", "123 Lê Lợi", 10.7769, 106.7009);
            var driver = new Driver(driverId, "Tran Van Driver", "0911111111", "driver123",
                true, vehicle, position, "B2-123456");
            trip.AssignDriver(driver);
            trip.MarkArrived();
            trip.StartTrip();
            trip.CompleteTrip(5.0, 15.0, 50_000m);

            return trip;
        }
        #endregion

        #region CreateRating Tests
        [Fact]
        public async Task CreateRating_ValidData_ShouldReturnRating()
        {
            // Arrange
            var passengerId = Guid.NewGuid();
            var driver = CreateDriver();
            var trip = CreateCompletedTrip(passengerId, driver.Id);

            _mockTripRepo.Setup(r => r.GetById(trip.Id)).ReturnsAsync(trip);
            _mockRatingRepo.Setup(r => r.ExistsForTrip(trip.Id)).ReturnsAsync(false);
            _mockRatingRepo.Setup(r => r.Add(It.IsAny<Rating>())).Returns(Task.CompletedTask);
            _mockUserRepo.Setup(r => r.GetById(driver.Id)).ReturnsAsync(driver);
            _mockUserRepo.Setup(r => r.Update(It.IsAny<User>())).Returns(Task.CompletedTask);
            _mockTripRepo.Setup(r => r.Update(It.IsAny<Trip>())).Returns(Task.CompletedTask);

            // Act
            var rating = await _ratingService.CreateRating(trip.Id, passengerId, 5, "Tốt lắm");

            // Assert
            Assert.NotNull(rating);
            Assert.Equal(trip.Id, rating.TripId);
            Assert.Equal(passengerId, rating.PassengerId);
            Assert.Equal(driver.Id, rating.DriverId);
            Assert.Equal(5, rating.Score);
            Assert.Equal("Tốt lắm", rating.Comment);
        }

        [Fact]
        public async Task CreateRating_TripNotFound_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var tripId = Guid.NewGuid();
            _mockTripRepo.Setup(r => r.GetById(tripId)).ReturnsAsync((Trip?)null);

            // Act
            var act = () => _ratingService.CreateRating(tripId, Guid.NewGuid(), 5, "OK");

            // Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(act);
        }

        [Fact]
        public async Task CreateRating_AlreadyRated_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var passengerId = Guid.NewGuid();
            var driver = CreateDriver();
            var trip = CreateCompletedTrip(passengerId, driver.Id);

            _mockTripRepo.Setup(r => r.GetById(trip.Id)).ReturnsAsync(trip);
            _mockRatingRepo.Setup(r => r.ExistsForTrip(trip.Id)).ReturnsAsync(true); // already rated

            // Act
            var act = () => _ratingService.CreateRating(trip.Id, passengerId, 5, "OK");

            // Assert
            await Assert.ThrowsAsync<InvalidOperationException>(act);
        }

        [Fact]
        public async Task CreateRating_WrongPassenger_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var realPassengerId = Guid.NewGuid();
            var wrongPassengerId = Guid.NewGuid();
            var driver = CreateDriver();
            var trip = CreateCompletedTrip(realPassengerId, driver.Id);

            _mockTripRepo.Setup(r => r.GetById(trip.Id)).ReturnsAsync(trip);
            _mockRatingRepo.Setup(r => r.ExistsForTrip(trip.Id)).ReturnsAsync(false);

            // Act
            var act = () => _ratingService.CreateRating(trip.Id, wrongPassengerId, 5, "OK");

            // Assert
            await Assert.ThrowsAsync<InvalidOperationException>(act);
        }

        [Fact]
        public async Task CreateRating_ShouldCallRatingRepoAdd()
        {
            // Arrange
            var passengerId = Guid.NewGuid();
            var driver = CreateDriver();
            var trip = CreateCompletedTrip(passengerId, driver.Id);

            _mockTripRepo.Setup(r => r.GetById(trip.Id)).ReturnsAsync(trip);
            _mockRatingRepo.Setup(r => r.ExistsForTrip(trip.Id)).ReturnsAsync(false);
            _mockRatingRepo.Setup(r => r.Add(It.IsAny<Rating>())).Returns(Task.CompletedTask);
            _mockUserRepo.Setup(r => r.GetById(driver.Id)).ReturnsAsync(driver);
            _mockUserRepo.Setup(r => r.Update(It.IsAny<User>())).Returns(Task.CompletedTask);
            _mockTripRepo.Setup(r => r.Update(It.IsAny<Trip>())).Returns(Task.CompletedTask);

            // Act
            await _ratingService.CreateRating(trip.Id, passengerId, 4, "Khá tốt");

            // Assert
            _mockRatingRepo.Verify(r => r.Add(It.IsAny<Rating>()), Times.Once);
        }

        [Fact]
        public async Task CreateRating_ShouldUpdateDriverRating()
        {
            // Arrange
            var passengerId = Guid.NewGuid();
            var driver = CreateDriver();
            var trip = CreateCompletedTrip(passengerId, driver.Id);

            _mockTripRepo.Setup(r => r.GetById(trip.Id)).ReturnsAsync(trip);
            _mockRatingRepo.Setup(r => r.ExistsForTrip(trip.Id)).ReturnsAsync(false);
            _mockRatingRepo.Setup(r => r.Add(It.IsAny<Rating>())).Returns(Task.CompletedTask);
            _mockUserRepo.Setup(r => r.GetById(driver.Id)).ReturnsAsync(driver);
            _mockUserRepo.Setup(r => r.Update(It.IsAny<User>())).Returns(Task.CompletedTask);
            _mockTripRepo.Setup(r => r.Update(It.IsAny<Trip>())).Returns(Task.CompletedTask);

            // Act
            await _ratingService.CreateRating(trip.Id, passengerId, 3, "Bình thường");

            // Assert - driver rating should be updated
            _mockUserRepo.Verify(r => r.Update(It.Is<User>(u => u.Id == driver.Id)), Times.Once);
        }
        #endregion

        #region GetRatingByTrip Tests
        [Fact]
        public async Task GetRatingByTrip_ExistingTrip_ShouldReturnRating()
        {
            // Arrange
            var tripId = Guid.NewGuid();
            var rating = new Rating(tripId, Guid.NewGuid(), Guid.NewGuid(), 5, "Tốt");
            _mockRatingRepo.Setup(r => r.GetByTripId(tripId)).ReturnsAsync(rating);

            // Act
            var result = await _ratingService.GetRatingByTrip(tripId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(tripId, result.TripId);
        }

        [Fact]
        public async Task GetRatingByTrip_NonExistingTrip_ShouldReturnNull()
        {
            // Arrange
            var tripId = Guid.NewGuid();
            _mockRatingRepo.Setup(r => r.GetByTripId(tripId)).ReturnsAsync((Rating?)null);

            // Act
            var result = await _ratingService.GetRatingByTrip(tripId);

            // Assert
            Assert.Null(result);
        }
        #endregion

        #region GetRatingsByDriver Tests
        [Fact]
        public async Task GetRatingsByDriver_ShouldReturnDriverRatings()
        {
            // Arrange
            var driverId = Guid.NewGuid();
            var ratings = new List<Rating>
            {
                new Rating(Guid.NewGuid(), driverId, Guid.NewGuid(), 5, "Tốt"),
                new Rating(Guid.NewGuid(), driverId, Guid.NewGuid(), 4, "Khá tốt")
            };
            _mockRatingRepo.Setup(r => r.GetByDriverId(driverId)).ReturnsAsync(ratings);

            // Act
            var result = await _ratingService.GetRatingsByDriver(driverId);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.All(result, r => Assert.Equal(driverId, r.DriverId));
        }

        [Fact]
        public async Task GetRatingsByDriver_NoRatings_ShouldReturnEmptyList()
        {
            // Arrange
            var driverId = Guid.NewGuid();
            _mockRatingRepo.Setup(r => r.GetByDriverId(driverId)).ReturnsAsync(new List<Rating>());

            // Act
            var result = await _ratingService.GetRatingsByDriver(driverId);

            // Assert
            Assert.Empty(result);
        }
        #endregion
    }
}
