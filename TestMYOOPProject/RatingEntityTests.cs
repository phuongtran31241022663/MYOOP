using OOP.Domain.Entities;

namespace TestMYOOPProject
{
    /// <summary>
    /// Unit Tests: Rating entity - tạo, validate, cập nhật
    /// </summary>
    public class RatingEntityTests
    {
        private static readonly Guid ValidTripId = Guid.NewGuid();
        private static readonly Guid ValidDriverId = Guid.NewGuid();
        private static readonly Guid ValidPassengerId = Guid.NewGuid();

        #region Constructor Tests
        [Fact]
        public void Constructor_ValidData_ShouldCreateRating()
        {
            var rating = new Rating(ValidTripId, ValidDriverId, ValidPassengerId, 5, "Tốt lắm");

            Assert.NotNull(rating);
            Assert.Equal(ValidTripId, rating.TripId);
            Assert.Equal(ValidDriverId, rating.DriverId);
            Assert.Equal(ValidPassengerId, rating.PassengerId);
            Assert.Equal(5, rating.Score);
            Assert.Equal("Tốt lắm", rating.Comment);
        }

        [Fact]
        public void Constructor_EmptyTripId_ShouldThrowArgumentException()
        {
            var act = () => new Rating(Guid.Empty, ValidDriverId, ValidPassengerId, 5, "OK");
            Assert.Throws<ArgumentException>(act);
        }

        [Fact]
        public void Constructor_EmptyDriverId_ShouldThrowArgumentException()
        {
            var act = () => new Rating(ValidTripId, Guid.Empty, ValidPassengerId, 5, "OK");
            Assert.Throws<ArgumentException>(act);
        }

        [Fact]
        public void Constructor_EmptyPassengerId_ShouldThrowArgumentException()
        {
            var act = () => new Rating(ValidTripId, ValidDriverId, Guid.Empty, 5, "OK");
            Assert.Throws<ArgumentException>(act);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(6)]
        [InlineData(-1)]
        public void Constructor_InvalidScore_ShouldThrowArgumentException(int score)
        {
            var act = () => new Rating(ValidTripId, ValidDriverId, ValidPassengerId, score, "OK");
            Assert.Throws<ArgumentException>(act);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        public void Constructor_ValidScore_ShouldSucceed(int score)
        {
            var rating = new Rating(ValidTripId, ValidDriverId, ValidPassengerId, score, "Bình thường");
            Assert.Equal(score, rating.Score);
        }

        [Fact]
        public void Constructor_NullComment_ShouldDefaultToEmpty()
        {
            var rating = new Rating(ValidTripId, ValidDriverId, ValidPassengerId, 5, null!);
            Assert.Equal(string.Empty, rating.Comment);
        }

        [Fact]
        public void Constructor_CommentWithWhitespace_ShouldBeTrimmed()
        {
            var rating = new Rating(ValidTripId, ValidDriverId, ValidPassengerId, 5, "  Tốt  ");
            Assert.Equal("Tốt", rating.Comment);
        }

        [Fact]
        public void Constructor_ShouldSetCreatedAt()
        {
            var before = DateTime.UtcNow.AddSeconds(-1);
            var rating = new Rating(ValidTripId, ValidDriverId, ValidPassengerId, 5, "OK");
            var after = DateTime.UtcNow.AddSeconds(1);

            Assert.InRange(rating.CreatedAt, before, after);
        }
        #endregion

        #region UpdateScore Tests
        [Fact]
        public void UpdateScore_ValidData_ShouldUpdateScoreAndComment()
        {
            var rating = new Rating(ValidTripId, ValidDriverId, ValidPassengerId, 5, "Tốt");
            rating.UpdateScore(4, "Khá tốt");

            Assert.Equal(4, rating.Score);
            Assert.Equal("Khá tốt", rating.Comment);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(6)]
        [InlineData(-1)]
        public void UpdateScore_InvalidScore_ShouldThrowArgumentException(int score)
        {
            var rating = new Rating(ValidTripId, ValidDriverId, ValidPassengerId, 5, "Tốt");
            var act = () => rating.UpdateScore(score, "Bình thường");
            Assert.Throws<ArgumentException>(act);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void UpdateScore_LowScoreWithoutComment_ShouldThrowArgumentException(int lowScore)
        {
            var rating = new Rating(ValidTripId, ValidDriverId, ValidPassengerId, 5, "Tốt");
            var act = () => rating.UpdateScore(lowScore, "");
            Assert.Throws<ArgumentException>(act);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void UpdateScore_LowScoreWithComment_ShouldSucceed(int lowScore)
        {
            var rating = new Rating(ValidTripId, ValidDriverId, ValidPassengerId, 5, "Tốt");
            rating.UpdateScore(lowScore, "Tài xế đến muộn");
            Assert.Equal(lowScore, rating.Score);
        }

        [Theory]
        [InlineData(4)]
        [InlineData(5)]
        public void UpdateScore_HighScoreWithoutComment_ShouldSucceed(int highScore)
        {
            var rating = new Rating(ValidTripId, ValidDriverId, ValidPassengerId, 3, "Bình thường");
            rating.UpdateScore(highScore, "");
            Assert.Equal(highScore, rating.Score);
        }
        #endregion

        #region ToString Tests
        [Fact]
        public void ToString_ShouldContainStarsAndScore()
        {
            var rating = new Rating(ValidTripId, ValidDriverId, ValidPassengerId, 4, "Tốt");
            var str = rating.ToString();
            Assert.Contains("4/5", str);
            Assert.Contains("⭐", str);
        }

        [Fact]
        public void ToString_EmptyComment_ShouldShowNoComment()
        {
            var rating = new Rating(ValidTripId, ValidDriverId, ValidPassengerId, 5, "");
            var str = rating.ToString();
            Assert.Contains("Không có", str);
        }

        [Fact]
        public void ToString_WithComment_ShouldShowComment()
        {
            var rating = new Rating(ValidTripId, ValidDriverId, ValidPassengerId, 5, "Rất tốt");
            var str = rating.ToString();
            Assert.Contains("Rất tốt", str);
        }
        #endregion
    }
}
