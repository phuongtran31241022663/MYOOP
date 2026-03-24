using System.ComponentModel;
using OOP.Application.Services.Interfaces;
using OOP.Domain.Entities;
using OOP.Domain.Enums;
using OOP.Presentation.BaseForms;
using OOP.Presentation.Common.Theme;

namespace OOP.Presentation
{
    /// <summary>
    /// Shell duy nhất cho Admin — thay thế AdminDashboardForm.
    /// Admin đã dùng TabControl nội bộ nên thực chất là single-window.
    /// Đây chỉ là rename + fix encoding, giữ nguyên 100% logic.
    /// </summary>
    public class AdminShell : BaseDashboardForm
    {
        // ── Dependencies ──────────────────────────────────────────────────────
        private readonly Admin _admin;
        private readonly IAdminService _adminService;

        // ── DataGridViews ─────────────────────────────────────────────────────
        private DataGridView _dgvUsers = null!;
        private DataGridView _dgvDrivers = null!;
        private DataGridView _dgvPassengers = null!;
        private DataGridView _dgvTrips = null!;
        private DataGridView _dgvFareRules = null!;

        // ── Stats labels ──────────────────────────────────────────────────────
        private Label _lblStatsTotalUsers = null!;
        private Label _lblStatsActiveDrivers = null!;
        private Label _lblStatsOnTripDrivers = null!;
        private Label _lblStatsOngoingTrips = null!;

        // ── Report labels ─────────────────────────────────────────────────────
        private Label _lblTotalTrips = null!;
        private Label _lblTotalRevenue = null!;
        private Label _lblDriverIncome = null!;
        private Label _lblCommission = null!;

        // ── Search boxes ──────────────────────────────────────────────────────
        private TextBox _txtSearchUsers = null!;
        private TextBox _txtSearchDrivers = null!;
        private TextBox _txtSearchPassengers = null!;
        private TextBox _txtSearchTrips = null!;

        // ── TabControl ────────────────────────────────────────────────────────
        private TabControl _tabs = null!;

        // ── Cached data ───────────────────────────────────────────────────────
        private List<User> _allUsers = new();
        private List<Driver> _allDrivers = new();
        private List<Passenger> _allPassengers = new();
        private List<Trip> _allTrips = new();

        // ── Tab indices ───────────────────────────────────────────────────────
        private const int TAB_DASHBOARD = 0;
        private const int TAB_USERS = 1;
        private const int TAB_DRIVERS = 2;
        private const int TAB_PASSENGERS = 3;
        private const int TAB_TRIPS = 4;
        private const int TAB_FARE_RULES = 5;
        private const int TAB_REPORTS = 6;

        // ─────────────────────────────────────────────────────────────────────
        public AdminShell(Admin admin, IAdminService adminService)
        {
            _admin = admin ?? throw new ArgumentNullException(nameof(admin));
            _adminService = adminService ?? throw new ArgumentNullException(nameof(adminService));

            Text = $"Quản trị hệ thống  ({_admin.Name})";
            Size = new Size(1200, 760);
            MinimumSize = new Size(900, 600);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = AppTheme.PageBg;
            Font = new Font("Segoe UI", 10F);

            BuildUI();
            Shown += async (_, _) => await LoadAllData();
        }

        // ── Build UI ──────────────────────────────────────────────────────────

