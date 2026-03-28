using OOP.Presentation.Common.Theme;
using OOP.Application.Services.Interfaces;
using OOP.Domain.Entities;
using OOP.Domain.Enums;
using OOP.Domain.Interfaces;
using OOP.Presentation.BaseForms;
using OOP.Presentation.Screens.Passenger;

namespace OOP.Presentation
{
    /// <summary>
    /// Shell duy nhất cho Passenger.
    ///
        /// Layout chuẩn:
        ///   ┌─────────────────────────────────────────────┐
        ///   │          Header (Top, 56px)                  │
        ///   ├──────────────┬──────────────────────────────┤
        ///   │  Sidebar     │          Content              │
        ///   │  (200px)     │          (Fill)               │
        ///   └──────────────┴──────────────────────────────┘
        /// </summary>
    public class PassengerShell : BaseDashboardForm
    {
        // ── Dependencies ──────────────────────────────────────────────────────
        private readonly Passenger _passenger;
        private readonly ITripService _tripService;
        private readonly IUserService _userService;
        private readonly IUserRepository _userRepo;
        private readonly IRatingService _ratingService;
        private readonly INotificationService _notification;
        private readonly HttpClient _http;
        private readonly IRouteService _routeService;
        private readonly IFareService _fareService;

        // ── Navigation ────────────────────────────────────────────────────────
        private ScreenNavigator _nav = null!;

        // ── Screens ───────────────────────────────────────────────────────────
        private PassengerHomeScreen _homeScreen = null!;
        private PassengerActiveTripScreen _activeTripScreen = null!;
        private PassengerHistoryScreen _historyScreen = null!;
        private PassengerProfileScreen _profileScreen = null!;
        private PassengerRatingScreen _ratingScreen = null!;

        // ── Header ────────────────────────────────────────────────────────────
        private Label _lblHeaderTitle = null!;
        private Button _btnLogout = null!;

        // ── Sidebar nav buttons ───────────────────────────────────────────────
        private Button _btnNavHome = null!;
        private Button _btnNavTrip = null!;
        private Button _btnNavHistory = null!;
        private Button _btnNavProfile = null!;
        private Button _btnNavRating = null!;

        // ── Shared state ──────────────────────────────────────────────────────
        public Trip? CurrentTrip { get; private set; }
        public Passenger Passenger => _passenger;
        public ScreenNavigator Nav => _nav;

        private readonly System.Windows.Forms.Timer _pollTimer = new() { Interval = 4000 };
        private readonly HashSet<string> _recentNotifications = new();
        private DateTime _lastNotificationTime = DateTime.MinValue;

        // ── Screen keys ───────────────────────────────────────────────────────
        public const string KEY_HOME = "home";
        public const string KEY_TRIP = "trip";
        public const string KEY_HISTORY = "history";
        public const string KEY_PROFILE = "profile";
        public const string KEY_RATING = "rating";

        // ─────────────────────────────────────────────────────────────────────
        public PassengerShell(
            Passenger passenger,
            ITripService tripService,
            IUserService userService,
            IUserRepository userRepo,
            IRatingService ratingService,
            INotificationService notification,
            HttpClient http,
            IRouteService routeService,
            IFareService fareService)
        {
            _passenger = passenger ?? throw new ArgumentNullException(nameof(passenger));
            _tripService = tripService ?? throw new ArgumentNullException(nameof(tripService));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _userRepo = userRepo ?? throw new ArgumentNullException(nameof(userRepo));
            _ratingService = ratingService ?? throw new ArgumentNullException(nameof(ratingService));
            _notification = notification ?? throw new ArgumentNullException(nameof(notification));
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _routeService = routeService ?? throw new ArgumentNullException(nameof(routeService));
            _fareService = fareService ?? throw new ArgumentNullException(nameof(fareService));

            Text = _passenger.Name;
            MaximizeBox = true;

            BuildShell();
            WireUpEvents();
        }

        // ── Shell construction ────────────────────────────────────────────────

        private void BuildShell()
        {
            BuildHeader();
            _nav = new ScreenNavigator(ContentPanel);
            BuildSidebar();

            _homeScreen = new PassengerHomeScreen(this, _tripService, _userService, _http, _routeService, _fareService);
            _activeTripScreen = new PassengerActiveTripScreen(this, _tripService);
            _historyScreen = new PassengerHistoryScreen(_passenger.Id, _tripService, _userRepo);
            _profileScreen = new PassengerProfileScreen(_passenger, _userService);
            _ratingScreen = new PassengerRatingScreen(this, _ratingService, _tripService);

            _nav.Register(KEY_HOME, _homeScreen);
            _nav.Register(KEY_TRIP, _activeTripScreen);
            _nav.Register(KEY_HISTORY, _historyScreen);
            _nav.Register(KEY_PROFILE, _profileScreen);
            _nav.Register(KEY_RATING, _ratingScreen);

            _nav.ScreenChanged += OnScreenChanged;
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
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));

            _lblHeaderTitle = new Label
            {
                Text = "Trang chủ",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var lblUser = new Label
            {
                Text = $"👤 {_passenger.Name}",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(200, 225, 255),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight
            };

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

            layout.Controls.Add(_lblHeaderTitle, 0, 0);
            layout.Controls.Add(lblUser, 1, 0);
            layout.Controls.Add(_btnLogout, 2, 0);
            HeaderPanel.Controls.Add(layout);
        }

