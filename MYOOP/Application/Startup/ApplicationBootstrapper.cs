using MYOOP.Presentation.Common.MapComponent;
using OOP.Application.Services;
using OOP.Application.Services.Implementations;
using OOP.Application.Services.Interfaces;
using OOP.Domain.Entities;
using OOP.Domain.Interfaces;
using OOP.Infrastructure;
using OOP.Infrastructure.Map;
using OOP.Infrastructure.Repositories;
using OOP.Infrastructure.Storage;
using OOP.Presentation;
using OOP.Presentation.Shells;

namespace OOP
{
    internal static class ApplicationBootstrapper
    {
        internal static void Run()
        {
            var logger = Logger.Instance;
            logger.Info("=== ứng dụng khởi động ===");

            var config = ConfigService.Instance;
            logger.Info($"Config loaded: Simulation={config.Simulation.Enabled}, Timeout={config.TripTimeout.SearchTimeoutSeconds}s");

            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);

            MapControl.InitializeMapProvider();
            logger.Info("Map provider initialized");

            string dataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            try
            {
                Directory.CreateDirectory(dataPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Không thể tạo thư mục lưu dữ liệu tại:\n{dataPath}\n\nLỗi: {ex.Message}\n\n" +
                    "Vui lòng chạy ứng dụng với quyền Administrator.",
                    "Lỗi Quyền Truy Cập",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            var storage = new JsonStorage(dataPath);

            IUserRepository userRepo = new ThreadSafeUserRepository(new UserRepository(storage));
            ITripRepository tripRepo = new ThreadSafeTripRepository(new TripRepository(storage));
            IFareRepository fareRepo = new FareRuleRepository(storage);
            IRatingRepository ratingRepo = new RatingRepository(storage);
            IPaymentRepository paymentRepo = new PaymentRepository(storage);

            IMapRouteProvider mapProvider = new MapRouteProvider();
            IRouteService routeService = new RouteService(mapProvider);

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

            var eventDispatcher = new EventDispatcher(userRepo, tripRepo, matchingService, notificationService);

            ITripService tripService = new TripService(
                tripRepo,
                userRepo,
                fareRepo,
                fareRuleService,
                paymentService,
                matchingService,
                notificationService,
                routeService,
                eventDispatcher
            );

            var simulationService = new SimulationService(
                userRepo,
                tripRepo,
                notificationService,
                tripService,
                routeService
            );

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "OOP-App/1.0");

            try
            {
                fareRepo.EnsureSeeded().GetAwaiter().GetResult();
                AppDataSeeder.SeedAsync(tripRepo, userRepo, fareRepo, userService).GetAwaiter().GetResult();
            }
            catch (System.Threading.SynchronizationLockException ex)
            {
                MessageBox.Show($"Lỗi đồng bộ khi khởi tạo dữ liệu:\n{ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khởi tạo dữ liệu:\n{ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Func<Passenger, Form> passengerDashboardFactory = p =>
                new PassengerShell(
                    p,
                    tripService,
                    userService,
                    userRepo,
                    ratingService,
                    notificationService,
                    httpClient,
                    routeService,
                    fareRuleService
                );

            Func<Driver, Form> driverDashboardFactory = d =>
                new DriverShell(
                    d,
                    tripService,
                    userService,
                    userRepo,
                    routeService,
                    notificationService,
                    simulationService,
                    fareRuleService
                );

            var adminDashboardFactory = (Admin a) => new AdminShell(a, adminService);

            Func<LoginForm> loginFormFactory = () => new LoginForm(
                userService,
                passengerDashboardFactory,
                driverDashboardFactory,
                adminDashboardFactory);

            Func<RegisterForm> registerFormFactory = () => new RegisterForm(userService);

            var simulationTimer = CreateSimulationTimer(simulationService);
            simulationTimer.Start();

            var timeoutTimer = CreateTimeoutTimer(tripService);
            timeoutTimer.Start();

            System.Windows.Forms.Application.Run(
                new MainForm(
                    loginFormFactory,
                    registerFormFactory,
                    userService,
                    passengerDashboardFactory,
                    driverDashboardFactory
                )
            );
        }

        private static System.Windows.Forms.Timer CreateSimulationTimer(ISimulationService simulationService)
        {
            var timer = new System.Windows.Forms.Timer { Interval = 2000 };
            timer.Tick += async (_, _) =>
            {
                try
                {
                    if (!AppRuntime.SimulationConfig.Enabled) return;
                    await simulationService.UpdateDriverLocations();
                }
                catch (System.Threading.SynchronizationLockException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Simulation] SynchronizationLockException: {ex.Message}\n{ex.StackTrace}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Simulation] UpdateDriverLocations error: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
                }
            };
            return timer;
        }

        private static System.Windows.Forms.Timer CreateTimeoutTimer(ITripService tripService)
        {
            var timer = new System.Windows.Forms.Timer { Interval = 5000 };
            timer.Tick += async (_, _) =>
            {
                try
                {
                    await tripService.ExpireSearchingTrips(AppRuntime.GetTripTimeout());
                }
                catch (System.Threading.SynchronizationLockException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Timeout] SynchronizationLockException: {ex.Message}\n{ex.StackTrace}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Timeout] ExpireSearchingTrips error: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
                }
            };
            return timer;
        }
    }
}
