using OOP.Presentation.Base;
using OOP.Application.Services.Interfaces;
using OOP.Domain.Entities;
using OOP.Domain.Enums;
using OOP.Presentation.Common.Theme;
using OOP.Presentation.Base;

namespace OOP.Presentation.Screens.Passenger
{
    /// <summary>
    /// Màn hình theo dõi chuyến đi đang diễn ra.
    /// Thay thế panel _pnlTripStatus trong PassengerDashboardForm cũ.
    ///
    /// Lợi thế so với thiết kế cũ:
    /// - Toàn màn hình, hiển thị đầy đủ thông tin
    /// - Hiển thị bản đồ mini + vị trí tài xế
    /// - Nhận update từ Shell.PollTripStatus() qua ApplyTripUpdate()
    /// - Khi không có chuyến → hiện empty state, không lộn xộn
    /// </summary>
    public class PassengerActiveTripScreen : UserControl, IScreen
    {
        // ── Dependencies ──────────────────────────────────────────────────────
        private readonly PassengerShell _shell;
        private readonly ITripService _tripService;

        // ── UI Controls ───────────────────────────────────────────────────────
        private Panel _pnlEmpty = null!;       // Khi chưa có chuyến
        private Panel _pnlActive = null!;      // Khi có chuyến đang diễn ra

        // Active panel controls
        private Label _lblStatus = null!;
        private Panel _pnlStatusBadge = null!;
        private Label _lblPickup = null!;
        private Label _lblDestination = null!;
        private Label _lblFare = null!;
        private Label _lblDriverInfo = null!;
        private Button _btnCancel = null!;
        private Button _btnGoHome = null!;

        // ── IScreen ───────────────────────────────────────────────────────────
        public string ScreenTitle => "Chuyến đi";

        public async Task OnNavigatedTo(object? parameter = null)
        {
            // Có thể nhận trip từ parameter (khi Shell navigate sang ngay sau đặt chuyến)
            if (parameter is Trip trip)
                ApplyTripUpdate(trip);
            else if (_shell.CurrentTrip != null)
                ApplyTripUpdate(_shell.CurrentTrip);
            else
                ShowEmptyState();

            await Task.CompletedTask;
        }

        public bool OnNavigatingFrom()
        {
            // Cho phép rời bất kỳ lúc nào — shell vẫn poll nền
            return true;
        }

        // ─────────────────────────────────────────────────────────────────────
        public PassengerActiveTripScreen(PassengerShell shell, ITripService tripService)
        {
            _shell = shell ?? throw new ArgumentNullException(nameof(shell));
            _tripService = tripService ?? throw new ArgumentNullException(nameof(tripService));
            BuildUI();
        }

        // ── Build UI ──────────────────────────────────────────────────────────

        private void BuildUI()
        {
            BackColor = AppTheme.PageBg;
            Padding = new Padding(16);

            // ── Empty state ──
            _pnlEmpty = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.PageBg };
            var lblEmpty = new Label
            {
                Text = "Bạn chưa có chuyến đi nào.\nHãy đặt chuyến từ Trang chủ.",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 12),
                ForeColor = AppTheme.TextMuted
            };
            _btnGoHome = FormHelper.MakeButton("Về Trang chủ", AppTheme.Primary, AppTheme.PrimaryHover);
            _btnGoHome.Width = 200;
            _btnGoHome.Location = new Point(100, 280);
            _btnGoHome.Click += async (_, _) => await _shell.Nav.NavigateTo(PassengerShell.KEY_HOME);
            _pnlEmpty.Controls.Add(lblEmpty);
            _pnlEmpty.Controls.Add(_btnGoHome);

