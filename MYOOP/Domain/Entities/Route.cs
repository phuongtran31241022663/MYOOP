using OOP.Domain.Entities;
using System.Runtime.Serialization;

namespace OOP.Domain.Entities
{
    [DataContract]
    public class Route
    {
        #region Properties
        [DataMember] public Guid Id { get; private set; }
        private double _distance;
        [DataMember]
        public double Distance
        {
            get => _distance;
            private set => _distance = value <= 0
                ? throw new ArgumentException("Khoảng cách không hợp lệ.")
                : value;
        }

        private double _duration;
        [DataMember]
        public double Duration
        {
            get => _duration;
            private set => _duration = value <= 0
                ? throw new ArgumentException("Thời gian không hợp lệ.")
                : value;
        }

        [DataMember] private List<GeoLocation> _points = new();
        public IReadOnlyList<GeoLocation> Points => _points.AsReadOnly();

        public void AddPoint(GeoLocation point)
        {
            _points.Add(point ?? throw new ArgumentNullException("Điểm không được null."));
        }

        public void ClearPoints() => _points.Clear();

        private GeoLocation _start = null!;
        [DataMember]
        public GeoLocation Start
        {
            get => _start;
            set => _start = value ?? throw new ArgumentNullException("Điểm bắt đầu không được null.");
        }

        private GeoLocation _end = null!;
        [DataMember]
        public GeoLocation End
        {
            get => _end;
            set
            {
                if (value == null)
                    throw new ArgumentNullException("Điểm kết thúc không được null.");
                if (_start != null && GeoLocation.IsSameLocation(_start, value))
                    throw new ArgumentException("Điểm bắt đầu và kết thúc không được trùng nhau.");
                _end = value;
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
            _points = points ?? new List<GeoLocation>();
        }
        #endregion
        public GeoLocation GetLocationAtProgress(double progress)
        {
            if (Points.Count == 0) return Start;
            int index = (int)(progress * (Points.Count - 1));
            return Points[index];
        }
    }
}
