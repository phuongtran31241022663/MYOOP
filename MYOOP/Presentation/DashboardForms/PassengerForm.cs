﻿using GMap.NET;
using OOP.Application.Services;
using OOP.Application.Services.Interfaces;
using OOP.Domain.Entities;
using OOP.Domain.Enums;
using OOP.Domain.Interfaces;
using OOP.Presentation.Map;
using OOP.Presentation.CoreForms;
using static OOP.Presentation.AppTheme;
using static OOP.Presentation.FormHelper;

namespace OOP.Presentation
{
    public class PassengerDashboardForm : Form
    {
        private readonly Passenger _passenger;
        private readonly IUserRepository _userRepo;
        private readonly ITripService _tripService;
        private readonly IRatingService _ratingService;
        private readonly IUserService _userService;
        private readonly INotificationService _notification;
        private readonly Func<Passenger, ITripService, Form> _requestTripFormFactory;
        private readonly Func<Passenger, ITripService, Form> _tripHistoryFormFactory;
        private readonly Func<Passenger, IRatingService, ITripService, Form> _ratingFormFactory;

        private Label _lblWelcome = null!;
        private Label _lblStats = null!;
        private const int HeaderHeight = 110;

        // ── Trip status strip (visible only when a trip is active) ────────────
        private Panel _pnlTripStatus = null!;
        private Label _lblStatus = null!;
        private Button _btnCancel = null!;

        // ── Notification log ──────────────────────────────────────────────────
        private ListBox _lstLog = null!;
        private readonly System.Windows.Forms.Timer _tripTimer = new() { Interval = 2000 };
        private Guid _currentTripId = Guid.Empty;

        public PassengerDashboardForm(
            Passenger passenger,
            IUserRepository userRepo,
            ITripService tripService,
            IRatingService ratingService,
            IUserService userService,
            INotificationService notification,
            Func<Passenger, ITripService, Form> requestTripFormFactory,
            Func<Passenger, ITripService, Form> tripHistoryFormFactory,
            Func<Passenger, IRatingService, ITripService, Form> ratingFormFactory)
        {
            _passenger = passenger ?? throw new ArgumentNullException(nameof(passenger));
            _userRepo = userRepo;
            _tripService = tripService ?? throw new ArgumentNullException(nameof(tripService));
            _ratingService = ratingService ?? throw new ArgumentNullException(nameof(ratingService));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _requestTripFormFactory = requestTripFormFactory ?? throw new ArgumentNullException(nameof(requestTripFormFactory));
            _tripHistoryFormFactory = tripHistoryFormFactory ?? throw new ArgumentNullException(nameof(tripHistoryFormFactory));
            _ratingFormFactory = ratingFormFactory ?? throw new ArgumentNullException(nameof(ratingFormFactory));

            _notification = notification ?? throw new ArgumentNullException(nameof(notification));
            _notification.OnPassengerNotified += OnNotification;

            InitForm();
            BuildUI();

            _tripTimer.Tick += async (_, _) => await RefreshTripStatus();
            _tripTimer.Start();

            FormClosed += (_, _) =>
            {
                _tripTimer.Stop();
                _tripTimer.Dispose();
                _notification.OnPassengerNotified -= OnNotification;
            };
        }

        // ── Notifications ──────────────────────────────────────────────────────
        private void OnNotification(Guid id, string msg)
        {
            if (InvokeRequired) { BeginInvoke(() => OnNotification(id, msg)); return; }

            if (_lstLog.Items.Count >= 200) _lstLog.Items.RemoveAt(0);
            _lstLog.Items.Add($"[{DateTime.Now:HH:mm}] {msg}");
            _lstLog.TopIndex = _lstLog.Items.Count - 1; // auto-scroll to latest
        }

        // ── Trip status polling ────────────────────────────────────────────────
        private async Task RefreshTripStatus()
        {
            if (_currentTripId == Guid.Empty) return;

            try
            {
                var trip = await _tripService.GetTrip(_currentTripId);
                if (trip == null) return;

                if (InvokeRequired) { BeginInvoke(() => ApplyTripStatus(trip)); return; }
                ApplyTripStatus(trip);
            }
            catch { /* swallow — polling should never crash the UI */ }
        }

