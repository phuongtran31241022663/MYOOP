﻿﻿﻿using OOP.Application.Interfaces;
using OOP.Application.Services.Interfaces;
using OOP.Domain.Entities;
using OOP.Domain.Enums;
using OOP.Presentation.TripForms;
using OOP.Presentation.TripForms;

namespace OOP.Presentation
{
    public class DriverDashboardForm : Form
    {
        // --- Dependencies ---
        private readonly Driver _driver;
        private readonly ITripService _tripService;
        private readonly IUserService _userService;
        private readonly IRouteService _routeService;
        private readonly INotificationService _notification;

        // --- Controls ---
        private Label _lblDriverName = null!;
        private Label _lblStatus = null!;
        private Label _lblWallet = null!;
        private Label _lblRating = null!;
        private Button _btnOnline = null!;
        private Button _btnOffline = null!;
        private DataGridView _dgvTrips = null!;
        private Button _btnAccept = null!;
        private Button _btnStart = null!;
        private Button _btnComplete = null!;
        private Button _btnRoute = null!;
        private Button _btnRefresh = null!;
        private Button _btnLogout = null!;
        private Label _lblCurrentTrip = null!;
        private Panel _pnlCurrentTrip = null!;
        private ListBox _lstLog = null!;

        // --- State ---
        private Trip? _currentTrip;

        // --- Constants ---
        private static readonly Color DarkBg = AppTheme.DarkBg;
        private static readonly Color SideText = Color.White;
        private static readonly Color Blue = AppTheme.Primary;
        private static readonly Color BlueHov = AppTheme.PrimaryHover;
        private static readonly Color Green = AppTheme.Success;
        private static readonly Color GreenHov = AppTheme.SuccessHover;
        private static readonly Color Orange = AppTheme.Warning;
        private static readonly Color OrangeHov = AppTheme.WarningHover;
        private static readonly Color Red = AppTheme.Danger;
        private static readonly Color RedHov = AppTheme.DangerHover;
        private static readonly Color GrayDark = AppTheme.TextMuted;

        public DriverDashboardForm(
            Driver driver,
            ITripService tripService,
            IUserService userService,
            IRouteService routeService,
            INotificationService notification)
        {
            _driver = driver ?? throw new ArgumentNullException(nameof(driver));
            _tripService = tripService ?? throw new ArgumentNullException(nameof(tripService));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _routeService = routeService ?? throw new ArgumentNullException(nameof(routeService));
            _notification = notification ?? throw new ArgumentNullException(nameof(notification));

            InitForm();
            BuildUI();
            _notification.OnTripUpdated += OnTripUpdated;
            Load += async (s, e) =>
            {
                if (_dgvTrips != null)
                    await LoadAvailableTrips();
            };

            FormClosed += (_, _) =>
            {
                _notification.OnTripUpdated -= OnTripUpdated;
            };
        }

        // ─── Setup ───────────────────────────────────────────────────────────────

        private void InitForm()
        {
            Text = $"RideGo – Tài xế: {_driver.Name}";
            Size = new Size(1100, 720);
            MinimumSize = new Size(860, 580);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = AppTheme.PageBg;
            Font = new Font("Segoe UI", 10F);
        }

        private void BuildUI()
        {
            // ── Sidebar ──────────────────────────────────────────────────────────
            var sidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 260,
                BackColor = DarkBg,
                Padding = new Padding(0)
            };

            // Driver info block
            var pnlInfo = new Panel
            {
                Dock = DockStyle.Top,
                Height = 140,
                BackColor = AppTheme.DarkBg,
                Padding = new Padding(18, 16, 18, 12)
            };

            _lblDriverName = MakeSideLabel(_driver.Name.ToUpper(), 13, FontStyle.Bold);
            _lblDriverName.Location = new Point(18, 16);
            _lblDriverName.AutoSize = true;

            _lblWallet = MakeSideLabel($"Ví: {_driver.Wallet:N0} đ", 10.5f);
            _lblWallet.ForeColor = Color.FromArgb(255, 193, 7);
            _lblWallet.Location = new Point(18, 46);
            _lblWallet.AutoSize = true;

