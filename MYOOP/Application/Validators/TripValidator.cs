using GMap.NET.MapProviders;
using OOP.Domain.Entities;
using OOP.Domain.Enums;
using DomainLocation = OOP.Domain.Entities.Location;

namespace OOP.Application.Validators
{
    public static class TripValidator
    {
        // Gọi trước khi tạo Trip mới
        public static void ValidateRequest(DomainLocation pickup, DomainLocation destination, VehicleType vehicleType, double routeDistance)
        {
            if (pickup == null)
                throw new ArgumentNullException(nameof(pickup), "Điểm đón không được để trống.");

            if (destination == null)
                throw new ArgumentNullException(nameof(destination), "Điểm đến không được để trống.");

            if (pickup.Lat == destination.Lat && pickup.Lng == destination.Lng)
                throw new ArgumentException("Điểm đón và điểm đến không được trùng nhau.");

            if (!Enum.IsDefined(typeof(VehicleType), vehicleType))
                throw new ArgumentException("Loại xe không hợp lệ.");

            if (routeDistance <= 0)
                throw new ArgumentException("Không tính được khoảng cách lộ trình cho chuyến đi.");
        }

        // Gọi trước khi hủy chuyến
        public static void ValidateCancellation(Trip trip)
        {
            if (trip == null)
                throw new ArgumentNullException(nameof(trip));

            if (trip.Status == TripStatus.Completed)
                throw new InvalidOperationException("Không thể hủy chuyến đã hoàn thành.");

            if (trip.Status == TripStatus.Cancelled)
                throw new InvalidOperationException("Chuyến đã được hủy trước đó.");

            if (trip.Status == TripStatus.Timeout)
                throw new InvalidOperationException("Chuyến đã hết thời gian tìm tài xế.");
        }

        // Gọi trước khi bắt đầu chuyến (Arrived → Started)
        public static void ValidateStart(Trip trip)
        {
            if (trip == null)
                throw new ArgumentNullException(nameof(trip));

            if (trip.Status != TripStatus.Arrived)
                throw new InvalidOperationException(
                    $"Không thể bắt đầu chuyến khi trạng thái là '{trip.Status}'. Tài xế phải đến nơi đón trước.");

            if (trip.DriverId == null)
                throw new InvalidOperationException("Chuyến chưa được gán tài xế.");
        }

        // Gọi trước khi hoàn thành chuyến (Started → Completed)
        public static void ValidateCompletion(Trip trip, double distance, decimal fare)
        {
            if (trip == null)
                throw new ArgumentNullException(nameof(trip));

            if (trip.Status != TripStatus.Started)
                throw new InvalidOperationException(
                    $"Không thể hoàn thành chuyến khi trạng thái là '{trip.Status}'. Chuyến phải đang chạy.");
            if (distance <= 0) throw new ArgumentException("Khoảng cách không hợp lệ.");
            if (fare < 0) throw new ArgumentException("Cước phí không hợp lệ.");
        }

        // Gọi trước khi assign driver vào trip (Requested → Matched)
        public static void ValidateDriverAssignment(Trip trip, Driver driver)
        {
            if (trip == null)
                throw new ArgumentNullException(nameof(trip));

            if (driver == null)
                throw new ArgumentNullException(nameof(driver));

            if (trip.Status != TripStatus.Requested && trip.Status != TripStatus.Searching)
                throw new InvalidOperationException(
                    $"Không thể gán tài xế khi trip đang ở trạng thái '{trip.Status}'.");

            if (driver.Vehicle.Type != trip.VehicleType)
                throw new InvalidOperationException(
                    $"Loại xe không phù hợp: trip yêu cầu '{trip.VehicleType}', tài xế có '{driver.Vehicle.Type}'.");

            if (!driver.IsActive)
                throw new InvalidOperationException("Tài xế đã bị vô hiệu hóa.");

            if (driver.Status != DriverStatus.Available)
                throw new InvalidOperationException(
                    $"Tài xế hiện không sẵn sàng (trạng thái: '{driver.Status}').");
        }

        // Gọi độc lập khi cần kiểm tra tài xế có thể nhận chuyến không
        public static void ValidateDriverAvailability(Driver driver)
        {
            if (driver == null)
                throw new ArgumentNullException(nameof(driver));

            if (!driver.IsActive)
                throw new InvalidOperationException("Tài xế đã bị vô hiệu hóa.");

            if (driver.Status != DriverStatus.Available)
                throw new InvalidOperationException(
                    $"Tài xế hiện không sẵn sàng nhận chuyến (trạng thái: '{driver.Status}').");
        }
    }
}
