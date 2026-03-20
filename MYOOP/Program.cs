using OOP.Application.Services;
using OOP.Application.Services.Interfaces;
using OOP.Domain.Entities;
using OOP.Domain.Enums;
using OOP.Domain.Interfaces;
using OOP.Infrastructure.Map;
using OOP.Infrastructure.Repositories;
using OOP.Infrastructure.Storage;
using OOP.Presentation;
using OOP.Presentation.Map;
using OOP.Presentation.TripForms;
using DomainLocation = OOP.Domain.Entities.GeoLocation;

namespace OOP
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);

            MapControl.InitializeMapProvider();

            // ── Data directory ────────────────────────────────────────────────
            string dataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");

            try
            {
                Directory.CreateDirectory(dataPath); // no-op if already exists
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Không thể tạo thư mục lưu dữ liệu tại:\n{dataPath}\n\nLỗi: {ex.Message}\n\n" +
                    "Vui lòng chạy ứng dụng với quyền Administrator.",
                    "Lỗi Quyền Truy Cập",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return; // cannot run safely without the data directory
            }

            var storage = new JsonStorage(dataPath);

            // ── Repositories ──────────────────────────────────────────────────
            IUserRepository userRepo = new ThreadSafeUserRepository(new UserRepository(storage));
            ITripRepository tripRepo = new ThreadSafeTripRepository(new TripRepository(storage));
            IFareRepository fareRepo = new FareRuleRepository(storage);
            IRatingRepository ratingRepo = new RatingRepository(storage);
            IPaymentRepository paymentRepo = new PaymentRepository(storage);

            // ── Map ───────────────────────────────────────────────────────────
            IMapRouteProvider mapProvider = new MapRouteProvider();
            IRouteService routeService = new RouteService(mapProvider);

            // ── Application services ──────────────────────────────────────────
            var userService = new UserService(userRepo);
            var paymentService = new PaymentService(paymentRepo, fareRepo);
            var fareRuleService = new FareService(fareRepo);
            var notificationService = new NotificationService(userRepo, tripRepo);

            var tripNotificationSubscriber =
                new TripNotificationSubscriber(notificationService, tripRepo);

            notificationService.OnTripUpdated +=
                async (tripId, message) =>
                    await tripNotificationSubscriber.Handle(tripId, message);

            var matchingService = new DriverMatchingService(userRepo, tripRepo, routeService);
            var adminService = new AdminService(userRepo, tripRepo, fareRepo, paymentRepo);
            var ratingService = new RatingService(ratingRepo, userRepo, tripRepo);
            ITripService tripService = new TripService(
              tripRepo,
              userRepo,
              fareRepo,
              fareRuleService,
              paymentService,
              matchingService,
              notificationService,
              routeService
          );
            var simulationService = new SimulationService(userRepo, tripRepo, notificationService, tripService, routeService);

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "RideGo-App/1.0");

            // ── Seed data (sync before UI starts) ─────────────────────────────
            SeedData(tripRepo, userRepo, fareRepo, userService).GetAwaiter().GetResult();

            // ── UI factories ──────────────────────────────────────────────────
            Func<Passenger, ITripService, Form> requestTripFactory = (p, s) =>
                new RequestTripForm(p.Id, s, routeService, fareRuleService, httpClient);

            Func<Passenger, ITripService, Form> tripHistoryFactory = (p, s) =>
                new TripHistoryForm(p.Id, s, userRepo);

            Func<Passenger, IRatingService, ITripService, Form> ratingFormFactory = (p, r, s) =>
                new RatingForm(p, r, s);

            Func<Passenger, Form> passengerDashboardFactory = p =>
                new PassengerDashboardForm(
                    p,
                    userRepo,
                    tripService,
                    ratingService,
                    userService,
                    notificationService,
                    requestTripFactory,
                    tripHistoryFactory,
                    ratingFormFactory
                );

            Func<Driver, Form> driverDashboardFactory = d =>
                new DriverDashboardForm(d, tripService, userService, userRepo, routeService, notificationService);

            Func<Admin, Form> adminDashboardFactory = a =>
                new AdminDashboardForm(a, adminService);

            Func<LoginForm> loginFormFactory = () => new LoginForm(
                userService,
                passengerDashboardFactory,
                driverDashboardFactory,
                adminDashboardFactory);

            Func<RegisterForm> registerFormFactory = () => new RegisterForm(userService);

            var simulationTimer = new System.Windows.Forms.Timer { Interval = 2000 };
            simulationTimer.Tick += async (_, _) =>
            {
                try
                {
                    if (!SimulationConfig.Enabled) return;
                    await simulationService.UpdateDriverLocations();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Simulation] UpdateDriverLocations error: {ex.Message}");
                }
            };
            simulationTimer.Start();

            var timeoutTimer = new System.Windows.Forms.Timer { Interval = 5000 };
            timeoutTimer.Tick += async (_, _) =>
            {
                try
                {
                    await tripService.ExpireSearchingTrips(TripTimeoutConfig.SearchTimeout);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Timeout] ExpireSearchingTrips error: {ex.Message}");
                }
            };
            timeoutTimer.Start();

            System.Windows.Forms.Application.Run(
                new MainForm(
                    loginFormFactory,
                    registerFormFactory,
                    userService,
                    passengerDashboardFactory,
                    driverDashboardFactory));
        }

        internal static class SimulationConfig
        {
            public static bool Enabled { get; set; } = true;

            // Demo map bounds for Ho Chi Minh City area
            public static double MinLat { get; set; } = 10.7200;
            public static double MaxLat { get; set; } = 10.8500;
            public static double MinLng { get; set; } = 106.6000;
            public static double MaxLng { get; set; } = 106.7800;

            private static readonly Random Random = new();

            public static DomainLocation GenerateRandomLocation(string name, string address)
            {
                var lat = MinLat + (MaxLat - MinLat) * Random.NextDouble();
                var lng = MinLng + (MaxLng - MinLng) * Random.NextDouble();
                return new DomainLocation(name, address, lat, lng);
            }
        }

        internal static class TripTimeoutConfig
        {
            // Demo timeout for searching trips
            public static TimeSpan SearchTimeout { get; set; } = TimeSpan.FromSeconds(60);
        }

        private static async Task SeedData(
            ITripRepository tripRepo,
            IUserRepository userRepo,
            IFareRepository fareRepo,
            UserService userService)
        {
            // ── Admin ─────────────────────────────────────────────────────────
            const string adminPhone = "0000000000";
            if (!await userRepo.ExistsByPhone(adminPhone))
            {
                var admin = new Admin(
                    Guid.NewGuid(), "Hệ Thống Admin", adminPhone,
                    "admin123"); // Admin luôn active
                await userRepo.Add(admin);
            }

            var hcm = new DomainLocation("Trung tâm", "TP. Hồ Chí Minh", 10.7769, 106.7009);
            var q1 = new DomainLocation("Quận 1", "Bến Thành", 10.7720, 106.6980);
            var q3 = new DomainLocation("Quận 3", "Võ Thị Sáu", 10.7846, 106.6844);
            var q5 = new DomainLocation("Quận 5", "Chợ Lớn", 10.7540, 106.6640);
            var q7 = new DomainLocation("Quận 7", "Phú Mỹ Hưng", 10.7287, 106.7219);
            var tanBinh = new DomainLocation("Tân Bình", "Sân bay Tân Sơn Nhất", 10.8132, 106.6620);
            var phuNhuan = new DomainLocation("Phú Nhuận", "Ngã 4 Phú Nhuận", 10.7995, 106.6792);

            var p1 = await EnsurePassenger(userService, userRepo, "Nguyễn Văn A", "0900000001");
            var p2 = await EnsurePassenger(userService, userRepo, "Trần Thị B", "0900000002");
            var p3 = await EnsurePassenger(userService, userRepo, "Phạm Minh C", "0900000004");

            // Create drivers with random positions in demo area
            var d1 = await EnsureDriver(
                userService, userRepo,
                "Lê Tài Xế", "0900000003", SimulationConfig.GenerateRandomLocation("Vị trí", "TP.HCM"),
                new Motorbike(Guid.NewGuid(), "59X1-12345", "Honda", "Vision", "Đỏ"),
                "A1-12345");

            var d2 = await EnsureDriver(
                userService, userRepo,
                "Ngô Tài Xế", "0900000005", SimulationConfig.GenerateRandomLocation("Vị trí", "TP.HCM"),
                new Car(Guid.NewGuid(), "51H-78901", "Toyota", "Vios", "Trắng", 4),
                "B2-54321");

            var d3 = await EnsureDriver(
                userService, userRepo,
                "Hoàng Tài Xế", "0900000006", SimulationConfig.GenerateRandomLocation("Vị trí", "TP.HCM"),
                new Motorbike(Guid.NewGuid(), "59Y2-67890", "Yamaha", "Sirius", "Đen"),
                "A1-67890");



            var existingTrips = await tripRepo.GetAll();
            if (existingTrips.Count > 0) return;

            var motorRule = await fareRepo.GetByVehicleType(VehicleType.Motorbike)
                ?? throw new InvalidOperationException("Không tìm thấy cấu hình giá xe máy.");
            var carRule = await fareRepo.GetByVehicleType(VehicleType.Car)
                ?? throw new InvalidOperationException("Không tìm thấy cấu hình giá ô tô.");

            // Before assigning any driver to a trip, bring them online first
            d1.SetAvailable();
            d2.SetAvailable();
            d3.SetAvailable();

            // Now AssignDriver won't throw
            var t1 = new Trip(p1.Id, motorRule.Id, q1, q5, VehicleType.Motorbike, 3.5);
            t1.AssignDriver(d1);
            t1.MarkArrived();
            t1.StartTrip();
            t1.CompleteTrip(3.5, 12, 15_000m);
            // Do NOT call await userRepo.Update(d1) here — leave drivers in their original saved state
            await tripRepo.Add(t1);

            var t2 = new Trip(p2.Id, carRule.Id, q3, q7, VehicleType.Car, 8.2);
            t2.AssignDriver(d2);
            t2.MarkArrived();
            t2.StartTrip();
            await tripRepo.Add(t2);

            var t3 = new Trip(p3.Id, motorRule.Id, phuNhuan, tanBinh, VehicleType.Motorbike, 4.1);
            t3.AssignDriver(d3);
            await tripRepo.Add(t3);

            var t4 = new Trip(p1.Id, motorRule.Id, q5, q1, VehicleType.Motorbike, 2.2);
            t4.CancelTrip("Hành khách đổi ý");
            await tripRepo.Add(t4);

            var t5 = new Trip(p2.Id, carRule.Id, tanBinh, q1, VehicleType.Car, 6.4);
            await tripRepo.Add(t5);
        }

        private static async Task<Passenger> EnsurePassenger(UserService userService, IUserRepository userRepo,
            string name,
            string phone)
        {
            var existing = await userRepo.GetByPhone(phone);
            if (existing is Passenger p) return p;
            if (existing != null) throw new InvalidOperationException($"Số điện thoại '{phone}' đã dùng cho role khác.");
            return await userService.RegisterPassenger(name, phone, "123456");
        }

        private static async Task<Driver> EnsureDriver(
            UserService userService,
            IUserRepository userRepo,
            string name,
            string phone,
            DomainLocation location,
            Vehicle vehicle,
            string license)
        {
            var existing = await userRepo.GetByPhone(phone);
            if (existing is Driver d) 
            {
                // Driver already exists (from previous run) — return existing instance.
                // Status is whatever was persisted from last run (could be Available or Busy).
                return d;
            }
            if (existing != null) throw new InvalidOperationException($"Số điện thoại '{phone}' đã dùng cho role khác.");
            
            // First-time creation: set driver to Available so they can receive trip assignments
            var driver = await userService.RegisterDriver(name, phone, "123456", vehicle, location, license);
            driver.SetAvailable(); 
            await userRepo.Update(driver);
            return driver;
        }
    }
}





