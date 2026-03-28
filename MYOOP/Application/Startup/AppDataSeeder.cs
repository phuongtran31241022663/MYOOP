using OOP.Application.Services.Interfaces;
using OOP.Domain.Entities;
using OOP.Domain.Enums;
using OOP.Domain.Interfaces;
using DomainLocation = OOP.Domain.Entities.GeoLocation;

namespace OOP
{
    internal static class AppDataSeeder
    {
        public static async Task SeedAsync(
            ITripRepository tripRepo,
            IUserRepository userRepo,
            IFareRepository fareRepo,
            IUserService userService)
        {
            const string adminPhone = "0000000000";
            if (!await userRepo.ExistsByPhone(adminPhone))
            {
                var admin = new Admin(
                    Guid.NewGuid(), "Hệ Thống Admin", adminPhone,
                    "admin123");
                await userRepo.Add(admin);
            }

            var q1 = new DomainLocation("Quận 1", "Bến Thành", 10.7720, 106.6980);
            var q3 = new DomainLocation("Quận 3", "Võ Thị Sáu", 10.7846, 106.6844);
            var q5 = new DomainLocation("Quận 5", "Chợ Lớn", 10.7540, 106.6640);
            var q7 = new DomainLocation("Quận 7", "Phú Mỹ Hưng", 10.7287, 106.7219);
            var tanBinh = new DomainLocation("Tân Bình", "Sân bay Tân Sơn Nhất", 10.8132, 106.6620);
            var phuNhuan = new DomainLocation("Phú Nhuận", "Ngã 4 Phú Nhuận", 10.7995, 106.6792);

            var p1 = await EnsurePassenger(userService, userRepo, "Nguyễn Văn A", "0900000001");
            var p2 = await EnsurePassenger(userService, userRepo, "Trần Thị B", "0900000002");
            var p3 = await EnsurePassenger(userService, userRepo, "Phạm Minh C", "0900000004");

            var d1 = await EnsureDriver(
                userRepo,
                "L?? T??i X???", "0900000003", AppRuntime.GenerateRandomLocation("V??? tr??", "TP.HCM"),
                id => new Motorbike(id, "59X1-12345", "Honda", "Vision", "?????"),
                "A1-12345");
            var d2 = await EnsureDriver(
                userRepo,
                "Ng?? T??i X???", "0900000005", AppRuntime.GenerateRandomLocation("V??? tr??", "TP.HCM"),
                id => new Car(id, "51H-78901", "Toyota", "Vios", "Tr???ng", 4),
                "B2-54321");
            var d3 = await EnsureDriver(
                userRepo,
                "Ho??ng T??i X???", "0900000006", AppRuntime.GenerateRandomLocation("V??? tr??", "TP.HCM"),
                id => new Motorbike(id, "59Y2-67890", "Yamaha", "Sirius", "??en"),
                "A1-67890");

            var existingTrips = await tripRepo.GetAll();
            if (existingTrips.Count > 0) return;

            var motorRule = await fareRepo.GetByVehicleType(VehicleType.Motorbike)
                ?? throw new InvalidOperationException("Không tìm thấy cấu hình giá xe máy.");
            var carRule = await fareRepo.GetByVehicleType(VehicleType.Car)
                ?? throw new InvalidOperationException("Không tìm thấy cấu hình giá ô tô.");

            d1.SetAvailable();
            d2.SetAvailable();
            d3.SetAvailable();
            await userRepo.Update(d1);
            await userRepo.Update(d2);
            await userRepo.Update(d3);

            var t1 = new Trip(p1.Id, motorRule.Id, q1, q5, VehicleType.Motorbike, 3.5, 50000);
            await tripRepo.Add(t1);

            var t2 = new Trip(p2.Id, carRule.Id, q3, q7, VehicleType.Car, 8.2, 120000);
            await tripRepo.Add(t2);

            var t3 = new Trip(p3.Id, motorRule.Id, phuNhuan, tanBinh, VehicleType.Motorbike, 4.1, 60000);
            await tripRepo.Add(t3);

            var t4 = new Trip(p1.Id, motorRule.Id, q5, q1, VehicleType.Motorbike, 2.2, 35000);
            t4.CancelTrip("Hành khách đổi ý");
            await tripRepo.Add(t4);

            var t5 = new Trip(p2.Id, carRule.Id, tanBinh, q1, VehicleType.Car, 6.4, 100000);
            await tripRepo.Add(t5);
        }

        private static async Task<Passenger> EnsurePassenger(
            IUserService userService,
            IUserRepository userRepo,
            string name,
            string phone)
        {
            var existing = await userRepo.GetByPhone(phone);
            if (existing is Passenger p) return p;
            if (existing != null) throw new InvalidOperationException($"Số điện thoại '{phone}' đã dùng cho role khác.");
            return await userService.RegisterPassenger(name, phone, "123456");
        }

        private static async Task<Driver> EnsureDriver(
            IUserRepository userRepo,
            string name,
            string phone,
            DomainLocation location,
            Func<Guid, Vehicle> vehicleFactory,
            string license)
        {
            var existing = await userRepo.GetByPhone(phone);
            if (existing is Driver d)
            {
                if (d.Status != DriverStatus.Available)
                {
                    try
                    {
                        d.SetAvailable();
                        await userRepo.Update(d);
                    }
                    catch
                    {
                    }
                }
                return d;
            }

            if (existing != null) throw new InvalidOperationException($"Số điện thoại '{phone}' đã dùng cho role khác.");

            var driverId = Guid.NewGuid();
            var vehicle = vehicleFactory(driverId);
            var driver = new Driver(driverId, name, phone, "123456", true, vehicle, location, license);
            await userRepo.Add(driver);
            driver.SetAvailable();
            await userRepo.Update(driver);
            return driver;
        }
    }
}
