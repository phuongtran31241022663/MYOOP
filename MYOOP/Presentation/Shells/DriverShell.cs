using OOP.Application.Services;
using OOP.Application.Services.Interfaces;
using OOP.Domain.Entities;
using OOP.Domain.Enums;
using OOP.Domain.Interfaces;
using OOP.Presentation.BaseForms;
using OOP.Presentation.Common.Theme;
using OOP.Presentation.Screens.Driver;

namespace OOP.Presentation
{
    /// <summary>
    /// Shell duy nhất cho Driver — thay thế DriverDashboardForm.
    ///
    /// Layout:
    ///   ┌─────────────────────────────────────────────┐
    ///   │  Header (tên + status pill + toggle online) │
    ///   ├─────────────────────────────────────────────┤
    ///   │                                             │
    ///   │           Content Area (screens)            │
    ///   │                                             │
    ///   ├─────────────────────────────────────────────┤
    ///   │   Dashboard | Bản đồ | Lịch sử | Cá nhân   │
    ///   └─────────────────────────────────────────────┘
    ///
    /// Khác với PassengerShell:
    /// - Notification (accept/reject request) hiện ngay trong DashboardScreen
    /// - Khi accept → Shell.OnTripAccepted() chuyển sang MapTripScreen
    /// - Poll timer cũng do Shell quản lý
    /// </summary>
    public class DriverShell : BaseDashboardForm
    {
        // ── Dependencies ──────────────────────────────────────────────────────
        public readonly Driver Driver;
        private readonly ITripService _tripService;
        private readonly IUserService _userService;
        private readonly IUserRepository _userRepo;
        private readonly IRouteService _routeService;
        private readonly INotificationService _notification;
        private readonly ISimulationService _simulationService;
        private readonly IFareService _fareService;

        // ── Navigation ────────────────────────────────────────────────────────
        public ScreenNavigator Nav = null!;

        // ── Screens ───────────────────────────────────────────────────────────
        private DriverDashboardScreen _dashboardScreen = null!;
        private DriverActiveTripScreen _activeTripScreen = null!;
        private DriverHistoryScreen _historyScreen = null!;
        private DriverProfileScreen _profileScreen = null!;

        // ── Header controls ───────────────────────────────────────────────────
        private Label _lblTitle = null!;
        private Panel _pnlOnlineToggle = null!;
        private Label _lblOnlineStatus = null!;

        // ── Bottom nav ────────────────────────────────────────────────────────
        private Button _btnNavDashboard = null!;
        private Button _btnNavMap = null!;
        private Button _btnNavHistory = null!;
        private Button _btnNavProfile = null!;

        // ── Shared state ──────────────────────────────────────────────────────
        public Trip? CurrentTrip { get; private set; }
        private bool _onlineToggleState = false;

        private readonly System.Windows.Forms.Timer _refreshTimer = new() { Interval = 3000 };
        private readonly HashSet<Guid> _notifiedTripIds = new();

        // ── Screen keys ───────────────────────────────────────────────────────
        public const string KEY_DASHBOARD = "dashboard";
        public const string KEY_MAP = "map";
        public const string KEY_HISTORY = "history";
        public const string KEY_PROFILE = "profile";

        // ─────────────────────────────────────────────────────────────────────
        public DriverShell(
            Driver driver,
            ITripService tripService,
            IUserService userService,
            IUserRepository userRepo,
            IRouteService routeService,
            INotificationService notification,
            ISimulationService simulationService,
            IFareService fareService)
        {
            Driver = driver ?? throw new ArgumentNullException(nameof(driver));
            _tripService = tripService;
            _userService = userService;
            _userRepo = userRepo;
            _routeService = routeService;
            _notification = notification;
            _simulationService = simulationService;
            _fareService = fareService;

            Text = $"TX – {Driver.Name}";
            Size = new Size(1060, 700);
            MinimumSize = new Size(820, 560);

            BuildShell();
            WireUpEvents();
        }

        // ── Shell construction ────────────────────────────────────────────────

        private void BuildShell()
        {
            var header = BuildHeader();

            var contentHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.PageBg
            };

            var bottomNav = BuildBottomNav();

            Controls.Add(contentHost);
            Controls.Add(bottomNav);
            Controls.Add(header);

            Nav = new ScreenNavigator(contentHost);

