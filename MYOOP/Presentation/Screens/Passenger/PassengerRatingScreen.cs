using OOP.Application.Services.Interfaces;
using OOP.Domain.Entities;
using OOP.Domain.Enums;
using OOP.Domain.Validators;
using OOP.Presentation.Common.Theme;
using OOP.Presentation.Base;
using OOP.Presentation.Screens;

namespace OOP.Presentation.Screens.Passenger
{
    /// <summary>
    /// Màn hình đánh giá tài xế sau chuyến đi hoàn thành.
    /// Thay thế RatingForm (trước đây mở bằng ShowDialog).
    ///
    /// Flow:
    ///   1. OnNavigatedTo → load danh sách chuyến đã hoàn thành chưa đánh giá
    ///   2. User chọn chuyến → chọn sao → nhập comment → Gửi
    ///   3. Sau khi gửi thành công → reload lại list (có thể gửi nhiều lần)
    ///
    /// Nếu được gọi với parameter là Trip → pre-select chuyến đó.
    /// </summary>
    public class PassengerRatingScreen : UserControl, IScreen
    {
        // ── Dependencies ──────────────────────────────────────────────────────
        private readonly PassengerShell _shell;
        private readonly IRatingService _ratingService;
        private readonly ITripService _tripService;

        // ── State ─────────────────────────────────────────────────────────────
        private List<Trip> _pendingTrips = new();
        private Trip? _selectedTrip;

        // ── Controls ──────────────────────────────────────────────────────────
        // Trip list (left panel)
        private Panel _pnlEmpty = null!;
        private Panel _pnlContent = null!;
        private ListBox _lstTrips = null!;
        private Label _lblTripCount = null!;

        // Rating form (right panel)
        private Panel _pnlNoSelection = null!;
        private Panel _pnlRatingForm = null!;
        private Label _lblSelectedTrip = null!;
        private Panel _pnlStars = null!;
        private Button[] _starButtons = null!;
        private int _selectedScore = 5;
        private TextBox _txtComment = null!;
        private Label _lblCommentHint = null!;
        private Button _btnSubmit = null!;
        private Label _lblSuccess = null!;

        // ── IScreen ───────────────────────────────────────────────────────────
        public string ScreenTitle => "Đánh giá";

        public async Task OnNavigatedTo(object? parameter = null)
        {
            await LoadPendingTrips();

            // Nếu được navigate tới với trip cụ thể → pre-select
            if (parameter is Trip t)
                PreSelectTrip(t);
        }

        public bool OnNavigatingFrom() => true;

        // ─────────────────────────────────────────────────────────────────────
        public PassengerRatingScreen(
            PassengerShell shell,
            IRatingService ratingService,
            ITripService tripService)
        {
            _shell = shell ?? throw new ArgumentNullException(nameof(shell));
            _ratingService = ratingService ?? throw new ArgumentNullException(nameof(ratingService));
            _tripService = tripService ?? throw new ArgumentNullException(nameof(tripService));
            BuildUI();
        }

        // ── Build UI ──────────────────────────────────────────────────────────

        private void BuildUI()
        {
            BackColor = AppTheme.PageBg;

            // Header bar
            var headerBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 54,
                BackColor = AppTheme.CardBg,
                Padding = new Padding(16, 8, 16, 8)
            };
            var lblTitle = new Label
            {
                Text = "Đánh giá tài xế",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            var btnRefresh = FormHelper.MakeButton("🔄 Làm mới", AppTheme.Primary, AppTheme.PrimaryHover, height: 36);
            btnRefresh.Width = 110;
            btnRefresh.Dock = DockStyle.Right;
            btnRefresh.Click += async (_, _) => await LoadPendingTrips();
            headerBar.Controls.Add(lblTitle);
            headerBar.Controls.Add(btnRefresh);

            // Empty state (khi không có chuyến cần đánh giá)
            _pnlEmpty = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.PageBg, Visible = false };
            _pnlEmpty.Controls.Add(new Label
            {
                Text = "✅ Bạn đã đánh giá tất cả chuyến đi.\nKhông có chuyến nào chờ đánh giá.",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 11),
                ForeColor = AppTheme.TextMuted
            });

