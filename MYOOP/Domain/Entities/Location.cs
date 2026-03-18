﻿using System.Runtime.Serialization;

namespace OOP.Domain.Entities
{
    [DataContract]
    public class Location
    {
        [DataMember] public string Name { get; set; } = string.Empty;
        [DataMember] public string Address { get; set; } = string.Empty;
        [DataMember] public double Lat { get; set; }
        [DataMember] public double Lng { get; set; }

        public Location() { }

        public Location(string name, string address, double lat, double lng)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Nhãn địa điểm không được để trống.", nameof(name));

            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("Địa chỉ không được để trống.", nameof(address));

            if (lat < -90 || lat > 90)
                throw new ArgumentOutOfRangeException(nameof(lat), "Vĩ độ phải từ -90 đến 90.");

            if (lng < -180 || lng > 180)
                throw new ArgumentOutOfRangeException(nameof(lng), "Kinh độ phải từ -180 đến 180.");

            Name = name;
            Address = address;
            Lat = lat;
            Lng = lng;
        }
        public override string ToString() => $"{Name}: {Address} [{Lat}, {Lng}]";
    }
}