            _dashboardScreen = new DriverDashboardScreen(this, _tripService, _userService, _simulationService);
            _activeTripScreen = new DriverActiveTripScreen(this, _tripService, _routeService, _userService, _simulationService, _fareService);
            _historyScreen = new DriverHistoryScreen(Driver.Id, _tripService, _userRepo, _fareService);
            _profileScreen = new DriverProfileScreen(Driver, _userService);

            Nav.Register(KEY_DASHBOARD, _dashboardScreen);
            Nav.Register(KEY_MAP, _activeTripScreen);
            Nav.Register(KEY_HISTORY, _historyScreen);
            Nav.Register(KEY_PROFILE, _profileScreen);

            Nav.ScreenChanged += OnScreenChanged;
        }

        private Panel BuildHeader()
        {
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 58,
                BackColor = AppTheme.SidebarDark,
                Padding = new Padding(16, 0, 16, 0)
            };

            _lblTitle = new Label
            {
                Text = Driver.Name,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Left,
                Width = 200,
                TextAlign = ContentAlignment.MiddleLeft
            };

            // Toggle Online/Inactive
            _pnlOnlineToggle = new Panel
            {
                Width = 120,
                Dock = DockStyle.Right,
                Cursor = Cursors.Hand
            };
            _lblOnlineStatus = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            _pnlOnlineToggle.Controls.Add(_lblOnlineStatus);
            _pnlOnlineToggle.Click += async (_, _) => await ToggleOnline();
            _lblOnlineStatus.Click += async (_, _) => await ToggleOnline();