        private void BuildSidebar()
        {
            _btnNavHome = AddSidebarNav("🏠", "Trang chủ", async (_, _) => await _nav.NavigateTo(KEY_HOME));
            _btnNavTrip = AddSidebarNav("🚗", "Chuyến đi", async (_, _) => await _nav.NavigateTo(KEY_TRIP));
            _btnNavHistory = AddSidebarNav("📋", "Lịch sử", async (_, _) => await _nav.NavigateTo(KEY_HISTORY));
            _btnNavRating = AddSidebarNav("⭐", "Đánh giá", async (_, _) => await _nav.NavigateTo(KEY_RATING));
            _btnNavProfile = AddSidebarNav("👤", "Tài khoản", async (_, _) => await _nav.NavigateTo(KEY_PROFILE));
        }

        // ── Events ────────────────────────────────────────────────────────────

        private void OnLogoutClicked(object? sender, EventArgs e)
        {
            if (FormHelper.ShowConfirm("Bạn có chắc muốn đăng xuất?"))
                Close();
        }

        private void WireUpEvents()
        {
            _notification.OnPassengerNotified += OnNotification;

            Load += async (_, _) =>
            {
                await _nav.NavigateTo(KEY_HOME);
                await RestoreActiveTrip();

                _pollTimer.Tick += async (_, _) => await PollTripStatus();
                _pollTimer.Start();
            };

            FormClosed += (_, _) =>
            {
                _pollTimer.Stop();
                _pollTimer.Dispose();
                _notification.OnPassengerNotified -= OnNotification;
            };
        }

        private void OnNotification(Guid id, string msg)
        {
            if (InvokeRequired) { BeginInvoke(() => OnNotification(id, msg)); return; }

            var key = $"passenger_{id}_{msg.GetHashCode()}";
            var now = DateTime.Now;
            if (_recentNotifications.Contains(key) && (now - _lastNotificationTime).TotalMilliseconds < 500) return;

            _recentNotifications.Add(key);
            _lastNotificationTime = now;
            if (_recentNotifications.Count > 100) _recentNotifications.Clear();
        }

        private void OnScreenChanged(string key)
        {
            _lblHeaderTitle.Text = key switch
            {
                KEY_HOME => "Trang chủ",
                KEY_TRIP => "Chuyến đi",
                KEY_HISTORY => "Lịch sử",
                KEY_PROFILE => "Tài khoản",
                KEY_RATING => "Đánh giá",
                _ => "OOP"
            };

            var activeBtn = key switch
            {
                KEY_HOME => _btnNavHome,
                KEY_TRIP => _btnNavTrip,
                KEY_HISTORY => _btnNavHistory,
                KEY_PROFILE => _btnNavProfile,
                KEY_RATING => _btnNavRating,
                _ => null
            };
            if (activeBtn != null) SetActiveNav(activeBtn);
        }

        // ── Shared state API ──────────────────────────────────────────────────

        public async Task OnTripStarted(Trip trip)
        {
            SetCurrentTrip(trip);
            await _nav.NavigateTo(KEY_TRIP, trip);
            UpdateTripTabBadge(hasBadge: true);
        }

        public void SetCurrentTrip(Trip? trip)
        {
            CurrentTrip = trip;
            UpdateTripTabBadge(hasBadge: trip != null);
        }

        private void UpdateTripTabBadge(bool hasBadge)
        {
            _btnNavTrip.Text = hasBadge ? "🚗  Chuyến đi  ●" : "🚗  Chuyến đi";
        }

        private void UpdateRatingTabBadge()
        {
            _btnNavRating.Text = "⭐  Đánh giá  ●";
        }

        // ── Trip polling ──────────────────────────────────────────────────────

        private async Task RestoreActiveTrip()
        {
            try
            {
                var history = await _tripService.GetTripHistory(_passenger.Id);
                var active = history.FirstOrDefault(t =>
                    t.Status is TripStatus.Requested or TripStatus.Searching
                             or TripStatus.Matched or TripStatus.Arrived
                             or TripStatus.Started);
                if (active != null)
                {
                    SetCurrentTrip(active);
                    await _nav.NavigateTo(KEY_TRIP, active);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RestoreActiveTrip] {ex.Message}");
            }
        }

        private async Task PollTripStatus()
        {
            if (CurrentTrip == null) return;
            try
            {
                var updated = await _tripService.GetTrip(CurrentTrip.Id);
                if (updated == null) return;

                SetCurrentTrip(updated);

                if (_nav.CurrentKey == KEY_TRIP) _activeTripScreen.ApplyTripUpdate(updated);
                if (_nav.CurrentKey == KEY_HOME) _homeScreen.ApplyTripUpdate(updated);

                bool finished = updated.Status is TripStatus.Completed
                                              or TripStatus.Cancelled
                                              or TripStatus.Timeout;
                if (finished)
                {
                    SetCurrentTrip(null);
                    if (InvokeRequired) BeginInvoke(async () => await OnTripFinished(updated));
                    else await OnTripFinished(updated);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PollTripStatus] {ex.Message}");
            }
        }

        private async Task OnTripFinished(Trip trip)
        {
            if (_nav.CurrentKey != KEY_TRIP)
            {
                if (trip.Status == TripStatus.Completed)
                {
                    if (FormHelper.ShowConfirm(
                        $"Chuyến đi hoàn thành!\nCước phí: {trip.Fare:N0} VNĐ\n\nBạn có muốn đánh giá tài xế không?",
                        "Chuyến đi hoàn thành"))
                        await _nav.NavigateTo(KEY_RATING, trip);
                    UpdateRatingTabBadge();
                }
                else
                {
                    string msg = trip.Status switch
                    {
                        TripStatus.Cancelled => "Chuyến đi đã bị hủy.",
                        TripStatus.Timeout => "Không tìm được tài xế.",
                        _ => "Chuyến đi đã kết thúc."
                    };
                    FormHelper.ShowSuccess(msg, "Thông báo");
                }
            }
            _activeTripScreen.ApplyTripUpdate(trip);
        }
    }
}