            _lblRating = MakeSideLabel($"⭐ {_driver.AverageRating:F1}  |  {_driver.TotalTrips} chuyến", 9.5f);
            _lblRating.ForeColor = Color.FromArgb(180, 180, 180);
            _lblRating.Location = new Point(18, 70);
            _lblRating.AutoSize = true;

            _lblStatus = MakeSideLabel(StatusText(_driver.Status), 10, FontStyle.Bold);
            _lblStatus.ForeColor = StatusColor(_driver.Status);
            _lblStatus.Location = new Point(18, 100);
            _lblStatus.AutoSize = true;

            pnlInfo.Controls.AddRange(new Control[]
                { _lblDriverName, _lblWallet, _lblRating, _lblStatus });

            // Online / Offline buttons
            _btnOnline = MakeSideButton("🟢  Online", Green);
            _btnOnline.Click += async (s, e) => await OnOnlineClicked();

            _btnOffline = MakeSideButton("⛔  Offline", GrayDark);
            _btnOffline.Click += async (s, e) => await OnOfflineClicked();

            // Separator label
            var lblSep = new Label
            {
                Text = "  DANH SÁCH CHUYẾN",
                ForeColor = Color.FromArgb(120, 120, 120),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.BottomLeft,
                BackColor = Color.Transparent,
                Padding = new Padding(18, 0, 0, 4)
            };

            // Logout at bottom
            _btnLogout = MakeSideButton("← Đăng xuất", Red);
            _btnLogout.Dock = DockStyle.Bottom;
            _btnLogout.Click += OnLogoutClicked;
            HoverEffect(_btnLogout, Red, RedHov);

            sidebar.Controls.Add(_btnLogout);   // Bottom first
            sidebar.Controls.Add(lblSep);
            sidebar.Controls.Add(_btnOffline);
            sidebar.Controls.Add(_btnOnline);
            sidebar.Controls.Add(pnlInfo);

