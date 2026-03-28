using System.Runtime.Serialization;

namespace OOP.Domain.Entities
{
    [DataContract]
    public class GeoLocation
    {
        #region Properties
        [DataMember] public string Name { get; init; }
        [DataMember] public string Address { get; init; }
        [DataMember] public double Lat { get; init; }
        [DataMember] public double Lng { get; init; }
        #endregion
        #region Constructors
        protected GeoLocation() { }

        public GeoLocation(string name, string address, double lat, double lng)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Tên địa điểm không được để trống.");

            if (lat < -90 || lat > 90)
                throw new ArgumentOutOfRangeException(nameof(lat), "Vĩ độ phải nằm trong khoảng từ -90 đến 90.");

            if (lng < -180 || lng > 180)
                throw new ArgumentOutOfRangeException(nameof(lng), "Kinh độ phải nằm trong khoảng từ -180 đến 180.");
            Name = name;
            Address = address ?? string.Empty;
            Lat = lat;
            Lng = lng;
        }
        #endregion
        #region Methods
        public static bool IsSameLocation(GeoLocation a, GeoLocation b)
        {
            if (a == null || b == null) return false;
            const double threshold = 0.0001; // ~10m
            return Math.Abs(a.Lat - b.Lat) <= threshold && Math.Abs(a.Lng - b.Lng) <= threshold;
        }
        public override string ToString()
        {
            return $"{Name} ({Lat:F5}, {Lng:F5})";
        }
        #endregion
    }
}