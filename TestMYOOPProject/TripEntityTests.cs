using OOP.Domain.Entities;
using OOP.Domain.Enums;
using OOP.Domain.Events;

namespace TestMYOOPProject
{
    /// <summary>
    /// Unit Tests: Trip entity - state machine, assign driver, cancel, complete
    /// </summary>
    public class TripEntityTests
    {
        #region Helpers
        private static GeoLocation MakeLocation(string name, double lat, double lng)
            => new GeoLocation(name, name + " address", lat, lng);

        private static Trip CreateTrip(double distance = 5.0)
        {
            var pickup = MakeLocation("Điểm đón", 10.7769, 106.7009);
            var dest = MakeLocation("Điểm đến", 10.8000, 106.7200);
            return new Trip(
                Guid.NewGuid(),
                Guid.NewGuid(),
                pickup,
                dest,
                VehicleType.Motorbike,
                distance);
        }

        private static Driver CreateAvailableDriver(VehicleType vehicleType = VehicleType.Motorbike)
        {
            Vehicle vehicle = vehicleType == VehicleType.Motorbike
                ? new Motorbike(Guid.NewGuid(), "59A-12345", "Honda", "Wave", "Đỏ")
                : (Vehicle)new Car(Guid.NewGuid(), "51A-99999", "Toyota", "Vios", "Trắng", 4);

            var position = MakeLocation("Quận 1", 10.7769, 106.7009);
            return new Driver(
                Guid.NewGuid(), "Tran Van Driver", "0911111111", "driver123",
                true, vehicle, position, "B2-123456");
        }

        /// <summary>
        /// Brings a trip to Matched state with a driver assigned.
        /// </summary>
        private static (Trip trip, Driver driver) CreateMatchedTrip()
        {
            var trip = CreateTrip();
            trip.MarkSearching();
            var driver = CreateAvailableDriver();
            trip.AssignDriver(driver);
            driver.SetBusy(); // normally done by TripService, but we need it for AddTrip later
            return (trip, driver);
        }
        #endregion

        #region Constructor Tests
        [Fact]
        public void Constructor_ValidData_ShouldCreateTrip()
        {
            var trip = CreateTrip();

            Assert.NotEqual(Guid.Empty, trip.Id);
            Assert.Equal(TripStatus.Requested, trip.Status);
            Assert.Equal(VehicleType.Motorbike, trip.VehicleType);
            Assert.Equal(5.0, trip.Distance);
            Assert.False(trip.IsRated);
            Assert.Empty(trip.RejectedDriverIds);
        }

        [Fact]
        public void Constructor_EmptyPassengerId_ShouldThrowArgumentException()
        {
            var pickup = MakeLocation("A", 10.0, 106.0);
            var dest = MakeLocation("B", 10.1, 106.1);

            var act = () => new Trip(Guid.Empty, Guid.NewGuid(), pickup, dest, VehicleType.Motorbike, 5.0);

            Assert.Throws<ArgumentException>(act);
        }

        [Fact]
        public void Constructor_NullPickup_ShouldThrowArgumentException()
        {
            var dest = MakeLocation("B", 10.1, 106.1);

            var act = () => new Trip(Guid.NewGuid(), Guid.NewGuid(), null!, dest, VehicleType.Motorbike, 5.0);

            Assert.Throws<ArgumentException>(act);
        }

        [Fact]
        public void Constructor_NullDestination_ShouldThrowArgumentException()
        {
            var pickup = MakeLocation("A", 10.0, 106.0);

            var act = () => new Trip(Guid.NewGuid(), Guid.NewGuid(), pickup, null!, VehicleType.Motorbike, 5.0);

            Assert.Throws<ArgumentException>(act);
        }

