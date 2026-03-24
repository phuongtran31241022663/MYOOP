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
    /// Shell duy nhất cho Passenger — thay thế PassengerDashboardForm.
    ///
    /// Kiến trúc:
    ///   ┌─────────────────────────────┐
    ///   │  Header (tên + trạng thái) │  ← cố định
    ///   ├─────────────────────────────┤
    ///   │                             │
    ///   │    Content Area (screens)   │  ← swap bằng ScreenNavigator
    ///   │                             │
    ///   ├─────────────────────────────┤
    ///   │  Home | Trip | History | 👤 │  ← Bottom nav cố định
    ///   └─────────────────────────────┘
    ///
    /// State dùng chung (CurrentTrip, v.v.) sống ở đây — các screen
    /// nhận tham chiếu tới Shell để đọc/ghi state này.
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

        // ── Header controls ───────────────────────────────────────────────────
        private Label _lblHeaderTitle = null!;

        // ── Bottom nav buttons ────────────────────────────────────────────────
        private Button _btnNavHome = null!;
        private Button _btnNavTrip = null!;
        private Button _btnNavHistory = null!;
        private Button _btnNavProfile = null!;
        private Button _btnNavRating = null!;

        // ── Shared state ──────────────────────────────────────────────────────
        /// <summary>
        /// Chuyến đi hiện tại của passenger. Null nếu không có chuyến.
        /// Các screen đọc/ghi qua SetCurrentTrip().
        /// </summary>
        public Trip? CurrentTrip { get; private set; }

        // Expose Passenger and Navigator for screens
        public Passenger Passenger => _passenger;
        public ScreenNavigator Nav => _nav;

        private readonly System.Windows.Forms.Timer _pollTimer = new() { Interval = 4000 };

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

            Text = $"{_passenger.Name}";
            Size = new Size(480, 780);
            MinimumSize = new Size(420, 640);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;

            BuildShell();
            WireUpEvents();
        }

        // ── Shell construction ────────────────────────────────────────────────

        private void BuildShell()
        {
            // 1. Header cố định trên cùng
            var header = BuildHeader();

            // 2. Bottom nav cố định dưới cùng
            var bottomNav = BuildBottomNav();

            // 3. Content host ở giữa — screens sẽ fill vào đây
            var contentHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.PageBg
            };

            // Thêm theo thứ tự ngược (Fill lấy phần còn lại sau Top/Bottom)
            Controls.Add(contentHost);
            Controls.Add(bottomNav);
            Controls.Add(header);

            // 4. Khởi tạo và đăng ký các screens
            _nav = new ScreenNavigator(contentHost);

            _homeScreen = new PassengerHomeScreen(this, _tripService, _http, _routeService, _fareService);
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

        private Panel BuildHeader()
        {
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 58,
                BackColor = AppTheme.Primary,
                Padding = new Padding(16, 0, 16, 0)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
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

            layout.Controls.Add(_lblHeaderTitle, 0, 0);
            layout.Controls.Add(lblUser, 1, 0);
            header.Controls.Add(layout);
            return header;
        }

        private Panel BuildBottomNav()
        {
            var nav = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 62,
                BackColor = Color.White,
                Padding = new Padding(0)
            };

            // Đường kẻ phân cách trên cùng của bottom nav
            nav.Paint += (s, e) =>
            {
                using var pen = new Pen(AppTheme.BorderLight);
                e.Graphics.DrawLine(pen, 0, 0, nav.Width, 0);
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 5,
                RowCount = 1
            };
            for (int i = 0; i < 5; i++)
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));

            _btnNavHome = MakeNavButton("🏠", "Trang chủ");
            _btnNavTrip = MakeNavButton("🚗", "Chuyến đi");
            _btnNavHistory = MakeNavButton("📋", "Lịch sử");
            _btnNavProfile = MakeNavButton("👤", "Cá nhân");
            _btnNavRating = MakeNavButton("⭐", "Đánh giá");

            _btnNavHome.Click += async (_, _) => await _nav.NavigateTo(KEY_HOME);
            _btnNavTrip.Click += async (_, _) => await _nav.NavigateTo(KEY_TRIP);
            _btnNavHistory.Click += async (_, _) => await _nav.NavigateTo(KEY_HISTORY);
            _btnNavProfile.Click += async (_, _) => await _nav.NavigateTo(KEY_PROFILE);
            _btnNavRating.Click += async (_, _) => await _nav.NavigateTo(KEY_RATING);

            layout.Controls.Add(_btnNavHome, 0, 0);
            layout.Controls.Add(_btnNavTrip, 1, 0);
            layout.Controls.Add(_btnNavHistory, 2, 0);
            layout.Controls.Add(_btnNavProfile, 3, 0);
            layout.Controls.Add(_btnNavRating, 4, 0);
            nav.Controls.Add(layout);
            return nav;
        }

        private static Button MakeNavButton(string icon, string label)
        {
            // Nút có icon trên, text dưới
            var btn = new Button
            {
                Text = $"{icon}\n{label}",
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 8f),
                ForeColor = AppTheme.TextMuted,
                BackColor = Color.White,
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.MouseEnter += (_, _) => btn.BackColor = AppTheme.PageBg;
            btn.MouseLeave += (_, _) => btn.BackColor = Color.White;
            return btn;
        }

        // ── Events ────────────────────────────────────────────────────────────

        private void WireUpEvents()
        {
            _notification.OnPassengerNotified += OnNotification;

            Load += async (_, _) =>
            {
                // Kiểm tra xem có chuyến đang dở không (vd: user tắt app giữa chừng)
                await RestoreActiveTrip();
                await _nav.NavigateTo(KEY_HOME);

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
            // Có thể hiện toast notification tại đây
        }

        private void OnScreenChanged(string key)
        {
            // Cập nhật header title
            _lblHeaderTitle.Text = key switch
            {
                KEY_HOME => "Trang chủ",
                KEY_TRIP => "Chuyến đi",
                KEY_HISTORY => "Lịch sử",
                KEY_PROFILE => "Tài khoản",
                KEY_RATING => "Đánh giá",
                _ => "G"
            };

            // Highlight active tab
            Color active = AppTheme.Primary;
            Color inactive = AppTheme.TextMuted;
            _btnNavHome.ForeColor = key == KEY_HOME ? active : inactive;
            _btnNavTrip.ForeColor = key == KEY_TRIP ? active : inactive;
            _btnNavHistory.ForeColor = key == KEY_HISTORY ? active : inactive;
            _btnNavProfile.ForeColor = key == KEY_PROFILE ? active : inactive;
            _btnNavRating.ForeColor = key == KEY_RATING ? active : inactive;
        }

        // ── Shared state API ─────────────────────────────────────────────────

        /// <summary>
        /// HomeScreen gọi sau khi đặt chuyến thành công.
        /// Shell sẽ tự động chuyển sang tab Trip và bắt đầu poll.
        /// </summary>
        public async Task OnTripStarted(Trip trip)
        {
            SetCurrentTrip(trip);
            await _nav.NavigateTo(KEY_TRIP, trip);
            UpdateTripTabBadge(hasBadge: true);
        }

        /// <summary>Gọi từ bất kỳ screen nào khi cần cập nhật trạng thái chuyến.</summary>
        public void SetCurrentTrip(Trip? trip)
        {
            CurrentTrip = trip;
            UpdateTripTabBadge(hasBadge: trip != null);
        }

        /// <summary>
        /// Cập nhật badge trên tab Trip (chấm đỏ khi có chuyến đang diễn ra).
        /// </summary>
        private void UpdateTripTabBadge(bool hasBadge)
        {
            _btnNavTrip.Text = hasBadge ? "🚗\nChuyến đi ●" : "🚗\nChuyến đi";
            _btnNavTrip.ForeColor = hasBadge ? AppTheme.Primary : AppTheme.TextMuted;
        }

        /// <summary>
        /// Cập nhật badge trên tab Đánh giá (chấm vàng ⭐● khi có chuyến cần đánh giá).
        /// </summary>
        private void UpdateRatingTabBadge()
        {
            _btnNavRating.Text = "⭐\nĐánh giá ●";
            _btnNavRating.ForeColor = Color.FromArgb(255, 193, 7); // Màu vàng
        }

        // ── Trip polling ──────────────────────────────────────────────────────

        /// <summary>
        /// Khi app khởi động lại, kiểm tra xem passenger có chuyến đang dở không.
        /// Đây chính là điểm giải quyết bài toán "tắt app giữa chừng".
        /// </summary>
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
                    // Tự động đi thẳng vào tab Trip
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

                // Thông báo cho ActiveTripScreen để update UI
                if (_nav.CurrentKey == KEY_TRIP)
                    _activeTripScreen.ApplyTripUpdate(updated);

                // Nếu chuyến kết thúc
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
            // Hiển thị popup nếu không đang ở tab Trip
            if (_nav.CurrentKey != KEY_TRIP)
            {
                if (trip.Status == TripStatus.Completed)
                {
                    // Gộp 2 popup thành 1
                    var result = MessageBox.Show(
                        $"✅ Chuyến đi hoàn thành!\nCước phí: {trip.Fare:N0} VNĐ\n\nBạn có muốn đánh giá tài xế không?",
                        "Chuyến đi hoàn thành",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Information);

                    if (result == DialogResult.Yes)
                        await _nav.NavigateTo(KEY_RATING, trip);
                    
                    // Update badge to remind user to rate
                    UpdateRatingTabBadge();
                }
                else
                {
                    string msg = trip.Status switch
                    {
                        TripStatus.Cancelled => "❌ Chuyến đi đã bị hủy.",
                        TripStatus.Timeout => "⌛ Không tìm được tài xế.",
                        _ => "Chuyến đi đã kết thúc."
                    };
                    MessageBox.Show(msg, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }

            // Cập nhật lại ActiveTripScreen (hiện empty state)
            _activeTripScreen.ApplyTripUpdate(trip);
        }
    }
}