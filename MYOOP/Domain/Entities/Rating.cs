using OOP.Domain.Enums;
using System.Runtime.Serialization;

namespace OOP.Domain.Entities
{
    [DataContract]
    public class Rating
    {
        #region Properties
        [DataMember] public Guid Id { get; init; }

        private Guid tripId;
        [DataMember]
        public Guid TripId
        {
            get => tripId;
            init => tripId = value == Guid.Empty
                ? throw new ArgumentException("Mã chuyến đi không hợp lệ.")
                : value;
        }

        private Guid driverId;
        [DataMember]
        public Guid DriverId
        {
            get => driverId;
            init => driverId = value == Guid.Empty
                ? throw new ArgumentException("Mã tài xế không hợp lệ.")
                : value;
        }

        private Guid passengerId;
        [DataMember]
        public Guid PassengerId
        {
            get => passengerId;
            init => passengerId = value == Guid.Empty
                ? throw new ArgumentException("Mã khách hàng không hợp lệ.")
                : value;
        }

        private int score;
        [DataMember]
        public int Score
        {
            get => score;
            private set => score = value < 1 || value > 5
                ? throw new ArgumentException("Sao phải từ 1-5.")
                : value;
        }

        [DataMember] public string Comment { get; private set; } = string.Empty;

        private const int MaxCommentLength = 500;

        [DataMember] public DateTime CreatedAt { get; init; }
        #endregion
        #region Constructors
        protected Rating() { }
        public Rating(Guid tripId, Guid driverId, Guid passengerId, int score, string comment)
        {
            Id = Guid.NewGuid();
            TripId = tripId;
            DriverId = driverId;
            PassengerId = passengerId;
            Score = score;
            Comment = ValidateComment(comment);
            CreatedAt = DateTime.UtcNow;
        }

        private static string ValidateComment(string? comment)
        {
            var trimmed = comment?.Trim() ?? string.Empty;
            if (trimmed.Length > MaxCommentLength)
                throw new ArgumentException($"Góp ý không được vượt quá {MaxCommentLength} ký tự.");
            return trimmed;
        }
        #endregion
        public void UpdateScore(int score, string comment)
        {
            if (score < 1 || score > 5) throw new ArgumentException("Sao phải từ 1-5.");
            if (score <= 3 && string.IsNullOrWhiteSpace(comment))
                throw new ArgumentException("Vui lòng để lại lý do cho đánh giá thấp.");

            Score = score;
            Comment = ValidateComment(comment);
        }
        public override string ToString()
        {
            string stars = new string('⭐', Score);
            return $"{stars} ({Score}/5)\nGóp ý: {(string.IsNullOrEmpty(Comment) ? "(Không có)" : Comment)}";
        }
    }
}