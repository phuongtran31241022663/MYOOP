using OOP.Domain.Entities;
using DomainLocation = OOP.Domain.Entities.Location;

namespace OOP.Domain.Events
{
    // 1. Định nghĩa Delegate cho gọn (giống như định nghĩa kiểu dữ liệu)
    public delegate Task TripEventHandler(Trip trip);
    public delegate Task LocationUpdatedHandler(Guid id, DomainLocation loc);

    // 2. Gom tất cả vào một "Hub" trung tâm
    public static class TripEvents
    {
        public static event TripEventHandler? Requested;
        public static event TripEventHandler? Matched;
        public static event TripEventHandler? Started;
        public static event TripEventHandler? Completed;
        public static event TripEventHandler? Cancelled;
        public static event LocationUpdatedHandler? DriverLocationUpdated;

        // Các phương thức Raise viết ngắn gọn (Expression-bodied)
        public static async Task OnRequested(Trip t) => await Invoke(Requested, t);
        public static async Task OnMatched(Trip t) => await Invoke(Matched, t);
        public static async Task OnStarted(Trip t) => await Invoke(Started, t);
        public static async Task OnCompleted(Trip t) => await Invoke(Completed, t);
        public static async Task OnCancelled(Trip t) => await Invoke(Cancelled, t);

        public static async Task OnDriverLocationUpdated(Guid id, DomainLocation loc)
        {
            if (DriverLocationUpdated == null) return;
            foreach (var handler in DriverLocationUpdated.GetInvocationList().Cast<LocationUpdatedHandler>())
                await handler(id, loc);
        }

        private static async Task Invoke(TripEventHandler? evt, Trip t)
        {
            if (evt == null) return;
            foreach (var handler in evt.GetInvocationList().Cast<TripEventHandler>())
                await handler(t);
        }
    }
}