            // Main content: left list + right form
            _pnlContent = new Panel { Dock = DockStyle.Fill };
            BuildTripList();
            BuildRatingForm();

            Controls.Add(_pnlEmpty);
            Controls.Add(_pnlContent);
            Controls.Add(headerBar);
        }

        private void BuildTripList()
        {
            // Left panel — danh sách chuyến chờ đánh giá
            var leftPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 200,
                BackColor = AppTheme.CardBg,
                Padding = new Padding(0)
            };

            var listHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = AppTheme.SidebarBg,
                Padding = new Padding(16, 0, 12, 0)
            };
            _lblTripCount = new Label
            {
                Text = "Chuyến chờ đánh giá",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.White
            };
            listHeader.Controls.Add(_lblTripCount);

            _lstTrips = new ListBox
            {
                Dock = DockStyle.Fill,
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 72,
                BorderStyle = BorderStyle.None,
                BackColor = AppTheme.CardBg,
                Font = new Font("Segoe UI", 9.5f)
            };
            _lstTrips.DrawItem += OnDrawTripItem;
            _lstTrips.SelectedIndexChanged += OnTripSelected;

            leftPanel.Controls.Add(_lstTrips);
            leftPanel.Controls.Add(listHeader);
            _pnlContent.Controls.Add(leftPanel);
        }

        private void BuildRatingForm()
        {
            // Right panel — form đánh giá
            var rightPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.PageBg,
                Padding = new Padding(24)
            };

            // Khi chưa chọn chuyến
            _pnlNoSelection = new Panel { Dock = DockStyle.Fill };
            _pnlNoSelection.Controls.Add(new Label
            {
                Text = "👈 Chọn một chuyến từ danh sách bên trái để đánh giá",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 11),
                ForeColor = AppTheme.TextMuted
            });

            // Form đánh giá
            _pnlRatingForm = new Panel { Dock = DockStyle.Fill, Visible = false };
            BuildRatingFormContent();

            rightPanel.Controls.Add(_pnlNoSelection);
            rightPanel.Controls.Add(_pnlRatingForm);
            _pnlContent.Controls.Add(rightPanel);
        }

        private void BuildRatingFormContent()
        {
            var card = FormHelper.MakeCard(200, 350);
            card.Location = new Point(0, 0);

            int y = 20;

            // Trip info
            _lblSelectedTrip = new Label
            {
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = AppTheme.TextPrimary,
                Width = 160,
                Height = 48,
                Location = new Point(20, y),
                AutoEllipsis = true
            };
            card.Controls.Add(_lblSelectedTrip); y += 56;

            // Divider
            card.Controls.Add(new Panel { Left = 20, Top = y, Width = 160, Height = 1, BackColor = AppTheme.BorderLight }); y += 16;

            // Stars
            var lblStarTitle = FormHelper.MakeLabel("Chấm điểm tài xế", 9.5f, foreColor: AppTheme.TextMuted);
            FormHelper.Place(lblStarTitle, card, 20, y, 160, 18); y += 24;

            _pnlStars = new Panel { Left = 20, Top = y, Width = 160, Height = 40 };
            _starButtons = new Button[5];
            for (int i = 0; i < 5; i++)
            {
                int score = i + 1;
                var btn = new Button
                {
                    Text = "★",
                    Width = 30,
                    Height = 30,
                    Left = i * 32,
                    Top = 5,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 14f),
                    Cursor = Cursors.Hand,
                    Tag = score,
                    BackColor = Color.Transparent
                };
                btn.FlatAppearance.BorderSize = 0;
                btn.Click += (_, _) => SetScore(score);
                _starButtons[i] = btn;
                _pnlStars.Controls.Add(btn);
            }
            card.Controls.Add(_pnlStars); y += 40;

            // Comment
            _lblCommentHint = FormHelper.MakeLabel("Nhận xét (tùy chọn)", 9.5f, foreColor: AppTheme.TextMuted);
            FormHelper.Place(_lblCommentHint, card, 20, y, 160, 18); y += 22;

            _txtComment = new TextBox
            {
                Left = 20,
                Top = y,
                Width = 160,
                Height = 80,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Segoe UI", 9f),
                PlaceholderText = "Nhận xét về tài xế, chất lượng dịch vụ...",
                BorderStyle = BorderStyle.FixedSingle
            };
            card.Controls.Add(_txtComment); y += 88;

            // Submit button
            _btnSubmit = FormHelper.MakeButton("⭐ Gửi đánh giá", AppTheme.Primary, AppTheme.PrimaryHover, height: 36);
            _btnSubmit.Width = 120;
            _btnSubmit.Left = 40;
            _btnSubmit.Top = y;
            _btnSubmit.Click += async (_, _) => await OnSubmitClicked();
            card.Controls.Add(_btnSubmit);

            // Success label
            _lblSuccess = new Label
            {
                Text = "✅ Đánh giá đã được gửi thành công!",
                Left = 20,
                Top = y + 12,
                Width = 160,
                Height = 22,
                Font = AppTheme.SmallFont,
                ForeColor = AppTheme.Success,
                Visible = false
            };
            card.Controls.Add(_lblSuccess);

            _pnlRatingForm.Controls.Add(card);

            // Init star display
            SetScore(5);
        }

        // ── Data loading ──────────────────────────────────────────────────────

        private async Task LoadPendingTrips()
        {
            try
            {
                var history = await _tripService.GetTripHistory(_shell.Passenger.Id);
                _pendingTrips = history
                    .Where(t => t.Status == TripStatus.Completed && !t.IsRated)
                    .OrderByDescending(t => t.RequestedAt)
                    .ToList();

                RefreshTripList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải dữ liệu: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RefreshTripList()
        {
            if (InvokeRequired) { BeginInvoke(RefreshTripList); return; }

            _lstTrips.Items.Clear();

            if (_pendingTrips.Count == 0)
            {
                _pnlContent.Visible = false;
                _pnlEmpty.Visible = true;
                return;
            }

            _pnlEmpty.Visible = false;
            _pnlContent.Visible = true;

            _lblTripCount.Text = $"Chờ đánh giá ({_pendingTrips.Count})";
            foreach (var t in _pendingTrips)
                _lstTrips.Items.Add(t);

            // Reset form nếu chuyến đang chọn không còn trong list
            if (_selectedTrip != null && !_pendingTrips.Any(t => t.Id == _selectedTrip.Id))
            {
                _selectedTrip = null;
                _pnlNoSelection.Visible = true;
                _pnlRatingForm.Visible = false;
            }
        }

        private void PreSelectTrip(Trip trip)
        {
            var match = _pendingTrips.FirstOrDefault(t => t.Id == trip.Id);
            if (match == null) return;

            int idx = _pendingTrips.IndexOf(match);
            _lstTrips.SelectedIndex = idx;
        }

        // ── Trip list rendering ───────────────────────────────────────────────

        private void OnDrawTripItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= _pendingTrips.Count) return;

            var trip = _pendingTrips[e.Index];
            bool selected = (e.State & DrawItemState.Selected) != 0;

            e.DrawBackground();
            var g = e.Graphics;

            // Background
            using var bgBrush = new SolidBrush(selected ? AppTheme.Highlight : Color.White);
            g.FillRectangle(bgBrush, e.Bounds);

            // Left accent bar
            using var accentBrush = new SolidBrush(selected ? AppTheme.Primary : AppTheme.BorderLight);
            g.FillRectangle(accentBrush, new Rectangle(e.Bounds.X, e.Bounds.Y, 4, e.Bounds.Height));

            int x = e.Bounds.X + 16;
            int y = e.Bounds.Y + 8;

            // Date
            using var dateFont = new Font("Segoe UI", 8.5f);
            using var dateBrush = new SolidBrush(AppTheme.TextMuted);
            g.DrawString(trip.RequestedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                dateFont, dateBrush, new PointF(x, y));
            y += 18;

            // Route
            using var routeFont = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            using var routeBrush = new SolidBrush(selected ? AppTheme.Primary : AppTheme.TextPrimary);
            string route = $"📍 {Truncate(trip.Pickup?.Name ?? "–", 22)}";
            g.DrawString(route, routeFont, routeBrush, new PointF(x, y));
            y += 20;

            using var destFont = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            string dest = $"🏁 {Truncate(trip.Destination?.Name ?? "–", 22)}";
            g.DrawString(dest, destFont, routeBrush, new PointF(x, y));
            y += 20;

            // Fare
            using var fareFont = new Font("Segoe UI", 8.5f);
            using var fareBrush = new SolidBrush(AppTheme.Success);
            string fare = trip.Fare > 0 ? $"💰 {trip.Fare:N0} VNĐ" : "–";
            g.DrawString(fare, fareFont, fareBrush, new PointF(x, y));

            // Bottom divider
            using var divPen = new Pen(AppTheme.BorderLight);
            g.DrawLine(divPen, e.Bounds.X + 8, e.Bounds.Bottom - 1, e.Bounds.Right - 8, e.Bounds.Bottom - 1);
        }

        private static string Truncate(string text, int maxLen) =>
            text.Length <= maxLen ? text : text[..maxLen] + "…";

        // ── Rating form logic ─────────────────────────────────────────────────

        private void OnTripSelected(object? sender, EventArgs e)
        {
            if (_lstTrips.SelectedIndex < 0 || _lstTrips.SelectedIndex >= _pendingTrips.Count)
            {
                _selectedTrip = null;
                _pnlNoSelection.Visible = true;
                _pnlRatingForm.Visible = false;
                return;
            }

            _selectedTrip = _pendingTrips[_lstTrips.SelectedIndex];
            LoadTripIntoForm(_selectedTrip);
            _pnlNoSelection.Visible = false;
            _pnlRatingForm.Visible = true;
        }

        private void LoadTripIntoForm(Trip trip)
        {
            _lblSelectedTrip.Text = $"{trip.Pickup?.Name ?? "–"}  →  {trip.Destination?.Name ?? "–"}";

            // Reset form
            _txtComment.Clear();
            _lblSuccess.Visible = false;
            _btnSubmit.Enabled = true;
            _btnSubmit.Text = "⭐ Gửi đánh giá";
            SetScore(5);
        }

        private void SetScore(int score)
        {
            _selectedScore = score;

            for (int i = 0; i < 5; i++)
            {
                bool filled = (i + 1) <= score;
                _starButtons[i].ForeColor = filled
                    ? Color.FromArgb(255, 180, 0)   // vàng
                    : Color.FromArgb(200, 200, 200); // xám
            }

            // Comment bắt buộc nếu điểm thấp
            bool needsComment = score < 3;
            _lblCommentHint.Text = needsComment
                ? "Nhận xét (BẮT BUỘC khi điểm < 3 sao)"
                : "Nhận xét (tùy chọn)";
            _lblCommentHint.ForeColor = needsComment ? AppTheme.Danger : AppTheme.TextMuted;
        }

        private async Task OnSubmitClicked()
        {
            if (_selectedTrip == null) return;

            // Validate comment bắt buộc khi điểm thấp
            if (_selectedScore < 3 && string.IsNullOrWhiteSpace(_txtComment.Text))
            {
                MessageBox.Show(
                    "Vui lòng nhập nhận xét khi chấm dưới 3 sao.",
                    "Thiếu thông tin",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                _txtComment.Focus();
                return;
            }

            _btnSubmit.Enabled = false;
            _btnSubmit.Text = "Đang gửi...";

            try
            {
                await _ratingService.CreateRating(
                    _selectedTrip.Id,
                    _shell.Passenger.Id,
                    _selectedScore,
                    _txtComment.Text.Trim());

                // Hiện thông báo thành công
                _lblSuccess.Visible = true;
                _btnSubmit.Text = "✅ Đã gửi";

                // Reload list sau 1.5 giây (để user thấy success)
                await Task.Delay(1500);
                _lblSuccess.Visible = false;
                _selectedTrip = null;
                _pnlNoSelection.Visible = true;
                _pnlRatingForm.Visible = false;
                await LoadPendingTrips();
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show(ex.Message, "Không hợp lệ",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _btnSubmit.Enabled = true;
                _btnSubmit.Text = "⭐ Gửi đánh giá";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi gửi đánh giá: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                _btnSubmit.Enabled = true;
                _btnSubmit.Text = "⭐ Gửi đánh giá";
            }
        }
    }
}