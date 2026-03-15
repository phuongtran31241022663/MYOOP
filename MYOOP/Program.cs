using GMap.NET;
using GMap.NET.MapProviders;
using OOP.Application.Services;
using OOP.Application.Services.Implementations;
using OOP.Application.Services.Interfaces;
using OOP.Domain.Entities;
using OOP.Domain.Enums;
using OOP.Domain.Interfaces;
using OOP.Infrastructure.Storage;
using OOP.Infrastructure.Map;
using OOP.Infrastructure.Repositories;
using OOP.Presentation;
using OOP.Presentation.TripForms;
using DomainLocation = OOP.Domain.Entities.Location;


namespace OOP
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
            GMapProvider.UserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
        "(KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36";
            GMaps.Instance.Mode = AccessMode.ServerAndCache;
            // 1. Khởi tạo đường dẫn ngay tại thư mục chứa file .exe cho dễ kiếm
            string dataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");

            try
            {
                // Nếu chưa có thư mục Data thì tạo mới
                if (!Directory.Exists(dataPath))
                {
                    Directory.CreateDirectory(dataPath);
                }
            }
            catch (Exception ex)
            {
                // Nếu vẫn bị lỗi quyền, thông báo ngay để bạn biết đường chạy quyền Admin
                MessageBox.Show($"Không thể tạo thư mục lưu dữ liệu tại: {dataPath}\nLỗi: {ex.Message}",
                                "Lỗi Quyền Truy Cập", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            var storage = new JsonStorage(dataPath);

            // 2. Khởi tạo Repositories & Map Services
            IUserRepository userRepo = new UserRepository(storage);
            ITripRepository tripRepo = new TripRepository(storage);
            IFareRuleRepository fareRepo = new FareRuleRepository(storage);
            IRatingRepository ratingRepo = new RatingRepository(storage);
            IPaymentRepository paymentRepo = new PaymentRepository(storage);

            // Khởi tạo hệ thống Map đúng chuẩn (Provider -> Service)
            IMapRouteProvider mapProvider = new MapRouteProvider();
            IRouteService routeService = new RouteService(mapProvider);

            // 3. Khởi tạo Application Services
            var userService = new UserService(userRepo);
            var authService = new AuthService(userRepo);
            var paymentService = new PaymentService(paymentRepo, fareRepo);
            var fareRuleService = new FareRuleService(fareRepo);
            var notificationService = new NotificationService(userRepo, tripRepo);
            var tripNotificationSubscriber =
    new TripNotificationSubscriber(notificationService, tripRepo);

            notificationService.OnTripUpdated +=
                async (tripId, message) =>
                    await tripNotificationSubscriber.Handle(tripId, message);
            var matchingService = new DriverMatchingService(userRepo, routeService);
            var adminService = new AdminService(userRepo, tripRepo, fareRepo);
            var ratingService = new RatingService(ratingRepo, userRepo, tripRepo);
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "RideGo-App/1.0");
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

            // 4. Seed Data (Chạy đồng bộ)
            SeedData(authService, tripRepo, userRepo).GetAwaiter().GetResult();

            // 5. Định nghĩa các Factories cho UI
            Func<Passenger, ITripService, Form> requestTripFactory = (p, s) =>
     new RequestTripForm(p.Id, s, routeService, fareRuleService, httpClient);

            Func<Passenger, ITripService, Form> tripHistoryFactory = (p, s) =>
                new TripHistoryForm(p.Id, s);

            Func<Passenger, IRatingService, ITripService, Form> ratingFormFactory = (p, r, s) =>
     new RatingForm(p, r, s);

            // Factory cho Dashboard Người dùng
            Func<Passenger, Form> passengerDashboardFactory = p =>
     new PassengerDashboardForm(
         p,
         tripService,
         ratingService,
         requestTripFactory,
         tripHistoryFactory,
         ratingFormFactory
     );

            // Factory cho Dashboard Tài xế
            Func<Driver, Form> driverDashboardFactory = d =>
                new DriverDashboardForm(d, tripService, userService);

            // Factory cho Dashboard Admin
            Func<Admin, Form> adminDashboardFactory = a =>
                new AdminDashboardForm(a, adminService);

            // Factory tạo LoginForm
            Func<LoginForm> loginFormFactory = () => new LoginForm(
                authService,
                passengerDashboardFactory,
                driverDashboardFactory,
                adminDashboardFactory);

            // Factory tạo RegisterForm
            Func<RegisterForm> registerFormFactory = () => new RegisterForm(authService);

            // 6. Chạy ứng dụng từ MainForm
            System.Windows.Forms.Application.Run(new MainForm(loginFormFactory, registerFormFactory));
        }

        private static async Task SeedData(AuthService authService, ITripRepository tripRepo, IUserRepository userRepo)
        {
            // 1. ADMIN
            string adminPhone = "0000000000";
            if (!await userRepo.ExistsByPhone(adminPhone))
            {
                var admin = new Admin(Guid.NewGuid(), "Hệ Thống Admin", adminPhone,
                                      AuthService.HashPassword("admin123"), true);
                await userRepo.Add(admin);
            }

            // 2. CHỈ SEED DỮ LIỆU MẪU NẾU REPO TRỐNG
            var allUsers = await userRepo.GetAll();
            // Nếu chỉ có mỗi Admin (count == 1) thì mới tạo thêm data mẫu
            if (allUsers.Count() <= 1)
            {
                var hcm = new DomainLocation("Default", "TP.HCM", 10.762622, 106.660172);
                var q1 = new DomainLocation("Quận 1", "Bến Thành", 10.772, 106.698);
                var q5 = new DomainLocation("Quận 5", "Chợ Lớn", 10.754, 106.664);

                var bike = new Motorbike(Guid.NewGuid(), "51F-123.45", "Honda", "Vision", "Đỏ");

                // Đăng ký qua service để tự động băm mật khẩu
                var p1 = await authService.RegisterPassenger("Nguyễn Văn A", "0900000001", "123456");
                var d1 = await authService.RegisterDriver("Lê Tài Xế", "0900000003", "123456", bike, hcm, "A1-12345");

                // Tạo Trip mẫu cho có dữ liệu hiển thị Chart/History
                var trip1 = new Trip(Guid.NewGuid(), p1.Id, q1, q5, VehicleType.Motorbike, 3.5);
                trip1.AssignDriver(d1.Id);
                trip1.MarkArrived();
                trip1.CompleteTrip(3.5, 12, 15000m);
                await tripRepo.Add(trip1);
            }
        }
    }
}