        [Fact]
        public void Constructor_SamePickupAndDestination_ShouldThrowArgumentException()
        {
            var loc = MakeLocation("Same", 10.7769, 106.7009);
            var locSame = MakeLocation("Same2", 10.7769, 106.7009); // same coords

            var act = () => new Trip(Guid.NewGuid(), Guid.NewGuid(), loc, locSame, VehicleType.Motorbike, 5.0);

            Assert.Throws<ArgumentException>(act);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Constructor_InvalidDistance_ShouldThrowArgumentException(double distance)
        {
            var pickup = MakeLocation("A", 10.0, 106.0);
            var dest = MakeLocation("B", 10.1, 106.1);

            var act = () => new Trip(Guid.NewGuid(), Guid.NewGuid(), pickup, dest, VehicleType.Motorbike, distance);

            Assert.Throws<ArgumentException>(act);
        }
        #endregion

        #region MarkSearching Tests
        [Fact]
        public void MarkSearching_WhenRequested_ShouldSucceed()
        {
            var trip = CreateTrip();
            trip.MarkSearching();
            Assert.Equal(TripStatus.Searching, trip.Status);
        }

        [Fact]
        public void MarkSearching_WhenNotRequested_ShouldThrowInvalidOperationException()
        {
            var trip = CreateTrip();
            trip.MarkSearching();
            var act = () => trip.MarkSearching(); // already Searching
            Assert.Throws<InvalidOperationException>(act);
        }
        #endregion

        #region AssignDriver Tests
        [Fact]
        public void AssignDriver_WhenRequested_ShouldSucceed()
        {
            var trip = CreateTrip();
            var driver = CreateAvailableDriver();

            trip.AssignDriver(driver);

            Assert.Equal(TripStatus.Matched, trip.Status);
            Assert.Equal(driver.Id, trip.DriverId);
            Assert.NotNull(trip.MatchedAt);
        }

        [Fact]
        public void AssignDriver_WhenSearching_ShouldSucceed()
        {
            var trip = CreateTrip();
            trip.MarkSearching();
            var driver = CreateAvailableDriver();

            trip.AssignDriver(driver);

            Assert.Equal(TripStatus.Matched, trip.Status);
        }

        [Fact]
        public void AssignDriver_NullDriver_ShouldThrowArgumentNullException()
        {
            var trip = CreateTrip();
            var act = () => trip.AssignDriver(null!);
            Assert.Throws<ArgumentNullException>(act);
        }

        [Fact]
        public void AssignDriver_InactiveDriver_ShouldThrowInvalidOperationException()
        {
            var trip = CreateTrip();
            var driver = CreateAvailableDriver();
            driver.Deactivate(Guid.NewGuid());

            var act = () => trip.AssignDriver(driver);
            Assert.Throws<InvalidOperationException>(act);
        }

        [Fact]
        public void AssignDriver_BusyDriver_ShouldThrowInvalidOperationException()
        {
            var trip = CreateTrip();
            var driver = CreateAvailableDriver();
            driver.SetBusy();

            var act = () => trip.AssignDriver(driver);
            Assert.Throws<InvalidOperationException>(act);
        }

        [Fact]
        public void AssignDriver_WrongVehicleType_ShouldThrowInvalidOperationException()
        {
            // Trip wants Motorbike, driver has Car
            var pickup = MakeLocation("A", 10.0, 106.0);
            var dest = MakeLocation("B", 10.1, 106.1);
            var trip = new Trip(Guid.NewGuid(), Guid.NewGuid(), pickup, dest, VehicleType.Car, 5.0);

            var motorbikeDriver = CreateAvailableDriver(VehicleType.Motorbike);

            var act = () => trip.AssignDriver(motorbikeDriver);
            Assert.Throws<InvalidOperationException>(act);
        }

        [Fact]
        public void AssignDriver_WhenCompleted_ShouldThrowInvalidOperationException()
        {
            var trip = CreateTrip();
            trip.MarkSearching();
            var driver = CreateAvailableDriver();
            trip.AssignDriver(driver);
            trip.MarkArrived();
            trip.StartTrip();
            trip.CompleteTrip(5.0, 15.0, 50_000m);

            var driver2 = CreateAvailableDriver();
            var act = () => trip.AssignDriver(driver2);
            Assert.Throws<InvalidOperationException>(act);
        }

        [Fact]
        public void AssignDriver_ShouldRaiseTripMatchedEvent()
        {
            var trip = CreateTrip();
            var driver = CreateAvailableDriver();
            trip.AssignDriver(driver);

            Assert.Contains(trip.DomainEvents, e => e is TripMatchedEvent);
        }
        #endregion

        #region MarkArrived Tests
        [Fact]
        public void MarkArrived_WhenMatched_ShouldSucceed()
        {
            var (trip, _) = CreateMatchedTrip();
            trip.MarkArrived();
            Assert.Equal(TripStatus.Arrived, trip.Status);
            Assert.NotNull(trip.ArrivedAt);
        }

        [Fact]
        public void MarkArrived_WhenNotMatched_ShouldThrowInvalidOperationException()
        {
            var trip = CreateTrip();
            var act = () => trip.MarkArrived();
            Assert.Throws<InvalidOperationException>(act);
        }
        #endregion

        #region StartTrip Tests
        [Fact]
        public void StartTrip_WhenArrived_ShouldSucceed()
        {
            var (trip, _) = CreateMatchedTrip();
            trip.MarkArrived();
            trip.StartTrip();
            Assert.Equal(TripStatus.Started, trip.Status);
            Assert.NotNull(trip.StartedAt);
        }

        [Fact]
        public void StartTrip_WhenNotArrived_ShouldThrowInvalidOperationException()
        {
            var (trip, _) = CreateMatchedTrip();
            var act = () => trip.StartTrip();
            Assert.Throws<InvalidOperationException>(act);
        }
        #endregion

        #region CompleteTrip Tests
        [Fact]
        public void CompleteTrip_WhenStarted_ShouldSucceed()
        {
            var (trip, _) = CreateMatchedTrip();
            trip.MarkArrived();
            trip.StartTrip();

            trip.CompleteTrip(5.0, 15.0, 50_000m);

            Assert.Equal(TripStatus.Completed, trip.Status);
            Assert.Equal(5.0, trip.Distance);
            Assert.Equal(15.0, trip.Duration);
            Assert.Equal(50_000m, trip.Fare);
            Assert.NotNull(trip.CompletedAt);
        }

        [Fact]
        public void CompleteTrip_WhenNotStarted_ShouldThrowInvalidOperationException()
        {
            var (trip, _) = CreateMatchedTrip();
            trip.MarkArrived();
            var act = () => trip.CompleteTrip(5.0, 15.0, 50_000m);
            Assert.Throws<InvalidOperationException>(act);
        }

        [Theory]
        [InlineData(0, 15, 50000)]
        [InlineData(-1, 15, 50000)]
        [InlineData(5, 0, 50000)]
        [InlineData(5, -1, 50000)]
        [InlineData(5, 15, -1)]
        public void CompleteTrip_InvalidArgs_ShouldThrowArgumentException(
            double distance, double duration, decimal fare)
        {
            var (trip, _) = CreateMatchedTrip();
            trip.MarkArrived();
            trip.StartTrip();

            var act = () => trip.CompleteTrip(distance, duration, fare);
            Assert.Throws<ArgumentException>(act);
        }

        [Fact]
        public void CompleteTrip_ShouldRaiseTripCompletedEvent()
        {
            var (trip, _) = CreateMatchedTrip();
            trip.MarkArrived();
            trip.StartTrip();
            trip.CompleteTrip(5.0, 15.0, 50_000m);

            Assert.Contains(trip.DomainEvents, e => e is TripCompletedEvent);
        }
        #endregion

        #region CancelTrip Tests
        [Fact]
        public void CancelTrip_WhenRequested_ShouldSucceed()
        {
            var trip = CreateTrip();
            trip.CancelTrip("Hành khách hủy");
            Assert.Equal(TripStatus.Cancelled, trip.Status);
            Assert.Equal("Hành khách hủy", trip.CancelReason);
            Assert.NotNull(trip.CancelledAt);
        }

        [Fact]
        public void CancelTrip_WhenSearching_ShouldSucceed()
        {
            var trip = CreateTrip();
            trip.MarkSearching();
            trip.CancelTrip("Không tìm được xe");
            Assert.Equal(TripStatus.Cancelled, trip.Status);
        }

        [Fact]
        public void CancelTrip_WhenCompleted_ShouldThrowInvalidOperationException()
        {
            var (trip, _) = CreateMatchedTrip();
            trip.MarkArrived();
            trip.StartTrip();
            trip.CompleteTrip(5.0, 15.0, 50_000m);

            var act = () => trip.CancelTrip("Muốn hủy sau khi xong");
            Assert.Throws<InvalidOperationException>(act);
        }

        [Fact]
        public void CancelTrip_WhenAlreadyCancelled_ShouldThrowInvalidOperationException()
        {
            var trip = CreateTrip();
            trip.CancelTrip("Lần 1");
            var act = () => trip.CancelTrip("Lần 2");
            Assert.Throws<InvalidOperationException>(act);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void CancelTrip_EmptyReason_ShouldThrowArgumentException(string? reason)
        {
            var trip = CreateTrip();
            var act = () => trip.CancelTrip(reason!);
            Assert.Throws<ArgumentException>(act);
        }

        [Fact]
        public void CancelTrip_ShouldRaiseTripCancelledEvent()
        {
            var trip = CreateTrip();
            trip.CancelTrip("Test cancel");
            Assert.Contains(trip.DomainEvents, e => e is TripCancelledEvent);
        }
        #endregion

        #region TimeoutTrip Tests
        [Fact]
        public void TimeoutTrip_WhenRequested_ShouldSucceed()
        {
            var trip = CreateTrip();
            trip.TimeoutTrip();
            Assert.Equal(TripStatus.Timeout, trip.Status);
            Assert.NotNull(trip.TimedOutAt);
        }

        [Fact]
        public void TimeoutTrip_WhenSearching_ShouldSucceed()
        {
            var trip = CreateTrip();
            trip.MarkSearching();
            trip.TimeoutTrip();
            Assert.Equal(TripStatus.Timeout, trip.Status);
        }

        [Fact]
        public void TimeoutTrip_WhenMatched_ShouldThrowInvalidOperationException()
        {
            var (trip, _) = CreateMatchedTrip();
            var act = () => trip.TimeoutTrip();
            Assert.Throws<InvalidOperationException>(act);
        }
        #endregion

        #region ApplyFare / ApplyDistance / ApplyDuration Tests
        [Fact]
        public void ApplyFare_ValidFare_ShouldSetFare()
        {
            var trip = CreateTrip();
            trip.ApplyFare(75_000m);
            Assert.Equal(75_000m, trip.Fare);
        }

        [Fact]
        public void ApplyFare_NegativeFare_ShouldThrowArgumentException()
        {
            var trip = CreateTrip();
            var act = () => trip.ApplyFare(-1m);
            Assert.Throws<ArgumentException>(act);
        }

        [Fact]
        public void ApplyFare_OnCompletedTrip_ShouldThrowInvalidOperationException()
        {
            var (trip, _) = CreateMatchedTrip();
            trip.MarkArrived();
            trip.StartTrip();
            trip.CompleteTrip(5.0, 15.0, 50_000m);

            var act = () => trip.ApplyFare(60_000m);
            Assert.Throws<InvalidOperationException>(act);
        }

        [Fact]
        public void ApplyDistance_ValidDistance_ShouldSetDistance()
        {
            var trip = CreateTrip();
            trip.ApplyDistance(8.5);
            Assert.Equal(8.5, trip.Distance);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void ApplyDistance_InvalidDistance_ShouldThrowArgumentException(double distance)
        {
            var trip = CreateTrip();
            var act = () => trip.ApplyDistance(distance);
            Assert.Throws<ArgumentException>(act);
        }

        [Fact]
        public void ApplyDuration_ValidDuration_ShouldSetDuration()
        {
            var trip = CreateTrip();
            trip.ApplyDuration(20.0);
            Assert.Equal(20.0, trip.Duration);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public void ApplyDuration_InvalidDuration_ShouldThrowArgumentException(double duration)
        {
            var trip = CreateTrip();
            var act = () => trip.ApplyDuration(duration);
            Assert.Throws<ArgumentException>(act);
        }
        #endregion

        #region MarkAsRated Tests
        [Fact]
        public void MarkAsRated_WhenCompleted_ShouldSucceed()
        {
            var (trip, _) = CreateMatchedTrip();
            trip.MarkArrived();
            trip.StartTrip();
            trip.CompleteTrip(5.0, 15.0, 50_000m);

            trip.MarkAsRated();

            Assert.True(trip.IsRated);
        }

        [Fact]
        public void MarkAsRated_WhenNotCompleted_ShouldThrowInvalidOperationException()
        {
            var trip = CreateTrip();
            var act = () => trip.MarkAsRated();
            Assert.Throws<InvalidOperationException>(act);
        }

        [Fact]
        public void MarkAsRated_WhenAlreadyRated_ShouldThrowInvalidOperationException()
        {
            var (trip, _) = CreateMatchedTrip();
            trip.MarkArrived();
            trip.StartTrip();
            trip.CompleteTrip(5.0, 15.0, 50_000m);
            trip.MarkAsRated();

            var act = () => trip.MarkAsRated();
            Assert.Throws<InvalidOperationException>(act);
        }
        #endregion

        #region AddRejectedDriver Tests
        [Fact]
        public void AddRejectedDriver_ValidId_ShouldAddToList()
        {
            var trip = CreateTrip();
            var driverId = Guid.NewGuid();
            trip.AddRejectedDriver(driverId);
            Assert.Contains(driverId, trip.RejectedDriverIds);
        }

        [Fact]
        public void AddRejectedDriver_EmptyGuid_ShouldThrowArgumentException()
        {
            var trip = CreateTrip();
            var act = () => trip.AddRejectedDriver(Guid.Empty);
            Assert.Throws<ArgumentException>(act);
        }

        [Fact]
        public void AddRejectedDriver_DuplicateId_ShouldNotAddTwice()
        {
            var trip = CreateTrip();
            var driverId = Guid.NewGuid();
            trip.AddRejectedDriver(driverId);
            trip.AddRejectedDriver(driverId);
            Assert.Single(trip.RejectedDriverIds);
        }
        #endregion

        #region Domain Events Tests
        [Fact]
        public void MarkSearching_ShouldRaiseTripSearchingEvent()
        {
            var trip = CreateTrip();
            trip.MarkSearching();
            Assert.Contains(trip.DomainEvents, e => e is TripSearchingEvent);
        }

        [Fact]
        public void MarkArrived_ShouldRaiseTripArrivedEvent()
        {
            var (trip, _) = CreateMatchedTrip();
            trip.MarkArrived();
            Assert.Contains(trip.DomainEvents, e => e is TripArrivedEvent);
        }

        [Fact]
        public void StartTrip_ShouldRaiseTripStartedEvent()
        {
            var (trip, _) = CreateMatchedTrip();
            trip.MarkArrived();
            trip.StartTrip();
            Assert.Contains(trip.DomainEvents, e => e is TripStartedEvent);
        }

        [Fact]
        public void TimeoutTrip_ShouldRaiseTripTimeoutEvent()
        {
            var trip = CreateTrip();
            trip.TimeoutTrip();
            Assert.Contains(trip.DomainEvents, e => e is TripTimeoutEvent);
        }

        [Fact]
        public void ClearDomainEvents_ShouldRemoveAllEvents()
        {
            var trip = CreateTrip();
            trip.MarkSearching();
            trip.ClearDomainEvents();
            Assert.Empty(trip.DomainEvents);
        }
        #endregion

        #region ToString Tests
        [Fact]
        public void ToString_ShouldContainTripInfo()
        {
            var trip = CreateTrip();
            var str = trip.ToString();
            Assert.Contains("Requested", str);
            Assert.Contains("Điểm đón", str);
            Assert.Contains("Điểm đến", str);
        }
        #endregion
    }
}