        private void BuildUI()
        {
            var sidebar = BuildSidebar();

            var main = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.PageBg };

            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 54,
                BackColor = AppTheme.Primary,
                Padding = new Padding(16, 0, 16, 0)
            };
            var headerLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));

            var lblTitle = new Label
            {
                Text = "🔧  Điều khiển Admin",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };
            var btnLogout = new Button
            {
                Text = "← Đăng xuất",
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(200, 35, 51),
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            btnLogout.FlatAppearance.BorderSize = 0;
            btnLogout.Click += OnLogoutClicked;
            headerLayout.Controls.Add(lblTitle, 0, 0);
            headerLayout.Controls.Add(btnLogout, 1, 0);
            header.Controls.Add(headerLayout);

            var statsBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 96,
                BackColor = Color.FromArgb(245, 247, 250),
                Padding = new Padding(16, 10, 16, 10)
            };
            BuildStatsBar(statsBar);

            _tabs = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10F) };
            _tabs.TabPages.Add(BuildDashboardTab());
            _tabs.TabPages.Add(BuildUsersTab());
            _tabs.TabPages.Add(BuildDriversTab());
            _tabs.TabPages.Add(BuildPassengersTab());
            _tabs.TabPages.Add(BuildTripsTab());
            _tabs.TabPages.Add(BuildFareRulesTab());
            _tabs.TabPages.Add(BuildReportsTab());

            main.Controls.Add(_tabs);
            main.Controls.Add(statsBar);
            main.Controls.Add(header);
            Controls.Add(main);
            Controls.Add(sidebar);
        }

        // ── Sidebar ───────────────────────────────────────────────────────────

        private Panel BuildSidebar()
        {
            var sidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 200,
                BackColor = AppTheme.SidebarBg
            };

            var lblLogo = new Label
            {
                Text = "G",
                Dock = DockStyle.Top,
                Height = 54,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(20, 0, 0, 0),
                BackColor = AppTheme.Primary
            };

            var navStack = new Panel { Dock = DockStyle.Fill };

            var btnReports = MakeSideBtn("📊  Báo cáo");
            var btnFareRules = MakeSideBtn("💰  Bảng giá");
            var btnTrips = MakeSideBtn("🚗  Chuyến đi");
            var btnPassengers = MakeSideBtn("👥  Hành khách");
            var btnDrivers = MakeSideBtn("🛵  Tài xế");
            var btnUsers = MakeSideBtn("👤  Người dùng");
            var btnDashboard = MakeSideBtn("🏠  Dashboard");

            btnDashboard.Click += (_, _) => _tabs.SelectedIndex = TAB_DASHBOARD;
            btnUsers.Click += (_, _) => _tabs.SelectedIndex = TAB_USERS;
            btnDrivers.Click += (_, _) => _tabs.SelectedIndex = TAB_DRIVERS;
            btnPassengers.Click += (_, _) => _tabs.SelectedIndex = TAB_PASSENGERS;
            btnTrips.Click += (_, _) => _tabs.SelectedIndex = TAB_TRIPS;
            btnFareRules.Click += (_, _) => _tabs.SelectedIndex = TAB_FARE_RULES;
            btnReports.Click += (_, _) => _tabs.SelectedIndex = TAB_REPORTS;

            var btnLogout = MakeSideBtn("← Đăng xuất");
            btnLogout.BackColor = Color.FromArgb(160, 30, 40);
            btnLogout.Dock = DockStyle.Bottom;
            btnLogout.Click += OnLogoutClicked;

            // Thêm ngược để Dock=Top xếp đúng thứ tự
            navStack.Controls.Add(btnReports);
            navStack.Controls.Add(btnFareRules);
            navStack.Controls.Add(btnTrips);
            navStack.Controls.Add(btnPassengers);
            navStack.Controls.Add(btnDrivers);
            navStack.Controls.Add(btnUsers);
            navStack.Controls.Add(btnDashboard);

            sidebar.Controls.Add(btnLogout);
            sidebar.Controls.Add(navStack);
            sidebar.Controls.Add(lblLogo);
            return sidebar;
        }

        private static Button MakeSideBtn(string text)
        {
            var btn = new Button
            {
                Text = text,
                Dock = DockStyle.Top,
                Height = 46,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(20, 0, 0, 0),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.MouseEnter += (_, _) => btn.BackColor = AppTheme.SidebarHover;
            btn.MouseLeave += (_, _) => btn.BackColor = Color.Transparent;
            return btn;
        }

        // ── Stats bar ─────────────────────────────────────────────────────────

        private void BuildStatsBar(Panel container)
        {
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1 };
            for (int i = 0; i < 4; i++)
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

            _lblStatsTotalUsers = AddStatCard(layout, 0, "👥", "Tổng người dùng", Color.FromArgb(0, 123, 255));
            _lblStatsActiveDrivers = AddStatCard(layout, 1, "🟢", "Tài xế đang hoạt động", Color.FromArgb(40, 167, 69));
            _lblStatsOnTripDrivers = AddStatCard(layout, 2, "🔴", "Tài xế đang bận", Color.FromArgb(255, 193, 7));
            _lblStatsOngoingTrips = AddStatCard(layout, 3, "🚗", "Chuyến đang diễn ra", Color.FromArgb(23, 162, 184));
            container.Controls.Add(layout);
        }

        private static Label AddStatCard(TableLayoutPanel layout, int col, string icon, string title, Color accent)
        {
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(6),
                BackColor = Color.White,
                Padding = new Padding(12),
                BorderStyle = BorderStyle.FixedSingle
            };
            var lblIcon = new Label { Text = icon, Font = new Font("Segoe UI", 14f), Location = new Point(12, 10), AutoSize = true };
            var lblTitle = new Label { Text = title, Font = new Font("Segoe UI", 8.5f), ForeColor = Color.FromArgb(108, 117, 125), Location = new Point(44, 12), AutoSize = true };
            var lblValue = new Label { Text = "--", Font = new Font("Segoe UI", 22f, FontStyle.Bold), ForeColor = accent, Location = new Point(12, 36), AutoSize = true };
            var accentBar = new Panel { Dock = DockStyle.Bottom, Height = 3, BackColor = accent };
            card.Controls.Add(accentBar);
            card.Controls.Add(lblValue);
            card.Controls.Add(lblTitle);
            card.Controls.Add(lblIcon);
            layout.Controls.Add(card, col, 0);
            return lblValue;
        }

        // ── Tabs ──────────────────────────────────────────────────────────────

        private TabPage BuildDashboardTab()
        {
            var page = new TabPage("🏠  Dashboard");
            var lbl = new Label
            {
                Text = "Chào mừng đến hệ thống quản trị.\nChọn tab bên trên hoặc mục trong sidebar để bắt đầu.",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 12),
                ForeColor = AppTheme.TextMuted
            };
            page.Controls.Add(lbl);
            return page;
        }

        private TabPage BuildUsersTab()
        {
            var page = new TabPage("👤  Người dùng");
            var searchPanel = MakeSearchPanel(out _txtSearchUsers, "Tìm theo tên, SĐT...");
            _txtSearchUsers.TextChanged += async (_, _) => await FilterUsers();

            var toolbar = MakeToolbar();
            var btnRefresh = MakeToolbarBtn("🔄  Làm mới", AppTheme.Primary);
            var btnToggle = MakeToolbarBtn("🔒  Khóa/Mở", AppTheme.Warning);
            btnRefresh.Click += async (_, _) => await LoadUsers();
            btnToggle.Click += async (_, _) => await ToggleUserActive();
            toolbar.Controls.Add(btnRefresh);
            toolbar.Controls.Add(btnToggle);

            _dgvUsers = MakeGrid();
            _dgvUsers.Columns.AddRange(
                MakeCol("UserId", "ID", 60, hidden: true),
                MakeCol("Name", "Họ tên", 200),
                MakeCol("Phone", "SĐT", 140),
                MakeCol("Role", "Vai trò", 100),
                MakeCol("Active", "Trạng thái", 110),
                MakeCol("JoinedAt", "Ngày tham gia", 150)
            );

            page.Controls.Add(_dgvUsers);
            page.Controls.Add(toolbar);
            page.Controls.Add(searchPanel);
            return page;
        }

        private TabPage BuildDriversTab()
        {
            var page = new TabPage("🛵  Tài xế");
            var searchPanel = MakeSearchPanel(out _txtSearchDrivers, "Tìm theo tên, SĐT...");
            _txtSearchDrivers.TextChanged += async (_, _) => await FilterDrivers();

            var toolbar = MakeToolbar();
            var btnRefresh = MakeToolbarBtn("🔄  Làm mới", AppTheme.Primary);
            var btnActivate = MakeToolbarBtn("✅  Kích hoạt", AppTheme.Success);
            var btnDeactive = MakeToolbarBtn("🔒  Vô hiệu hóa", AppTheme.Danger);
            btnRefresh.Click += async (_, _) => await LoadDrivers();
            btnActivate.Click += async (_, _) => await SetDriverActive(true);
            btnDeactive.Click += async (_, _) => await SetDriverActive(false);
            toolbar.Controls.Add(btnRefresh);
            toolbar.Controls.Add(btnActivate);
            toolbar.Controls.Add(btnDeactive);

            _dgvDrivers = MakeGrid();
            _dgvDrivers.Columns.AddRange(
                MakeCol("DriverId", "ID", 60, hidden: true),
                MakeCol("Name", "Họ tên", 180),
                MakeCol("Phone", "SĐT", 130),
                MakeCol("VehicleType", "Loại xe", 110),
                MakeCol("Plate", "Biển số", 110),
                MakeCol("Status", "Trạng thái", 120),
                MakeCol("Rating", "Đánh giá", 100),
                MakeCol("Trips", "Chuyến", 90),
                MakeCol("Active", "Hoạt động", 100)
            );

            page.Controls.Add(_dgvDrivers);
            page.Controls.Add(toolbar);
            page.Controls.Add(searchPanel);
            return page;
        }

        private TabPage BuildPassengersTab()
        {
            var page = new TabPage("👥  Hành khách");
            var searchPanel = MakeSearchPanel(out _txtSearchPassengers, "Tìm theo tên, SĐT...");
            _txtSearchPassengers.TextChanged += async (_, _) => await FilterPassengers();

            var toolbar = MakeToolbar();
            var btnRefresh = MakeToolbarBtn("🔄  Làm mới", AppTheme.Primary);
            var btnActivate = MakeToolbarBtn("✅  Kích hoạt", AppTheme.Success);
            var btnDeactive = MakeToolbarBtn("🔒  Vô hiệu hóa", AppTheme.Danger);
            btnRefresh.Click += async (_, _) => await LoadPassengers();
            btnActivate.Click += async (_, _) => await SetPassengerActive(true);
            btnDeactive.Click += async (_, _) => await SetPassengerActive(false);
            toolbar.Controls.Add(btnRefresh);
            toolbar.Controls.Add(btnActivate);
            toolbar.Controls.Add(btnDeactive);

            _dgvPassengers = MakeGrid();
            _dgvPassengers.Columns.AddRange(
                MakeCol("PassengerId", "ID", 60, hidden: true),
                MakeCol("Name", "Họ tên", 200),
                MakeCol("Phone", "SĐT", 140),
                MakeCol("Trips", "Số chuyến", 100),
                MakeCol("Active", "Hoạt động", 100),
                MakeCol("JoinedAt", "Ngày tham gia", 150)
            );

            page.Controls.Add(_dgvPassengers);
            page.Controls.Add(toolbar);
            page.Controls.Add(searchPanel);
            return page;
        }

        private TabPage BuildTripsTab()
        {
            var page = new TabPage("🚗  Chuyến đi");
            var searchPanel = MakeSearchPanel(out _txtSearchTrips, "Tìm theo địa chỉ...");
            _txtSearchTrips.TextChanged += async (_, _) => await FilterTrips();

            var reportStrip = new Panel
            {
                Dock = DockStyle.Top,
                Height = 38,
                BackColor = Color.FromArgb(240, 248, 255),
                Padding = new Padding(16, 0, 16, 0)
            };
            _lblTotalTrips = MakeStripLabel("Tổng chuyến: --", new Point(0, 0));
            _lblTotalRevenue = MakeStripLabel("Doanh thu: --", new Point(160, 0));
            _lblDriverIncome = MakeStripLabel("Thu nhập TX: --", new Point(380, 0));
            _lblCommission = MakeStripLabel("Hoa hồng: --", new Point(620, 0));
            reportStrip.Controls.Add(_lblTotalTrips);
            reportStrip.Controls.Add(_lblTotalRevenue);
            reportStrip.Controls.Add(_lblDriverIncome);
            reportStrip.Controls.Add(_lblCommission);

            var toolbar = MakeToolbar();
            var btnRefresh = MakeToolbarBtn("🔄  Làm mới", AppTheme.Primary);
            btnRefresh.Click += async (_, _) => await LoadTrips();
            toolbar.Controls.Add(btnRefresh);

            _dgvTrips = MakeGrid();
            _dgvTrips.Columns.AddRange(
                MakeCol("TripId", "ID", 60, hidden: true),
                MakeCol("Passenger", "Hành khách", 160),
                MakeCol("Driver", "Tài xế", 160),
                MakeCol("VehicleType", "Loại xe", 90),
                MakeCol("Pickup", "Điểm đón", 200),
                MakeCol("Destination", "Điểm đến", 200),
                MakeCol("Distance", "Khoảng cách", 110),
                MakeCol("Fare", "Cước phí", 110),
                MakeCol("Status", "Trạng thái", 120),
                MakeCol("RequestedAt", "Thời gian", 140)
            );

            page.Controls.Add(_dgvTrips);
            page.Controls.Add(toolbar);
            page.Controls.Add(reportStrip);
            page.Controls.Add(searchPanel);
            return page;
        }

        private TabPage BuildFareRulesTab()
        {
            var page = new TabPage("💰  Bảng giá");
            var toolbar = MakeToolbar();
            var btnRefresh = MakeToolbarBtn("🔄  Làm mới", AppTheme.Primary);
            var btnAdd = MakeToolbarBtn("➕  Thêm mới", AppTheme.Success);
            var btnEdit = MakeToolbarBtn("✏️  Chỉnh sửa", AppTheme.Success);
            btnRefresh.Click += async (_, _) => await LoadFareRules();
            btnAdd.Click += async (_, _) => await OnAddFareRule();
            btnEdit.Click += async (_, _) => await OnEditFareRule();
            toolbar.Controls.Add(btnRefresh);
            toolbar.Controls.Add(btnAdd);
            toolbar.Controls.Add(btnEdit);

            _dgvFareRules = MakeGrid();
            _dgvFareRules.Columns.AddRange(
                MakeCol("FareRuleId", "ID", 60, hidden: true),
                MakeCol("VehicleType", "Loại xe", 120),
                MakeCol("BaseFare", "Giá mở cửa", 140),
                MakeCol("PricePerKm", "Giá / km", 130),
                MakeCol("CommissionRate", "Hoa hồng (%)", 130),
                MakeCol("UpdatedAt", "Cập nhật lúc", 150)
            );

            page.Controls.Add(_dgvFareRules);
            page.Controls.Add(toolbar);
            return page;
        }

        private TabPage BuildReportsTab()
        {
            var page = new TabPage("📊  Báo cáo");
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 2,
                Padding = new Padding(20)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            layout.Controls.Add(MakeReportCard("🚗", "Tổng chuyến", "--", Color.FromArgb(0, 123, 255)), 0, 0);
            layout.Controls.Add(MakeReportCard("💰", "Tổng doanh thu", "--", Color.FromArgb(40, 167, 69)), 1, 0);

            var detailPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = new Padding(0, 8, 0, 0),
                BorderStyle = BorderStyle.FixedSingle
            };
            detailPanel.Controls.Add(new Label
            {
                Text = "Dữ liệu chi tiết sẽ xuất hiện tại đây.",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.Gray
            });
            layout.SetColumnSpan(detailPanel, 2);
            layout.Controls.Add(detailPanel, 0, 1);
            page.Controls.Add(layout);
            return page;
        }

        private static Panel MakeReportCard(string icon, string title, string value, Color color)
        {
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(8),
                BackColor = Color.White,
                Padding = new Padding(16),
                BorderStyle = BorderStyle.FixedSingle
            };
            card.Controls.Add(new Label { Text = icon, Font = new Font("Segoe UI", 22f), Location = new Point(16, 16), AutoSize = true });
            card.Controls.Add(new Label { Text = title, Font = new Font("Segoe UI", 9.5f), ForeColor = Color.Gray, Location = new Point(60, 16), AutoSize = true });
            card.Controls.Add(new Label { Text = value, Font = new Font("Segoe UI", 16f, FontStyle.Bold), ForeColor = color, Location = new Point(60, 36), AutoSize = true });
            return card;
        }

        // ── Data loading ──────────────────────────────────────────────────────

        private async Task LoadAllData()
        {
            try { await Task.WhenAll(LoadUsers(), LoadDrivers(), LoadPassengers(), LoadTrips(), LoadFareRules()); }
            catch (Exception ex) { ShowError(ex.Message); }
        }

        private async Task LoadUsers()
        {
            try
            {
                _allUsers = (await _adminService.GetAllUsers()).ToList();
                PopulateUsers(_allUsers);
                UpdateStatsBar();
            }
            catch (Exception ex) { ShowError(ex.Message); }
        }

        private void PopulateUsers(IEnumerable<User> users)
        {
            _dgvUsers.Rows.Clear();
            foreach (var u in users.OrderBy(u => u.Name))
            {
                var isActive = (u is Driver d) ? d.IsActive : (u is Passenger p) ? p.IsActive : true;
                var createdAt = (u is Driver d2) ? d2.CreatedAt : (u is Passenger p2) ? p2.CreatedAt : DateTime.UtcNow;
                _dgvUsers.Rows.Add(
                    u.Id,
                    u.Name,
                    u.Phone,
                    u is Driver ? "Tài xế" : u is Passenger ? "Hành khách" : "Admin",
                    isActive ? "✅ Hoạt động" : "🔒 Bị khóa",
                    createdAt.ToString("dd/MM/yyyy"));
            }
        }

        private async Task FilterUsers()
        {
            var q = _txtSearchUsers.Text.ToLower();
            PopulateUsers(_allUsers.Where(u =>
                u.Name.ToLower().Contains(q) || u.Phone.Contains(q)));
        }

        private async Task ToggleUserActive()
        {
            if (_dgvUsers.CurrentRow == null) return;
            var id = (Guid)_dgvUsers.CurrentRow.Cells["UserId"].Value;
            var user = _allUsers.FirstOrDefault(u => u.Id == id);
            if (user == null) return;
            try
            {
                var isActive = (user is Driver d) ? d.IsActive : (user is Passenger p) ? p.IsActive : true;
                if (isActive)
                    await _adminService.DeactivateUser(id, _admin.Id);
                else
                    await _adminService.ActivateUser(id);
                await LoadUsers();
            }
            catch (Exception ex) { ShowError(ex.Message); }
        }

        private async Task LoadDrivers()
        {
            try
            {
                _allDrivers = _allUsers.OfType<Driver>().ToList();
                if (!_allDrivers.Any())
                {
                    var all = await _adminService.GetAllUsers();
                    _allDrivers = all.OfType<Driver>().ToList();
                }
                PopulateDrivers(_allDrivers);
                UpdateStatsBar();
            }
            catch (Exception ex) { ShowError(ex.Message); }
        }

        private void PopulateDrivers(IEnumerable<Driver> drivers)
        {
            _dgvDrivers.Rows.Clear();
            foreach (var d in drivers.OrderBy(d => d.Name))
                _dgvDrivers.Rows.Add(
                    d.Id,
                    d.Name,
                    d.Phone,
                    d.Vehicle?.GetVehicleType() ?? "–",
                    d.Vehicle?.PlateNumber ?? "–",
                    d.Status.ToString(),
                    $"⭐ {d.AverageRating:F1}",
                    d.TotalTrips,
                    d.IsActive ? "✅" : "🔒");
        }

        private async Task FilterDrivers()
        {
            var q = _txtSearchDrivers.Text.ToLower();
            PopulateDrivers(_allDrivers.Where(d =>
                d.Name.ToLower().Contains(q) || d.Phone.Contains(q)));
        }

        private async Task SetDriverActive(bool activate)
        {
            if (_dgvDrivers.CurrentRow == null) return;
            var id = (Guid)_dgvDrivers.CurrentRow.Cells["DriverId"].Value;
            try
            {
                if (activate) await _adminService.ActivateUser(id);
                else await _adminService.DeactivateUser(id, _admin.Id);
                await LoadDrivers();
            }
            catch (Exception ex) { ShowError(ex.Message); }
        }

        private async Task LoadPassengers()
        {
            try
            {
                _allPassengers = _allUsers.OfType<Passenger>().ToList();
                if (!_allPassengers.Any())
                {
                    var all = await _adminService.GetAllUsers();
                    _allPassengers = all.OfType<Passenger>().ToList();
                }
                PopulatePassengers(_allPassengers);
            }
            catch (Exception ex) { ShowError(ex.Message); }
        }

        private void PopulatePassengers(IEnumerable<Passenger> passengers)
        {
            _dgvPassengers.Rows.Clear();
            foreach (var p in passengers.OrderBy(p => p.Name))
                _dgvPassengers.Rows.Add(
                    p.Id,
                    p.Name,
                    p.Phone,
                    p.TotalTrips,
                    p.IsActive ? "✅" : "🔒",
                    p.CreatedAt.ToString("dd/MM/yyyy"));
        }

        private async Task FilterPassengers()
        {
            var q = _txtSearchPassengers.Text.ToLower();
            PopulatePassengers(_allPassengers.Where(p =>
                p.Name.ToLower().Contains(q) || p.Phone.Contains(q)));
        }

        private async Task SetPassengerActive(bool activate)
        {
            if (_dgvPassengers.CurrentRow == null) return;
            var id = (Guid)_dgvPassengers.CurrentRow.Cells["PassengerId"].Value;
            try
            {
                if (activate) await _adminService.ActivateUser(id);
                else await _adminService.DeactivateUser(id, _admin.Id);
                await LoadPassengers();
            }
            catch (Exception ex) { ShowError(ex.Message); }
        }

        private async Task LoadTrips()
        {
            try
            {
                _allTrips = (await _adminService.GetAllTrips()).ToList();
                var users = await _adminService.GetAllUsers();
                var nameMap = users.ToDictionary(u => u.Id, u => u.Name);
                PopulateTrips(_allTrips, nameMap);
                await LoadTripReport();
            }
            catch (Exception ex) { ShowError(ex.Message); }
        }

        private void PopulateTrips(IEnumerable<Trip> trips, Dictionary<Guid, string> nameMap)
        {
            _dgvTrips.Rows.Clear();
            foreach (var t in trips.OrderByDescending(t => t.RequestedAt))
            {
                string passenger = nameMap.TryGetValue(t.PassengerId, out var pn) ? pn : t.PassengerId.ToString()[..8];
                string driver = t.DriverId.HasValue && nameMap.TryGetValue(t.DriverId.Value, out var dn) ? dn : "Chưa có";
                _dgvTrips.Rows.Add(
                    t.Id,
                    passenger,
                    driver,
                    t.VehicleType,
                    t.Pickup?.Address ?? "–",
                    t.Destination?.Address ?? "–",
                    t.Distance > 0 ? $"{t.Distance:F1} km" : "–",
                    t.Fare > 0 ? $"{t.Fare:N0} đ" : "–",
                    TripStatusLabel(t.Status),
                    t.RequestedAt.ToString("dd/MM/yyyy HH:mm"));
            }
        }

        private async Task FilterTrips()
        {
            var q = _txtSearchTrips.Text.ToLower();
            var users = await _adminService.GetAllUsers();
            var nameMap = users.ToDictionary(u => u.Id, u => u.Name);
            PopulateTrips(_allTrips.Where(t =>
                (t.Pickup?.Address.ToLower().Contains(q) ?? false) ||
                (t.Destination?.Address.ToLower().Contains(q) ?? false)), nameMap);
        }

        private async Task LoadTripReport()
        {
            try
            {
                var report = await _adminService.GetTripReport();
                _lblTotalTrips.Text = $"Tổng: {report.TotalTrips} chuyến";
                _lblTotalRevenue.Text = $"Doanh thu: {report.TotalRevenue:N0} đ";
                _lblDriverIncome.Text = $"Thu nhập TX: {report.TotalDriverIncome:N0} đ";
                _lblCommission.Text = $"Hoa hồng: {report.TotalCommission:N0} đ";
            }
            catch
            {
                _lblTotalTrips.Text = "Tổng chuyến: --";
            }
        }

        private async Task LoadFareRules()
        {
            try
            {
                var rules = await _adminService.GetFareRules();
                _dgvFareRules.Rows.Clear();
                foreach (var r in rules)
                    _dgvFareRules.Rows.Add(
                        r.Id,
                        r.VehicleType,
                        $"{r.BaseFare:N0} đ",
                        $"{r.PricePerKm:N0} đ",
                        $"{r.CommissionRate * 100:F0}%",
                        r.UpdatedAt.ToString("dd/MM/yyyy HH:mm"));
            }
            catch (Exception ex) { ShowError(ex.Message); }
        }

        private async Task OnEditFareRule()
        {
            if (_dgvFareRules.CurrentRow == null)
            {
                MessageBox.Show("Vui lòng chọn hàng cần chỉnh sửa.", "Chưa chọn",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var id = (Guid)_dgvFareRules.CurrentRow.Cells["FareRuleId"].Value;
            var rules = await _adminService.GetFareRules();
            var rule = rules.FirstOrDefault(r => r.Id == id);
            if (rule == null) return;

            using var form = new EditFareRuleForm(rule);
            if (form.ShowDialog(this) != DialogResult.OK) return;
            try
            {
                rule.UpdateRule(form.NewBaseFare, form.NewPricePerKm, form.NewCommissionRate);
                await _adminService.UpdateFareRule(rule);
                await LoadFareRules();
                MessageBox.Show("Cập nhật bảng giá thành công!", "Thành công",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { ShowError(ex.Message); }
        }

        private async Task OnAddFareRule()
        {
            using var form = new AddFareRuleForm();
            if (form.ShowDialog(this) != DialogResult.OK) return;
            try
            {
                var rule = new Fare(form.NewVehicleType, form.NewBaseFare,
                                    form.NewPricePerKm, form.NewCommissionRate);
                await _adminService.CreateFareRule(rule);
                await LoadFareRules();
                MessageBox.Show("Thêm bảng giá thành công!", "Thành công",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { ShowError(ex.Message); }
        }

        // ── Stats update ──────────────────────────────────────────────────────

        private void UpdateStatsBar()
        {
            if (InvokeRequired) { BeginInvoke(UpdateStatsBar); return; }
            _lblStatsTotalUsers.Text = _allUsers.Count.ToString();
            _lblStatsActiveDrivers.Text = _allUsers.OfType<Driver>()
                .Count(d => d.IsActive && d.Status == DriverStatus.Active).ToString();
            _lblStatsOnTripDrivers.Text = _allUsers.OfType<Driver>()
                .Count(d => d.IsActive && d.Status == DriverStatus.OnTrip).ToString();
            _lblStatsOngoingTrips.Text = _allTrips.Count(t =>
                t.Status is TripStatus.Matched or TripStatus.Arrived or TripStatus.Started).ToString();
        }

        // ── Logout ────────────────────────────────────────────────────────────

        private void OnLogoutClicked(object? sender, EventArgs e)
        {
            if (MessageBox.Show("Bạn có chắc muốn đăng xuất?", "Xác nhận",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                Close();
        }

        // ── UI factories ──────────────────────────────────────────────────────

        private static Panel MakeSearchPanel(out TextBox txt, string placeholder)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 42,
                BackColor = AppTheme.CardBg,
                Padding = new Padding(8, 5, 8, 5)
            };
            txt = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10),
                PlaceholderText = placeholder
            };
            panel.Controls.Add(txt);
            return panel;
        }

        private static Panel MakeToolbar() => new()
        {
            Dock = DockStyle.Top,
            Height = 48,
            BackColor = AppTheme.CardBg,
            Padding = new Padding(8, 7, 8, 7)
        };

        private static Button MakeToolbarBtn(string text, Color color)
        {
            var btn = new Button
            {
                Text = text,
                Dock = DockStyle.Left,
                Width = 148,
                Height = 34,
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Margin = new Padding(0, 0, 6, 0)
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private static DataGridView MakeGrid() => new()
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AutoGenerateColumns = false,
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

        private static DataGridViewTextBoxColumn MakeCol(string name, string header, int width, bool hidden = false) => new()
        {
            Name = name,
            HeaderText = header,
            Width = width,
            Visible = !hidden,
            SortMode = DataGridViewColumnSortMode.Automatic,
            DefaultCellStyle = new DataGridViewCellStyle { Padding = new Padding(4, 0, 4, 0) }
        };

        private static Label MakeStripLabel(string text, Point loc) => new()
        {
            Text = text,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = AppTheme.TextPrimary,
            Location = loc,
            AutoSize = true,
            BackColor = Color.Transparent
        };

        private static string TripStatusLabel(TripStatus s) => s switch
        {
            TripStatus.Requested => "⏳ Đang tìm",
            TripStatus.Searching => "🔎 Đang tìm",
            TripStatus.Matched => "🤝 Đã ghép",
            TripStatus.Arrived => "📍 Đã đến",
            TripStatus.Started => "🚗 Đang chạy",
            TripStatus.Completed => "✅ Hoàn thành",
            TripStatus.Cancelled => "❌ Đã hủy",
            TripStatus.Timeout => "⌛ Hết thời gian",
            _ => s.ToString()
        };

        private static void ShowError(string msg) =>
            MessageBox.Show(msg, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // EditFareRuleForm — dialog chỉnh sửa bảng giá (giữ nguyên logic)
    // ─────────────────────────────────────────────────────────────────────────

    public class EditFareRuleForm : Form
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public decimal NewBaseFare { get; protected set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public decimal NewPricePerKm { get; protected set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public decimal NewCommissionRate { get; protected set; }

        private readonly NumericUpDown _numBaseFare;
        private readonly NumericUpDown _numPricePerKm;
        private readonly NumericUpDown _numCommission;

        public EditFareRuleForm(Fare rule)
        {
            Text = $"Chỉnh sửa bảng giá – {rule.VehicleType}";
            Size = new Size(420, 340);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false;
            BackColor = Color.White;
            Font = new Font("Segoe UI", 10F);

            var layout = MakeFormLayout(6);
            AddHeaderLabel(layout, $"Loại xe: {rule.VehicleType}", 0);
            _numBaseFare = AddNumRow(layout, "Giá mở cửa (đ):", 1, rule.BaseFare, 0, 500_000);
            _numPricePerKm = AddNumRow(layout, "Giá / km (đ):", 2, rule.PricePerKm, 0, 100_000);
            _numCommission = AddNumRow(layout, "Hoa hồng (%):", 4, rule.CommissionRate * 100m, 0, 100, decimals: 0);
            AddButtons(layout, 5, OnSave);
            Controls.Add(layout);
        }

        private void OnSave(object? s, EventArgs e)
        {
            NewBaseFare = _numBaseFare.Value;
            NewPricePerKm = _numPricePerKm.Value;
            NewCommissionRate = _numCommission.Value / 100m;
            DialogResult = DialogResult.OK;
        }

        protected static TableLayoutPanel MakeFormLayout(int rows)
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = rows,
                Padding = new Padding(24, 16, 24, 12)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 46));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 54));
            for (int i = 0; i < rows; i++)
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            return layout;
        }

        protected static void AddHeaderLabel(TableLayoutPanel layout, string text, int row)
        {
            var lbl = new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 100, 200),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            layout.Controls.Add(lbl, 0, row);
            layout.SetColumnSpan(lbl, 2);
        }

        protected static NumericUpDown AddNumRow(TableLayoutPanel layout,
            string label, int row, decimal value, decimal min, decimal max, int decimals = 0)
        {
            layout.Controls.Add(new Label
            {
                Text = label,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Color.FromArgb(60, 60, 60)
            }, 0, row);

            var num = new NumericUpDown
            {
                Dock = DockStyle.Fill,
                Minimum = min,
                Maximum = max,
                Value = Math.Clamp(value, min, max),
                DecimalPlaces = decimals,
                ThousandsSeparator = decimals == 0,
                Font = new Font("Segoe UI", 10.5f)
            };
            layout.Controls.Add(num, 1, row);
            return num;
        }

        protected void AddButtons(TableLayoutPanel layout, int row, EventHandler saveHandler)
        {
            var panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0, 6, 0, 0)
            };
            var btnCancel = new Button
            {
                Text = "Hủy",
                Width = 90,
                Height = 34,
                DialogResult = DialogResult.Cancel,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            var btnSave = new Button
            {
                Text = "Lưu",
                Width = 90,
                Height = 34,
                BackColor = Color.FromArgb(0, 150, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += saveHandler;
            panel.Controls.Add(btnCancel);
            panel.Controls.Add(btnSave);
            layout.Controls.Add(panel, 0, row);
            layout.SetColumnSpan(panel, 2);
            AcceptButton = btnSave;
            CancelButton = btnCancel;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // AddFareRuleForm — dialog thêm bảng giá mới
    // ─────────────────────────────────────────────────────────────────────────

    public class AddFareRuleForm : EditFareRuleForm
    {
        public string NewVehicleType { get; private set; } = "Motorbike";

        private readonly ComboBox _cmbType;
        private readonly NumericUpDown _numBase;
        private readonly NumericUpDown _numPerKm;
        private readonly NumericUpDown _numCommission2;

        public AddFareRuleForm() : base(new Fare("Motorbike", 0, 0, 0))
        {
            Text = "Thêm bảng giá mới";
            Size = new Size(420, 380);
            Controls.Clear();

            var layout = MakeFormLayout(7);
            AddHeaderLabel(layout, "Thêm loại xe mới", 0);

            layout.Controls.Add(new Label
            {
                Text = "Loại xe:",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Color.FromArgb(60, 60, 60)
            }, 0, 1);

            _cmbType = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10.5f)
            };
            _cmbType.Items.AddRange(new object[] { "Motorbike", "Car" });
            _cmbType.SelectedIndex = 0;
            layout.Controls.Add(_cmbType, 1, 1);

            _numBase = AddNumRow(layout, "Giá mở cửa (đ):", 2, 10_000, 0, 500_000);
            _numPerKm = AddNumRow(layout, "Giá / km (đ):", 3, 5_000, 0, 100_000);
            _numCommission2 = AddNumRow(layout, "Hoa hồng (%):", 5, 20, 0, 100, decimals: 0);
            AddButtons(layout, 6, OnSaveNew);
            Controls.Add(layout);
        }

        private void OnSaveNew(object? s, EventArgs e)
        {
            NewVehicleType = _cmbType.SelectedItem?.ToString() ?? "Motorbike";
            NewBaseFare = _numBase.Value;
            NewPricePerKm = _numPerKm.Value;
            NewCommissionRate = _numCommission2.Value / 100m;
            DialogResult = DialogResult.OK;
        }
    }
}