            // ── Active trip panel ──
            _pnlActive = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.PageBg, Visible = false };
            BuildActiveTripUI();

            Controls.Add(_pnlEmpty);
            Controls.Add(_pnlActive);
        }

        private void BuildActiveTripUI()
        {
            // Status badge (màu thay đổi theo trạng thái)
            _pnlStatusBadge = new Panel
            {
                Height = 48,
                BackColor = AppTheme.Warning,
                Dock = DockStyle.Top
            };
            _lblStatus = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.White,
                Text = "⏳  Đang tìm tài xế..."
            };
            _pnlStatusBadge.Controls.Add(_lblStatus);

            // Info card
            var card = FormHelper.MakeCard(0, 180);
            card.Dock = DockStyle.None;
            card.Width = 380;
            card.Location = new Point(16, 64);

            int y = 12;
            var lblPickupTitle = FormHelper.MakeLabel("📍 Điểm đón", 9f, foreColor: AppTheme.TextMuted);
            FormHelper.Place(lblPickupTitle, card, 16, y, 340, 18); y += 20;

            _lblPickup = FormHelper.MakeLabel("", 10.5f);
            FormHelper.Place(_lblPickup, card, 16, y, 340, 20); y += 28;

            var lblDestTitle = FormHelper.MakeLabel("🏁 Điểm đến", 9f, foreColor: AppTheme.TextMuted);
            FormHelper.Place(lblDestTitle, card, 16, y, 340, 18); y += 20;

            _lblDestination = FormHelper.MakeLabel("", 10.5f);
            FormHelper.Place(_lblDestination, card, 16, y, 340, 20); y += 28;

            var divider = new Panel { Left = 16, Top = y, Width = 340, Height = 1, BackColor = AppTheme.BorderLight };
            card.Controls.Add(divider); y += 9;

            _lblFare = FormHelper.MakeLabel("", 10f, foreColor: AppTheme.TextMuted);
            FormHelper.Place(_lblFare, card, 16, y, 340, 20); y += 28;

            // Driver info (ẩn khi chưa matched)
            _lblDriverInfo = new Label
            {
                Left = 16,
                Top = y,
                Width = 340,
                Height = 48,
                Font = AppTheme.SmallFont,
                ForeColor = AppTheme.TextMuted,
                Visible = false
            };
            card.Controls.Add(_lblDriverInfo);

            _pnlActive.Controls.Add(_pnlStatusBadge);
            _pnlActive.Controls.Add(card);

            // Cancel button
            _btnCancel = FormHelper.MakeButton("Hủy chuyến", AppTheme.Danger, AppTheme.DangerHover);
            _btnCancel.Width = 200;
            _btnCancel.Location = new Point(90, 310);
            _btnCancel.Click += async (_, _) => await OnCancelClicked();
            _pnlActive.Controls.Add(_btnCancel);
        }

        // ── Public API (called by Shell.PollTripStatus) ───────────────────────

        /// <summary>
        /// Shell gọi method này mỗi khi có update về chuyến đi.
        /// Thread-safe: tự handle InvokeRequired.
        /// </summary>
        public void ApplyTripUpdate(Trip trip)
        {
            if (InvokeRequired) { BeginInvoke(() => ApplyTripUpdate(trip)); return; }

            bool finished = trip.Status is TripStatus.Completed
                                        or TripStatus.Cancelled
                                        or TripStatus.Timeout;
            if (finished)
            {
                ShowEmptyState();
                return;
            }

            // Chuyển sang active view
            _pnlEmpty.Visible = false;
            _pnlActive.Visible = true;

            // Status text + màu
            (_lblStatus.Text, _pnlStatusBadge.BackColor) = trip.Status switch
            {
                TripStatus.Requested => ("⏳  Đang tìm tài xế...", AppTheme.Warning),
                TripStatus.Searching => ("🔎  Đang tìm tài xế...", AppTheme.Accent),
                TripStatus.Matched => ("🚗  Tài xế đang đến đón bạn", AppTheme.Primary),
                TripStatus.Arrived => ("📍  Tài xế đã đến điểm đón", AppTheme.Primary),
                TripStatus.Started => ("🛣️  Chuyến đi đang diễn ra", AppTheme.Success),
                _ => (trip.Status.ToString(), AppTheme.TextMuted)
            };

            // Route info
            _lblPickup.Text = trip.Pickup?.Name ?? "–";
            _lblDestination.Text = trip.Destination?.Name ?? "–";
            _lblFare.Text = trip.Fare > 0
                ? $"Dự kiến: {trip.Fare:N0} VNĐ"
                : "Đang tính cước...";

            // Driver info (hiện khi đã matched)
            bool hasDriver = trip.DriverId.HasValue
                && trip.Status is not TripStatus.Requested
                              and not TripStatus.Searching;
            _lblDriverInfo.Visible = hasDriver;

            // Cancel chỉ cho phép ở một số trạng thái
            _btnCancel.Enabled = trip.Status is TripStatus.Requested
                                             or TripStatus.Searching
                                             or TripStatus.Matched;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void ShowEmptyState()
        {
            _pnlEmpty.Visible = true;
            _pnlActive.Visible = false;
        }

        private async Task OnCancelClicked()
        {
            if (_shell.CurrentTrip == null) return;

            var confirm = MessageBox.Show(
                "Bạn có chắc muốn hủy chuyến đi không?",
                "Xác nhận hủy",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            try
            {
                _btnCancel.Enabled = false;
                _btnCancel.Text = "Đang hủy...";
                await _tripService.CancelTrip(_shell.CurrentTrip.Id, "Passenger cancelled");
                _shell.SetCurrentTrip(null);
                ShowEmptyState();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể hủy: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _btnCancel.Enabled = true;
                _btnCancel.Text = "Hủy chuyến";
            }
        }
    }
}