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
    /// Shell duy nhất cho Driver.
    ///
        /// Layout chuẩn:
        ///   ┌─────────────────────────────────────────────┐
        ///   │  Header (Top, 56px): tên + status toggle     │
        ///   ├──────────────┬──────────────────────────────┤
        ///   │  Sidebar     │          Content              │
        ///   │  (200px)     │          (Fill)               │
        ///   └──────────────┴──────────────────────────────┘
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
        private Panel _pnlActiveToggle = null!;
        private Label _lblActiveStatus = null!;
        private Button _btnLogout = null!;

        // ── Sidebar nav buttons ───────────────────────────────────────────────
        private Button _btnNavDashboard = null!;
        private Button _btnNavMap = null!;
        private Button _btnNavHistory = null!;
        private Button _btnNavProfile = null!;

        // ── Shared state ──────────────────────────────────────────────────────
        public Trip? CurrentTrip { get; private set; }
        private bool _activeToggleState = false;

        private readonly System.Windows.Forms.Timer _refreshTimer = new() { Interval = 3000 };
        private readonly HashSet<Guid> _notifiedTripIds = new();
        private readonly HashSet<string> _recentNotifications = new();
        private DateTime _lastNotificationTime = DateTime.MinValue;

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
            MaximizeBox = true;

            BuildShell();
            WireUpEvents();
        }

        // ── Shell construction ────────────────────────────────────────────────

        private void BuildShell()
        {
            BuildHeader();
            Nav = new ScreenNavigator(ContentPanel);
            BuildSidebar();

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

        private void BuildHeader()
        {
            HeaderPanel.Padding = new Padding(16, 0, 16, 0);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));

            _lblTitle = new Label
            {
                Text = Driver.Name,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _pnlActiveToggle = new Panel
            {
                Dock = DockStyle.Fill,
                Cursor = Cursors.Hand
            };
            _lblActiveStatus = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            _pnlActiveToggle.Controls.Add(_lblActiveStatus);
            _pnlActiveToggle.Click += async (_, _) => await ToggleActive();
            _lblActiveStatus.Click += async (_, _) => await ToggleActive();

            _btnLogout = new Button
            {
                Text = "← Đăng xuất",
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                BackColor = AppTheme.Danger,
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold)
            };
            _btnLogout.FlatAppearance.BorderSize = 0;
            _btnLogout.Click += OnLogoutClicked;

            layout.Controls.Add(_lblTitle, 0, 0);
            layout.Controls.Add(_pnlActiveToggle, 1, 0);
            layout.Controls.Add(_btnLogout, 2, 0);
            HeaderPanel.Controls.Add(layout);
        }

        private void BuildSidebar()
        {
            _btnNavDashboard = AddSidebarNav("🏠", "Bảng điều khiển", async (_, _) => await Nav.NavigateTo(KEY_DASHBOARD));
            _btnNavMap = AddSidebarNav("🗺️", "Bản đồ / Chuyến đi", async (_, _) => await Nav.NavigateTo(KEY_MAP));
            _btnNavHistory = AddSidebarNav("📋", "Lịch sử", async (_, _) => await Nav.NavigateTo(KEY_HISTORY));
            _btnNavProfile = AddSidebarNav("👤", "Cá nhân", async (_, _) => await Nav.NavigateTo(KEY_PROFILE));
        }

        // ── Events ────────────────────────────────────────────────────────────

        private void OnLogoutClicked(object? sender, EventArgs e)
        {
            if (CurrentTrip != null)
            {
                MessageBox.Show("Bạn đang có chuyến đi. Vui lòng hoàn thành trước.",
                    "Không thể đăng xuất", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (FormHelper.ShowConfirm("Bạn có chắc muốn đăng xuất?"))
                Close();
        }

        private void WireUpEvents()
        {
            _notification.OnTripUpdated += OnTripNotified;
            _notification.OnDriverNotified += OnDriverNotified;

            Load += async (_, _) =>
            {
                _activeToggleState = Driver.Status != DriverStatus.Offline;
                UpdateActiveToggleUI();
                await Nav.NavigateTo(KEY_DASHBOARD);
                await RestoreActiveTrip();

                _refreshTimer.Tick += async (_, _) => await RefreshDashboard();
                _refreshTimer.Start();
            };

            FormClosing += async (s, e) =>
            {
                if (CurrentTrip != null)
                {
                    FormHelper.ShowError("Bạn đang có chuyến đi. Vui lòng hoàn thành trước.", "Không thể đóng");
                    e.Cancel = true;
                    return;
                }
                _refreshTimer.Stop();
                await SetOfflineOnExit();
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

            var key = $"trip_{tripId}_{msg.GetHashCode()}";
            var now = DateTime.Now;
            if (_recentNotifications.Contains(key) && (now - _lastNotificationTime).TotalMilliseconds < 500) return;
            _recentNotifications.Add(key);
            _lastNotificationTime = now;
            if (_recentNotifications.Count > 100) _recentNotifications.Clear();

            _dashboardScreen.AddLog($"[{DateTime.Now:HH:mm}] {msg}");
        }

        private void OnDriverNotified(Guid driverId, string msg)
        {
            if (driverId != Driver.Id) return;
            if (InvokeRequired) { BeginInvoke(() => OnDriverNotified(driverId, msg)); return; }

            var key = $"driver_{driverId}_{msg.GetHashCode()}";
            var now = DateTime.Now;
            if (_recentNotifications.Contains(key) && (now - _lastNotificationTime).TotalMilliseconds < 500) return;
            _recentNotifications.Add(key);
            _lastNotificationTime = now;
            if (_recentNotifications.Count > 100) _recentNotifications.Clear();

            _dashboardScreen.AddLog($"[{DateTime.Now:HH:mm}] {msg}");
        }

        private void OnScreenChanged(string key)
        {
            var activeBtn = key switch
            {
                KEY_DASHBOARD => _btnNavDashboard,
                KEY_MAP => _btnNavMap,
                KEY_HISTORY => _btnNavHistory,
                KEY_PROFILE => _btnNavProfile,
                _ => null
            };
            if (activeBtn != null) SetActiveNav(activeBtn);
        }

        // ── Shared state API ──────────────────────────────────────────────────

        public async Task OnTripAccepted(Trip trip)
        {
            SetCurrentTrip(trip);
            await Nav.NavigateTo(KEY_MAP, trip);
            _btnNavMap.Text = "🗺️  Bản đồ / Chuyến đi  ●";
        }

        public void OnTripEnded()
        {
            SetCurrentTrip(null);
            _btnNavMap.Text = "🗺️  Bản đồ / Chuyến đi";
        }

        public void SetCurrentTrip(Trip? trip) => CurrentTrip = trip;

        private async Task ToggleActive()
        {
            _activeToggleState = !_activeToggleState;
            UpdateActiveToggleUI();
            try
            {
                var newStatus = _activeToggleState ? DriverStatus.Available : DriverStatus.Offline;
                await _userService.UpdateDriverStatus(Driver.Id, newStatus);
                if (newStatus == DriverStatus.Available) Driver.SetAvailable();
                else Driver.SetOffline();
            }
            catch (Exception ex)
            {
                _activeToggleState = !_activeToggleState; // rollback
                UpdateActiveToggleUI();
                FormHelper.ShowError($"Không thể đổi trạng thái: {ex.Message}");
            }
        }

        private void UpdateActiveToggleUI()
        {
            if (_activeToggleState)
            {
                _pnlActiveToggle.BackColor = AppTheme.Success;
                _lblActiveStatus.Text = "● Active";
                _lblActiveStatus.ForeColor = Color.White;
            }
            else
            {
                _pnlActiveToggle.BackColor = AppTheme.SidebarHover;
                _lblActiveStatus.Text = "○ Offline";
                _lblActiveStatus.ForeColor = AppTheme.TextMuted;
            }
        }

        // ── Polling ───────────────────────────────────────────────────────────

        private async Task RestoreActiveTrip()
        {
            try
            {
                var trips = await _tripService.GetActiveTripsForDriver(Driver.Id);
                var active = trips.FirstOrDefault(t =>
                    t.Status is TripStatus.Matched or TripStatus.Arrived or TripStatus.Started);
                if (active != null) await OnTripAccepted(active);
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

        private async Task SetOfflineOnExit()
        {
            try
            {
                if (Driver.Status == DriverStatus.OnTrip) Driver.SetAvailable();
                Driver.SetOffline();
                await _userService.UpdateDriverStatus(Driver.Id, DriverStatus.Offline);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SetOfflineOnExit] {ex.Message}");
            }
        }
    }
}