        private void ApplyTripStatus(Trip trip)
        {
            _lblStatus.Text = trip.Status switch
            {
                TripStatus.Requested => "⏳  Đang tìm tài xế...",
                TripStatus.Searching => "🔎  Đang tìm tài xế...",
                TripStatus.Matched => "🚗  Tài xế đang đến đón bạn",
                TripStatus.Arrived => "📍  Tài xế đã đến điểm đón",
                TripStatus.Started => "🛣️  Chuyến đi đang diễn ra",
                TripStatus.Completed => "✅  Chuyến đi hoàn thành",
                TripStatus.Cancelled => "❌  Chuyến đi đã bị hủy",
                TripStatus.Timeout => "⌛  Hết thời gian tìm tài xế",
                _ => trip.Status.ToString()
            };

            bool canCancel = trip.Status == TripStatus.Requested
                          || trip.Status == TripStatus.Searching
                          || trip.Status == TripStatus.Matched;
            _btnCancel.Enabled = canCancel;

            // Hide strip when trip is finished
            bool finished = trip.Status == TripStatus.Completed
                         || trip.Status == TripStatus.Cancelled
                         || trip.Status == TripStatus.Timeout;
            if (finished)
            {
                _pnlTripStatus.Visible = false;
                _currentTripId = Guid.Empty;
            }
            else
            {
                _pnlTripStatus.Visible = true;
            }
        }

        // ── Form setup ────────────────────────────────────────────────────────
        private void InitForm()
        {
            Text = $"RideGo – {_passenger.Name}";
            Size = new Size(520, 620);
            MinimumSize = new Size(440, 560);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = AppTheme.PageBg;
            Font = new Font("Segoe UI", 10F);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
        }

        private void BuildUI()
        {
            // ── Log (Bottom) — add first so Fill area doesn't eat it ──────────
            _lstLog = new ListBox
            {
                Dock = DockStyle.Bottom,
                Height = 100,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(70, 70, 70),
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(248, 249, 250)
            };

            // ── Trip status strip (Bottom, above log) ─────────────────────────
            _pnlTripStatus = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 48,
                BackColor = Color.FromArgb(230, 244, 255),
                Padding = new Padding(12, 0, 12, 0),
                Visible = false
            };

