using OOP.Domain.Enums;
using System.Runtime.Serialization;

namespace OOP.Domain.Entities
{
    [DataContract]
    public class Passenger : User
    {
        [DataMember]
        public int TotalTrips { get; private set; }
        protected Passenger() { }
        public Passenger(
            Guid id,
            string name,
            string phone,
            string hashedPassword,
            bool isActive)
            : base(id, name, phone, hashedPassword, isActive, UserRole.Passenger)
        {
            TotalTrips = 0;
        }
        public void AddTrip()
        {
            TotalTrips++;
        }
        public override string GetInfo() =>
             $"{base.GetInfo()}\nTổng chuyến: {TotalTrips}";
    }
}
