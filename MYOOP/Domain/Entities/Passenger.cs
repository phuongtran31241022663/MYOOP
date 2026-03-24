using OOP.Domain.Enums;
using System.Runtime.Serialization;

namespace OOP.Domain.Entities
{
    [DataContract]
    public class Passenger : User
    {
        // Trạng thái tài khoản - chỉ có Passenger và Driver có IsActive
        [DataMember] public bool IsActive { get; private set; } = true;

        private int _totalTrips;
        [DataMember]
        public int TotalTrips
        {
            get => _totalTrips;
            private set => _totalTrips = value < 0
                ? throw new InvalidOperationException("Số chuyến không hợp lệ.")
                : value;
        }

        protected Passenger() { }

        public Passenger(
            Guid id,
            string name,
            string phone,
            string rawPassword,
            bool isActive)
            : base(id, name, phone, rawPassword)
        {
            TotalTrips = 0;
            IsActive = isActive;
        }

        // ── Domain behavior ────────────────────────
        public void AddTrip()
        {
            if (!IsActive)
                throw new InvalidOperationException("Tài khoản bị khóa.");

            TotalTrips++;
        }

        public void Deactivate(Guid actorId)
        {
            if (Id == actorId)
                throw new InvalidOperationException("Bạn không thể tự khóa tài khoản của chính mình.");

            if (!IsActive)
                throw new InvalidOperationException("Tài khoản đã bị khóa.");

            IsActive = false;
        }

        public void Activate()
        {
            if (IsActive)
                throw new InvalidOperationException("Tài khoản đang hoạt động.");

            IsActive = true;
        }

        public override string GetInfo()
        {
            return $"{base.GetInfo()} | [Hành khách] Trạng thái: {(IsActive ? "Active" : "Banned")} | Tổng chuyến: {TotalTrips}";
        }
    }
}