            _lblStatus = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 90, 180),
                BackColor = Color.Transparent
            };

            _btnCancel = new Button
            {
                Text = "Hủy",
                Dock = DockStyle.Right,
                Width = 80,
                Height = 32,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(200, 50, 50),
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Enabled = false
            };
            _btnCancel.FlatAppearance.BorderSize = 0;
            _btnCancel.Click += async (_, _) => await OnCancelTripClicked();

            _pnlTripStatus.Controls.Add(_lblStatus);
            _pnlTripStatus.Controls.Add(_btnCancel);

            // ── Header ────────────────────────────────────────────────────────
            BuildHeader();

            // ── Menu card ─────────────────────────────────────────────────────
            BuildMenuCard();

            // Register in correct dock order (Bottom-stacked from bottom up)
            Controls.Add(_lstLog);
            Controls.Add(_pnlTripStatus);
        }

        private void BuildHeader()
        {
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = HeaderHeight,
                BackColor = AppTheme.DarkBg,
                Padding = new Padding(28, 18, 28, 12)
            };

            _lblWelcome = new Label
            {
                Text = $"Xin chào, {_passenger.Name} 👋",
                Font = new Font("Segoe UI", 15, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Top,
                Height = 36,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _lblStats = new Label
            {
                Text = BuildStatsText(),
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = AppTheme.TextSubtle,
                Dock = DockStyle.Top,
                Height = 24,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft
            };

            header.Controls.Add(_lblStats);
            header.Controls.Add(_lblWelcome);
            Controls.Add(header);
        }

        private void BuildMenuCard()
        {
            var card = new Panel
            {
                BackColor = AppTheme.CardBg,
                Width = 380,
                Height = 300
            };

            card.Paint += FormHelper.RoundedBorderPainter(AppTheme.CardRadius);
            Resize += (_, _) => LayoutMenuCard(card);
            LayoutMenuCard(card);

            int y = 24;

            var btnRequestTrip = MakeMenuBtn(
                "🚗  Đặt xe", "Tìm tài xế và đặt chuyến mới",
                AppTheme.Success, AppTheme.SuccessHover, height: 56);
            FormHelper.Place(btnRequestTrip, card, 24, y, card.Width - 48, 56);
            btnRequestTrip.Click += (_, _) =>
            {
                var form = _requestTripFormFactory(_passenger, _tripService);
                OpenChildForm(form);

                _ = SyncActiveTripAsync();
            };
            y += 66;

            var btnTripHistory = MakeMenuBtn(
                "🕒  Lịch sử chuyến đi", "Xem các chuyến đã thực hiện",
                AppTheme.Primary, AppTheme.PrimaryHover, height: 48);
            FormHelper.Place(btnTripHistory, card, 24, y, card.Width - 48, 48);
            btnTripHistory.Click += (_, _) =>
                OpenChildForm(_tripHistoryFormFactory(_passenger, _tripService));
            y += 58;

            var btnProfile = MakeMenuBtn(
                "👤  Thông tin cá nhân", "Cập nhật họ tên và số điện thoại",
                AppTheme.Accent, AppTheme.AccentHover, height: 48);
            FormHelper.Place(btnProfile, card, 24, y, card.Width - 48, 48);
            btnProfile.Click += (_, _) => OpenChildForm(new ProfileForm(_passenger, _userService));
            y += 58;

            var btnRating = MakeMenuBtn(
                "⭐  Đánh giá tài xế", "Đánh giá chuyến đi vừa hoàn thành",
                AppTheme.Warning, AppTheme.WarningHover, height: 48);
            FormHelper.Place(btnRating, card, 24, y, card.Width - 48, 48);
            btnRating.Click += (_, _) =>
                OpenChildForm(_ratingFormFactory(_passenger, _ratingService, _tripService));
            y += 58;

            FormHelper.Place(
                new Label { BackColor = AppTheme.BorderLight },
                card, 24, y, card.Width - 48, 1);
            y += 14;

            var btnLogout = FormHelper.MakeOutlineButton("← Đăng xuất", height: AppTheme.SmallButton);
            FormHelper.Place(btnLogout, card, 24, y, card.Width - 48, AppTheme.SmallButton);
            btnLogout.Click += (_, _) =>
            {
                if (MessageBox.Show("Bạn có chắc muốn đăng xuất?", "Xác nhận",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    Close();
            };

            Controls.Add(card);
        }

        private void LayoutMenuCard(Control card)
        {
            int bottomReserve = _lstLog.Height + _pnlTripStatus.Height + 16;
            int available = ClientSize.Height - HeaderHeight - bottomReserve;
            int y = HeaderHeight + Math.Max(16, (available - card.Height) / 2);
            int x = (ClientSize.Width - card.Width) / 2;
            card.Location = new Point(x, y);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// After RequestTripForm closes, scan for a Requested/Matched/Arrived/Started
        /// trip belonging to this passenger and start showing the status strip.
        /// </summary>
        private async Task SyncActiveTripAsync()
        {
            try
            {
                var history = await _tripService.GetTripHistory(_passenger.Id);
                var active = history.FirstOrDefault(t =>
                    t.PassengerId == _passenger.Id &&
                    t.Status != TripStatus.Completed &&
                    t.Status != TripStatus.Cancelled &&
                    t.Status != TripStatus.Timeout);

                if (active != null)
                    _currentTripId = active.Id;
            }
            catch { /* non-critical */ }
        }

        private async Task OnCancelTripClicked()
        {
            if (_currentTripId == Guid.Empty) return;

            if (MessageBox.Show("Bạn có chắc muốn hủy chuyến đi?", "Xác nhận hủy",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            try
            {
                await _tripService.CancelTrip(_currentTripId, "Hành khách tự hủy");
                _pnlTripStatus.Visible = false;
                _currentTripId = Guid.Empty;
                MessageBox.Show("Chuyến đi đã được hủy.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể hủy: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void OpenChildForm(Form childForm)
        {
            using (childForm)
            {
                childForm.StartPosition = FormStartPosition.CenterParent;
                Hide();
                childForm.ShowDialog(this);
            }
            _lblStats.Text = BuildStatsText();
            Show();
            Focus();
        }

        private string BuildStatsText() =>
            $"Tổng chuyến đã đi: {_passenger.TotalTrips}   •   SĐT: {_passenger.Phone}";

        private static Button MakeMenuBtn(
            string text, string subtext,
            Color bg, Color hover, int height,
            Color? textColor = null)
        {
            var fg = textColor ?? Color.White;
            var btn = new Button
            {
                Height = height,
                BackColor = bg,
                ForeColor = fg,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                Text = ""
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Paint += (s, e) =>
            {
                var g = e.Graphics;
                var rect = btn.ClientRectangle;
                using var mainFont = new Font("Segoe UI", 10.5f, FontStyle.Bold);
                using var subFont = new Font("Segoe UI", 8.5f);
                using var mainBrush = new SolidBrush(fg);
                using var subBrush = new SolidBrush(Color.FromArgb(fg == Color.White ? 200 : 130, fg));
                int textX = btn.Padding.Left + 16;
                int totalH = (int)mainFont.GetHeight() + (int)subFont.GetHeight() + 2;
                int startY = (rect.Height - totalH) / 2;
                g.DrawString(text, mainFont, mainBrush, textX, startY);
                g.DrawString(subtext, subFont, subBrush, textX, startY + (int)mainFont.GetHeight() + 2);
            };
            FormHelper.AttachHover(btn, bg, hover);
            return btn;
        }
    }
}