            header.Controls.Add(_pnlOnlineToggle);
            header.Controls.Add(_lblTitle);
            return header;
        }

        private Panel BuildBottomNav()
        {
            var nav = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 62,
                BackColor = AppTheme.SidebarBg
            };
            nav.Paint += (s, e) =>
            {
                using var pen = new Pen(AppTheme.SidebarHover);
                e.Graphics.DrawLine(pen, 0, 0, nav.Width, 0);
            };

            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1 };
            for (int i = 0; i < 4; i++)
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));

            _btnNavDashboard = MakeNavButton("🏠", "Bảng điều khiển");
            _btnNavMap = MakeNavButton("🗺️", "Bản đồ");
            _btnNavHistory = MakeNavButton("📋", "Lịch sử");
            _btnNavProfile = MakeNavButton("👤", "Cá nhân");

            _btnNavDashboard.Click += async (_, _) => await Nav.NavigateTo(KEY_DASHBOARD);
            _btnNavMap.Click += async (_, _) => await Nav.NavigateTo(KEY_MAP);
            _btnNavHistory.Click += async (_, _) => await Nav.NavigateTo(KEY_HISTORY);
            _btnNavProfile.Click += async (_, _) => await Nav.NavigateTo(KEY_PROFILE);

            layout.Controls.Add(_btnNavDashboard, 0, 0);
            layout.Controls.Add(_btnNavMap, 1, 0);
            layout.Controls.Add(_btnNavHistory, 2, 0);
            layout.Controls.Add(_btnNavProfile, 3, 0);
            nav.Controls.Add(layout);
            return nav;
        }

        private static Button MakeNavButton(string icon, string label)
        {
            var btn = new Button
            {
                Text = $"{icon}\n{label}",
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 7.5f),
                ForeColor = Color.FromArgb(160, 180, 210),
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.MouseEnter += (_, _) => btn.BackColor = AppTheme.SidebarHover;
            btn.MouseLeave += (_, _) => btn.BackColor = Color.Transparent;
            return btn;
        }

        // ── Events ────────────────────────────────────────────────────────────

        private void WireUpEvents()
        {
            _notification.OnTripUpdated += OnTripNotified;
            _notification.OnDriverNotified += OnDriverNotified;

            Load += async (_, _) =>
            {
                _onlineToggleState = Driver.Status != DriverStatus.Inactive;
                UpdateOnlineToggleUI();
                await Nav.NavigateTo(KEY_DASHBOARD);

                // Restore nếu có chuyến dở
                await RestoreActiveTrip();

                _refreshTimer.Tick += async (_, _) => await RefreshDashboard();
                _refreshTimer.Start();
            };

            FormClosing += async (s, e) =>
            {
                if (CurrentTrip != null)
                {
                    MessageBox.Show("Bạn đang có chuyến đi. Vui lòng hoàn thành trước.",
                        "Không thể đóng", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    e.Cancel = true;
                    return;
                }

                _refreshTimer.Stop();
                await SetInactiveOnExit();
            };

            FormClosed += (_, _) =>
            {
                _notification.OnTripUpdated -= OnTripNotified;
                _notification.OnDriverNotified -= OnDriverNotified;
            };
        }

        private void OnTripNotified(Guid tripId, string msg)
        {
            if (InvokeRequired) { BeginInvoke(() => OnTripNotified(tripId, msg)); return; }
            _dashboardScreen.AddLog($"[{DateTime.Now:HH:mm}] {msg}");
        }

        private void OnDriverNotified(Guid driverId, string msg)
        {
            if (driverId != Driver.Id) return;
            if (InvokeRequired) { BeginInvoke(() => OnDriverNotified(driverId, msg)); return; }
            _dashboardScreen.AddLog($"[{DateTime.Now:HH:mm}] {msg}");
        }

        private void OnScreenChanged(string key)
        {
            Color active = Color.White;
            Color inactive = Color.FromArgb(160, 180, 210);
            _btnNavDashboard.ForeColor = key == KEY_DASHBOARD ? active : inactive;
            _btnNavMap.ForeColor = key == KEY_MAP ? active : inactive;
            _btnNavHistory.ForeColor = key == KEY_HISTORY ? active : inactive;
            _btnNavProfile.ForeColor = key == KEY_PROFILE ? active : inactive;
        }

        // ── Shared state API ─────────────────────────────────────────────────

        /// <summary>
        /// DashboardScreen gọi sau khi tài xế accept chuyến.
        /// Shell chuyển sang MapTripScreen.
        /// </summary>
        public async Task OnTripAccepted(Trip trip)
        {
            SetCurrentTrip(trip);
            await Nav.NavigateTo(KEY_MAP, trip);
            _btnNavMap.Text = "🗺️\nBản đồ ●";
            _btnNavMap.ForeColor = AppTheme.Success;
        }

        /// <summary>Gọi khi chuyến hoàn thành hoặc hủy.</summary>
        public void OnTripEnded()
        {
            SetCurrentTrip(null);
            _btnNavMap.Text = "🗺️\nBản đồ";
        }

        public void SetCurrentTrip(Trip? trip) => CurrentTrip = trip;

        // ── Online toggle ─────────────────────────────────────────────────────

        private async Task ToggleOnline()
        {
            _onlineToggleState = !_onlineToggleState;
            UpdateOnlineToggleUI();

            try
            {
                var newStatus = _onlineToggleState ? DriverStatus.Active : DriverStatus.Inactive;
                await _userService.UpdateDriverStatus(Driver.Id, newStatus);
                if (newStatus == DriverStatus.Active)
                    Driver.SetActive();
                else
                    Driver.SetInactive();
            }
            catch (Exception ex)
            {
                _onlineToggleState = !_onlineToggleState; // rollback
                UpdateOnlineToggleUI();
                MessageBox.Show($"Không thể đổi trạng thái: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateOnlineToggleUI()
        {
            if (_onlineToggleState)
            {
                _pnlOnlineToggle.BackColor = AppTheme.Success;
                _lblOnlineStatus.Text = "🟢  Online";
                _lblOnlineStatus.ForeColor = Color.White;
            }
            else
            {
                _pnlOnlineToggle.BackColor = AppTheme.SidebarHover;
                _lblOnlineStatus.Text = "⚫  Inactive";
                _lblOnlineStatus.ForeColor = Color.FromArgb(160, 180, 210);
            }
        }

        // ── Polling ───────────────────────────────────────────────────────────

        private async Task RestoreActiveTrip()
        {
            try
            {
                // Nếu driver đang có chuyến chưa xong → mở lại MapTripScreen
                var trips = await _tripService.GetActiveTripsForDriver(Driver.Id);
                var active = trips.FirstOrDefault(t =>
                    t.Status is TripStatus.Matched or TripStatus.Arrived or TripStatus.Started);

                if (active != null)
                    await OnTripAccepted(active);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DriverShell.RestoreActiveTrip] {ex.Message}");
            }
        }

        private async Task RefreshDashboard()
        {
            if (Nav.CurrentKey == KEY_DASHBOARD)
                await _dashboardScreen.RefreshAsync();
        }

        private async Task SetInactiveOnExit()
        {
            try
            {
                if (Driver.Status == DriverStatus.OnTrip)
                    Driver.ForceSetActive();
                Driver.SetInactive();
                await _userService.UpdateDriverStatus(Driver.Id, DriverStatus.Inactive);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SetInactiveOnExit] {ex.Message}");
            }
        }
    }
}
