using OOP.Domain.Entities;
using System.Runtime.Serialization;

namespace OOP.Domain.Entities
{
    [DataContract]
    public class Route
    {
        #region Properties
        [DataMember] public Guid Id { get; private set; }
        private double distance;
        [DataMember]
        public double Distance
        {
            get => distance;
            private set => distance = value < 0
                ? throw new ArgumentException("Khoảng cách không hợp lệ.")
                : value;
        }

        private double duration;
        [DataMember]
        public double Duration
        {
            get => duration;
            // Chỉ dùng để hiển thị thời gian ước tính, không validate chặt
            private set => duration = value;
        }

        [DataMember] private List<GeoLocation> points = new();
        public IReadOnlyList<GeoLocation> Points => points.AsReadOnly();

        private GeoLocation start = null!;
        [DataMember]
        public GeoLocation Start
        {
            get => start;
            set => start = value ?? throw new ArgumentNullException("Điểm bắt đầu không được null.");
        }

        private GeoLocation end = null!;
        [DataMember]
        public GeoLocation End
        {
            get => end;
            set
            {
                if (value == null)
                    throw new ArgumentNullException("Điểm kết thúc không được null.");
                if (start != null && GeoLocation.IsSameLocation(start, value))
                    throw new ArgumentException("Điểm bắt đầu và kết thúc không được trùng nhau.");
                end = value;
            }
        }
        #endregion
        #region Constructors
        public Route() { }

        public Route(GeoLocation start, GeoLocation end, double distance, double duration, List<GeoLocation> points)
        {
            // Properties will validate automatically via their setters
            Id = Guid.NewGuid();
            Start = start;
            End = end;
            Distance = distance;
            Duration = duration;
            this.points = points ?? new List<GeoLocation>();
        }
        #endregion
    }
}