            // ── Main content ─────────────────────────────────────────────────────
            var main = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16) };

            // Toolbar
            var toolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = AppTheme.CardBg,
                Padding = new Padding(8, 6, 8, 6)
            };

            _btnRefresh = MakeActionButton("🔄  Làm mới", Blue);
            _btnAccept = MakeActionButton("✅  Nhận cuốc", Green);
            _btnStart = MakeActionButton("🚗  Bắt đầu", Orange);
            _btnComplete = MakeActionButton("🏁  Hoàn thành", Color.FromArgb(102, 16, 242));
            _btnComplete.BackColor = AppTheme.Accent;
            _btnRoute = MakeActionButton("🗺  Lộ trình", AppTheme.Primary);

            HoverEffect(_btnRefresh, Blue, BlueHov);
            HoverEffect(_btnAccept, Green, GreenHov);
            HoverEffect(_btnStart, Orange, OrangeHov);

            _btnRefresh.Click += async (s, e) => await OnRefreshClicked();
            _btnAccept.Click += async (s, e) => await OnAcceptTripClicked();
            _btnStart.Click += async (s, e) => await OnStartTripClicked();
            _btnComplete.Click += async (s, e) => await OnCompleteTripClicked();
            _btnRoute.Click += (s, e) => OnViewRouteClicked();

            // Chỉ hiện nút phù hợp với trạng thái
            UpdateTripButtons();

            toolbar.Controls.AddRange(new Control[]
                { _btnRefresh, _btnAccept, _btnStart, _btnComplete, _btnRoute });

            // Trip grid
            _dgvTrips = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                BackgroundColor = AppTheme.CardBg,
                BorderStyle = BorderStyle.None,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                Font = new Font("Segoe UI", 9.5f),
                ColumnHeadersHeight = 36,
                RowTemplate = { Height = 30 }
            };
            _dgvTrips.Columns.AddRange(
                MakeCol("TripId", "ID", 60, hidden: true),
                MakeCol("Passenger", "Hành khách", 160),
                MakeCol("Pickup", "Điểm đón", 200),
                MakeCol("Destination", "Điểm đến", 200),
                MakeCol("Distance", "Khoảng cách", 100),
                MakeCol("Fare", "Cước phí", 110),
                MakeCol("Status", "Trạng thái", 130)
            );
            _dgvTrips.SelectionChanged += (s, e) => UpdateTripButtons();

            // Current trip info panel (hiện khi đang trong chuyến)
            _pnlCurrentTrip = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 80,
                BackColor = AppTheme.Highlight,
                Padding = new Padding(12, 8, 12, 8),
                Visible = false
            };

            _lblCurrentTrip = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(13, 110, 253),
                BackColor = Color.Transparent
            };
            _pnlCurrentTrip.Controls.Add(_lblCurrentTrip);

            _lstLog = new ListBox
            {
                Dock = DockStyle.Bottom,
                Height = 100,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(70, 70, 70),
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(248, 249, 250)
            };

            main.Controls.Add(_dgvTrips);
            main.Controls.Add(_pnlCurrentTrip);
            main.Controls.Add(_lstLog);
            main.Controls.Add(toolbar);

            Controls.Add(main);
            Controls.Add(sidebar);
        }

        // ─── Events ──────────────────────────────────────────────────────────────

        private async Task OnOnlineClicked()
        {
            try
            {
                _driver.SetAvailable();
                await PersistDriverStatus();
                UpdateStatusUI();
                await LoadAvailableTrips();
            }
            catch (Exception ex) { ShowError(ex.Message); }
        }

        private async Task OnOfflineClicked()
        {
            if (_currentTrip != null)
            {
                MessageBox.Show("Bạn đang có chuyến đi. Vui lòng hoàn thành trước khi offline.",
                    "Không thể offline", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                _driver.SetOffline();
                await PersistDriverStatus();
                UpdateStatusUI();
                _dgvTrips.Rows.Clear();
            }
            catch (Exception ex) { ShowError(ex.Message); }
        }

        private async Task OnRefreshClicked()
        {
            await LoadAvailableTrips();
        }

        private async Task OnAcceptTripClicked()
        {
            var tripId = GetSelectedTripId();
            if (tripId == null) return;

            try
            {
                // Gán driver vào trip — TripService xử lý validate + status change
                await _tripService.AssignDriver(tripId.Value, _driver.Id);

                _currentTrip = await _tripService.GetTrip(tripId.Value);
                _driver.SetBusy();
                await PersistDriverStatus();

                UpdateStatusUI();
                UpdateCurrentTripPanel();
                UpdateTripButtons();
                await LoadMyActiveTrip();

                MessageBox.Show(
                    $"Đã nhận chuyến!\n" +
                    $"Đón: {_currentTrip?.PickupLocation.Address}\n" +
                    $"Đến: {_currentTrip?.DestinationLocation.Address}",
                    "Nhận cuốc thành công",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (InvalidOperationException ex) { ShowError(ex.Message); }
            catch (Exception ex) { ShowError($"Lỗi hệ thống: {ex.Message}"); }
        }

        private async Task OnStartTripClicked()
        {
            if (_currentTrip == null) return;

            // Nếu còn ở Matched → cần MarkArrived trước
            if (_currentTrip.Status == TripStatus.Matched)
            {
                var confirm = MessageBox.Show(
                    "Bạn đã đến điểm đón chưa?\n(Bước này sẽ thông báo hành khách)",
                    "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (confirm != DialogResult.Yes) return;

                try
                {
                    await _tripService.MarkArrived(_currentTrip.Id);
                    _currentTrip = await _tripService.GetTrip(_currentTrip.Id);
                    UpdateCurrentTripPanel();
                    UpdateTripButtons();
                }
                catch (Exception ex) { ShowError(ex.Message); return; }
            }

            // Arrived → StartTrip
            if (_currentTrip?.Status == TripStatus.Arrived)
            {
                try
                {
                    await _tripService.StartTrip(_currentTrip.Id);
                    _currentTrip = await _tripService.GetTrip(_currentTrip.Id);
                    UpdateCurrentTripPanel();
                    UpdateTripButtons();

                    MessageBox.Show("Chuyến đi đã bắt đầu. Chúc bạn lái xe an toàn! 🚗",
                        "Bắt đầu", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex) { ShowError(ex.Message); }
            }
        }

        private async Task OnCompleteTripClicked()
        {
            if (_currentTrip == null) return;

            var confirm = MessageBox.Show(
                "Xác nhận hoàn thành chuyến đi?\n" +
                "Hệ thống sẽ tính cước và thanh toán tự động.",
                "Hoàn thành chuyến", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            try
            {
                await _tripService.CompleteTrip(_currentTrip.Id);
                var completedTrip = await _tripService.GetTrip(_currentTrip.Id);

                // Refresh driver info từ service (wallet đã được cộng bởi TripService)
                var updatedUser = await _userService.GetUserProfile(_driver.Id);
                if (updatedUser is Driver updatedDriver)
                {
                    // Sync lại các giá trị hiển thị
                    _driver.UpdateLocation(_driver.CurrentLocation); // trigger no-op để force
                }

                _currentTrip = null;
                UpdateStatusUI();
                UpdateCurrentTripPanel();
                UpdateTripButtons();
                await LoadAvailableTrips();

                MessageBox.Show(
                    $"✅ Chuyến đi hoàn thành!\n" +
                    $"Cước phí: {completedTrip?.Fare:N0} đ\n" +
                    $"Thu nhập của bạn đã được cộng vào ví.",
                    "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { ShowError($"Lỗi hoàn thành chuyến: {ex.Message}"); }
        }

        private void OnLogoutClicked(object? sender, EventArgs e)
        {
            if (_currentTrip != null)
            {
                MessageBox.Show("Bạn đang có chuyến đi. Vui lòng hoàn thành trước khi đăng xuất.",
                    "Không thể đăng xuất", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                "Bạn có chắc muốn đăng xuất?",
                "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirm == DialogResult.Yes)
                Close();
        }

        // ─── Data loading ─────────────────────────────────────────────────────────

        // Load các trip Requested mà driver có thể nhận (chưa có driver, đúng loại xe)
        private async Task LoadAvailableTrips()
        {
            if (_driver.Status == DriverStatus.Offline)
            {
                _dgvTrips.Rows.Clear();
                return;
            }

            try
            {
                var availableTrips = await _tripService.GetAvailableTripsForDriver(_driver.Id);
                var allDriverTrips = await _tripService.GetTripHistory(_driver.Id);

                _dgvTrips.Rows.Clear();

                // Hiện chuyến đang được gán cho driver này (Matched/Arrived/Ongoing)
                var myActiveTrips = allDriverTrips
                    .Where(t => t.DriverId == _driver.Id &&
                               (t.Status == TripStatus.Matched ||
                                t.Status == TripStatus.Arrived ||
                                t.Status == TripStatus.Ongoing))
                    .ToList();

                // Nếu đang có trip active → set _currentTrip
                if (_currentTrip == null && myActiveTrips.Any())
                    _currentTrip = myActiveTrips.First();

                foreach (var t in myActiveTrips)
                    AddTripRow(t, isCurrentTrip: true);

                // Requested trips phù hợp loại xe để driver nhận
                foreach (var t in availableTrips)
                    AddTripRow(t, isCurrentTrip: false);

                UpdateCurrentTripPanel();
                UpdateTripButtons();
            }
            catch (Exception ex) { ShowError(ex.Message); }
        }

        // Reload trip hiện tại sau khi accept
        private async Task LoadMyActiveTrip()
        {
            _dgvTrips.Rows.Clear();
            if (_currentTrip == null) return;

            var refreshed = await _tripService.GetTrip(_currentTrip.Id);
            if (refreshed != null)
            {
                _currentTrip = refreshed;
                AddTripRow(refreshed, isCurrentTrip: true);
            }
        }

        private void AddTripRow(Trip t, bool isCurrentTrip = false)
        {
            int rowIdx = _dgvTrips.Rows.Add(
                t.Id,
                t.PassengerId.ToString()[..8] + "...",
                t.PickupLocation.Address,
                t.DestinationLocation.Address,
                t.Distance > 0 ? $"{t.Distance:F1} km" : "–",
                t.Fare > 0 ? $"{t.Fare:N0} đ" : "–",
                StatusLabel(t.Status));

            if (isCurrentTrip)
            {
                _dgvTrips.Rows[rowIdx].DefaultCellStyle.BackColor = Color.FromArgb(230, 245, 255);
                _dgvTrips.Rows[rowIdx].DefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            }
        }

        // ─── UI state helpers ─────────────────────────────────────────────────────

        private void UpdateStatusUI()
        {
            _lblStatus.Text = StatusText(_driver.Status);
            _lblStatus.ForeColor = StatusColor(_driver.Status);
            _lblWallet.Text = $"Ví: {_driver.Wallet:N0} đ";
            _lblRating.Text = $"⭐ {_driver.AverageRating:F1}  |  {_driver.TotalTrips} chuyến";

            bool isOnline = _driver.Status != DriverStatus.Offline;
            _btnOnline.Enabled = !isOnline;
            _btnOffline.Enabled = isOnline;
        }

        private void UpdateCurrentTripPanel()
        {
            if (_currentTrip == null)
            {
                _pnlCurrentTrip.Visible = false;
                return;
            }

            _pnlCurrentTrip.Visible = true;
            _lblCurrentTrip.Text =
                $"🚗 Chuyến hiện tại  [{StatusLabel(_currentTrip.Status)}]   " +
                $"Đón: {_currentTrip.PickupLocation.Name}  →  " +
                $"Đến: {_currentTrip.DestinationLocation.Name}   " +
                $"Cước: {(_currentTrip.Fare > 0 ? _currentTrip.Fare.ToString("N0") + " đ" : "Chưa tính")}";
        }

        // Ẩn/hiện nút theo trạng thái
        private void UpdateTripButtons()
        {
            if (_dgvTrips == null || _btnAccept == null || _btnStart == null || _btnComplete == null || _btnRoute == null)
                return;

            bool hasSelection = _dgvTrips.CurrentRow != null;
            var tripStatus = _currentTrip?.Status;

            // Accept: có dòng được chọn, chưa có trip hiện tại, driver Available
            _btnAccept.Visible = _currentTrip == null
                              && _driver.Status == DriverStatus.Available
                              && hasSelection;

            // Start: đang có trip Matched hoặc Arrived
            _btnStart.Visible = tripStatus == TripStatus.Matched
                             || tripStatus == TripStatus.Arrived;

            // Complete: đang Ongoing
            _btnComplete.Visible = tripStatus == TripStatus.Ongoing;
            _btnRoute.Visible = hasSelection || _currentTrip != null;

            // Đổi text của Start theo trạng thái
            if (_btnStart.Visible)
                _btnStart.Text = tripStatus == TripStatus.Matched
                    ? "📍  Đã đến nơi đón"
                    : "🚗  Bắt đầu chuyến";
        }

        private void OnViewRouteClicked()
        {
            var selected = GetSelectedTripId();
            var tripId = selected ?? _currentTrip?.Id;
            if (tripId == null)
            {
                MessageBox.Show("Chưa chọn chuyến đi.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var form = new DriverTripForm(
                tripId.Value,
                _driver.Id,
                _tripService,
                _routeService,
                _userService);
            form.StartPosition = FormStartPosition.CenterParent;
            form.ShowDialog(this);
        }

        private async void OnTripUpdated(Guid tripId, string message)
        {
            try
            {
                var trip = await _tripService.GetTrip(tripId);
                if (trip?.DriverId != _driver.Id) return;

                if (InvokeRequired)
                {
                    BeginInvoke(() => OnTripUpdated(tripId, message));
                    return;
                }

                if (_lstLog.Items.Count >= 200) _lstLog.Items.RemoveAt(0);
                _lstLog.Items.Add($"[{DateTime.Now:HH:mm}] {message}");
                _lstLog.TopIndex = _lstLog.Items.Count - 1;
            }
            catch { }
        }

        // Persist trạng thái driver qua UserService (để lưu vào storage)
        private async Task PersistDriverStatus()
        {
            // UserService không có UpdateDriverStatus() riêng
            // — gọi UpdateUserProfile với thông tin hiện tại để trigger Save()
            // Trong app thật nên thêm method UpdateDriverStatus vào IUserService
            await _userService.UpdateUserProfile(_driver.Id, _driver.Name, _driver.Phone);
        }

        // ─── Grid helpers ─────────────────────────────────────────────────────────

        private Guid? GetSelectedTripId()
        {
            if (_dgvTrips.CurrentRow == null)
            {
                MessageBox.Show("Vui lòng chọn một chuyến đi.",
                    "Chưa chọn", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return null;
            }
            return (Guid)_dgvTrips.CurrentRow.Cells["TripId"].Value;
        }

        private static DataGridViewTextBoxColumn MakeCol(
            string name, string header, int width, bool hidden = false)
        {
            return new DataGridViewTextBoxColumn
            {
                Name = name,
                HeaderText = header,
                Width = width,
                Visible = !hidden,
                SortMode = DataGridViewColumnSortMode.Automatic,
                DefaultCellStyle = new DataGridViewCellStyle
                { Padding = new Padding(4, 0, 4, 0) }
            };
        }

        // ─── UI factories ─────────────────────────────────────────────────────────

        private static Label MakeSideLabel(string text, float size = 10f,
            FontStyle style = FontStyle.Regular)
        {
            return new Label
            {
                Text = text,
                ForeColor = SideText,
                Font = new Font("Segoe UI", size, style),
                BackColor = Color.Transparent,
                AutoSize = true
            };
        }

        private static Button MakeSideButton(string text, Color color)
        {
            var btn = new Button
            {
                Text = text,
                Dock = DockStyle.Top,
                Height = 46,
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Margin = new Padding(0)
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private static Button MakeActionButton(string text, Color color)
        {
            var btn = new Button
            {
                Text = text,
                Dock = DockStyle.Left,
                Width = 150,
                Height = 36,
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Margin = new Padding(0, 0, 6, 0)
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private static void HoverEffect(Button btn, Color normal, Color hover)
        {
            btn.MouseEnter += (s, e) => { if (btn.Enabled) btn.BackColor = hover; };
            btn.MouseLeave += (s, e) => { if (btn.Enabled) btn.BackColor = normal; };
        }

        // ─── Labels ───────────────────────────────────────────────────────────────

        private static string StatusText(DriverStatus status) => status switch
        {
            DriverStatus.Available => "🟢 Đang hoạt động",
            DriverStatus.Busy => "🟡 Đang có chuyến",
            DriverStatus.Offline => "⛔ Offline",
            _ => status.ToString()
        };

        private static Color StatusColor(DriverStatus status) => status switch
        {
            DriverStatus.Available => Color.LimeGreen,
            DriverStatus.Busy => Color.Orange,
            DriverStatus.Offline => Color.FromArgb(150, 150, 150),
            _ => Color.White
        };

        private static string StatusLabel(TripStatus status) => status switch
        {
            TripStatus.Requested => "⏳ Chờ tài xế",
            TripStatus.Matched => "🤝 Đã nhận",
            TripStatus.Arrived => "📍 Đã đến nơi đón",
            TripStatus.Ongoing => "🚗 Đang chạy",
            TripStatus.Completed => "✅ Hoàn thành",
            TripStatus.Cancelled => "❌ Đã hủy",
            _ => status.ToString()
        };

        private static void ShowError(string message) =>
            MessageBox.Show(message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}





