using GMap.NET;
using GMap.NET.MapProviders;
using System.Runtime.Serialization;

namespace OOP.Domain.Entities
{
    [DataContract]
    public class Location
    {
        // Tên địa điểm gợi nhớ (vd: "Nhà riêng", "Công ty", "Landmark 81")
        [DataMember]
        public string Label { get; init; }

        // Địa chỉ chi tiết: "268 Lý Thường Kiệt, Quận 10, TP.HCM"
        [DataMember]
        public string Address { get; init; }
        [DataMember]
        public double Lat { get; init; }
        [DataMember]
        public double Lng { get; init; }
        protected Location() { }
        public Location(string label, string address, double lat, double lng)
        {
            if (string.IsNullOrWhiteSpace(label))
                throw new ArgumentException("Nhãn địa điểm không được để trống.");

            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("Địa chỉ không được để trống.");

            if (lat < -90 || lat > 90)
                throw new ArgumentOutOfRangeException(nameof(lat), "Vĩ độ phải từ -90 đến 90.");

            if (lng < -180 || lng > 180)
                throw new ArgumentOutOfRangeException(nameof(lng), "Kinh độ phải từ -180 đến 180.");

            Label = label;
            Address = address;
            Lat = lat;
            Lng = lng;
        }
        public override string ToString() => $"{Label}: {Address} [{Lat}, {Lng}]";
    }
}