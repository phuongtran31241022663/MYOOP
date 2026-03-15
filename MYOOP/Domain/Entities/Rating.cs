using OOP.Application.Validators;
using System.Runtime.Serialization;

namespace OOP.Domain.Entities
{
    [DataContract]
    public class Rating
    {
        [DataMember]
        public Guid Id { get; init; }

        [DataMember]
        public Guid TripId { get; init; }

        [DataMember]
        public Guid DriverId { get; init; }

        [DataMember]
        public Guid PassengerId { get; init; }

        [DataMember]
        public int Score { get; private set; }

        [DataMember]
        public string Comment { get; private set; }

        [DataMember]
        public DateTime CreatedAt { get; init; }
        protected Rating() { }
        public Rating(Guid tripId, Guid driverId, Guid passengerId, int score, string comment)
        {
            if (tripId == Guid.Empty || driverId == Guid.Empty || passengerId == Guid.Empty)
                throw new ArgumentException("Thông tin định danh (ID) không hợp lệ.");

            RatingValidator.ValidateRating(score, comment);

            Id = Guid.NewGuid();
            TripId = tripId;
            DriverId = driverId;
            PassengerId = passengerId;
            Score = score;
            Comment = comment?.Trim() ?? string.Empty;
            CreatedAt = DateTime.UtcNow;
        }
        public void Update(int score, string comment)
        {
            RatingValidator.ValidateRating(score, comment);

            Score = score;
            Comment = comment?.Trim() ?? string.Empty;
        }
        public override string ToString()
        {
            string stars = new string('⭐', Score);
            return $"{stars} ({Score}/5)\nGóp ý: {(string.IsNullOrEmpty(Comment) ? "(Không có)" : Comment)}";
        }
    }
}
