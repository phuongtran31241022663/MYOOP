using OOP.Application.Services.Interfaces;
using OOP.Application.Services.Models;
using OOP.Domain.Entities;
using OOP.Domain.Enums;
using OOP.Presentation.BaseForms;

namespace OOP.Presentation
{
    public class AdminDashboardForm : BaseDashboardForm
    {
        // --- Dependencies ---
        private readonly Admin _admin;
        private readonly IAdminService _adminService;

        // --- Tab controls ---
        private DataGridView _dgvUsers = null!;
        private DataGridView _dgvTrips = null!;
        private DataGridView _dgvFareRules = null!;
        private Label _lblTotalTrips = null!;
        private Label _lblTotalRevenue = null!;
        private Label _lblTotalDriverIncome = null!;
        private Label _lblTotalCommission = null!;

        // --- Constants ---
        private static readonly Color Blue = AppTheme.Primary;
        private static readonly Color BlueHover = AppTheme.PrimaryHover;
        private static readonly Color Red = AppTheme.Danger;
        private static readonly Color RedHover = AppTheme.DangerHover;
        private static readonly Color Green = AppTheme.Success;
        private static readonly Color Orange = AppTheme.Warning;
        private static readonly Color BgPage = AppTheme.PageBg;

        public AdminDashboardForm(Admin admin, IAdminService adminService)
        {
            _admin = admin ?? throw new ArgumentNullException(nameof(admin));
            _adminService = adminService ?? throw new ArgumentNullException(nameof(adminService));

            InitForm();
            BuildUI();

            Shown += async (s, e) => await LoadAllData();
        }

        // ─── Setup ───────────────────────────────────────────────────────────────

        private void InitForm()
        {
            Text = $"RideGo – Quản trị hệ thống  ({_admin.Name})";
            Size = new Size(1200, 760);
            MinimumSize = new Size(900, 600);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = BgPage;
            Font = new Font("Segoe UI", 10F);
            FormBorderStyle = FormBorderStyle.Sizable;
        }

        private void BuildUI()
        {
            // ── Header ────────────────────────────────────────────────────────────
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 54,
                BackColor = Blue,
                Padding = new Padding(16, 0, 16, 0)
            };

            var lblTitle = new Label
            {
                Text = "🚗  RideGo  –  Bảng điều khiển Admin",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            var btnLogout = new Button
            {
                Text = "Đăng xuất",
                Dock = DockStyle.Right,
                Width = 110,
                FlatStyle = FlatStyle.Flat,
                BackColor = Red,
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            btnLogout.FlatAppearance.BorderSize = 0;
            btnLogout.Click += OnLogoutClicked;

            header.Controls.Add(lblTitle);
            header.Controls.Add(btnLogout);
            Controls.Add(header);

            // ── TabControl ────────────────────────────────────────────────────────
            var tabs = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F)
            };
            tabs.TabPages.Add(BuildUsersTab());
            tabs.TabPages.Add(BuildTripsTab());
            tabs.TabPages.Add(BuildFareRulesTab());
            Controls.Add(tabs);
        }

        // ─── Tab: Người dùng ─────────────────────────────────────────────────────

        private TabPage BuildUsersTab()
        {
            var page = new TabPage("👥  Người dùng");

            // Toolbar
            var toolbar = MakeToolbar();

            var btnRefresh = MakeToolbarButton("🔄  Làm mới", Blue);
            btnRefresh.Click += async (s, e) => await LoadUsers();

            var btnToggleActive = MakeToolbarButton("🔒  Khóa / Mở khoá", Orange);
            btnToggleActive.Click += async (s, e) => await ToggleUserActive();

            toolbar.Controls.Add(btnRefresh);
            toolbar.Controls.Add(btnToggleActive);

            // Grid
            _dgvUsers = MakeGrid();
            _dgvUsers.Columns.AddRange(
                MakeCol("UserId", "ID", 60, hidden: true),
                MakeCol("Name", "Họ tên", 180),
                MakeCol("Phone", "Số điện thoại", 130),
                MakeCol("Role", "Vai trò", 100),
                MakeCol("DriverStatus", "Trạng thái TX", 120),
                MakeCol("VehicleType", "Loại xe", 90),
                MakeCol("IsActive", "Hoạt động", 90),
                MakeCol("Rating", "Đánh giá", 90),
                MakeCol("TotalTrips", "Tổng chuyến", 100)
            );

            page.Controls.Add(_dgvUsers);
            page.Controls.Add(toolbar);
            toolbar.BringToFront();
            return page;
        }

        private async Task LoadUsers()
        {
            try
            {
                var users = await _adminService.GetAllUsers();
                _dgvUsers.Rows.Clear();

                foreach (var u in users)
                {
                    int trips = u is Passenger p ? p.TotalTrips
                              : u is Driver d ? d.TotalTrips
                              : 0;
                    string rating = u is Driver dr ? $"{dr.AverageRating:F1} ⭐" : "—";
                    string driverStatus = u is Driver d1 ? d1.Status.ToString() : "—";
                    string vehicleType = u is Driver d2 ? d2.Vehicle.Type.ToString() : "—";
                    
                    // Xác định role và trạng thái IsActive dựa trên kiểu
                    string role = u.GetType().Name;
                    bool isActive = u switch
                    {
                        Passenger passenger => passenger.IsActive,
                        Driver driver => driver.IsActive,
                        Admin => true, // Admin luôn active
                        _ => true
                    };

                    _dgvUsers.Rows.Add(
                        u.Id,
                        u.Name,
                        u.Phone,
                        role,
                        driverStatus,
                        vehicleType,
                        isActive ? "✅ Hoạt động" : "🔒 Đã khóa",
                        rating,
                        trips);
                }
            }
            catch (Exception ex) { ShowError(ex.Message); }
        }

        private async Task ToggleUserActive()
        {
            if (_dgvUsers.CurrentRow == null) return;

            // 1. Lấy ID của User đang được chọn trên Grid
            var targetUserId = (Guid)(_dgvUsers.CurrentRow.Cells["UserId"].Value ?? Guid.Empty);
            var targetUserName = _dgvUsers.CurrentRow.Cells["Name"].Value?.ToString() ?? string.Empty;

            // 2. KIỂM TRA: Admin không được tự khóa chính mình
            if (targetUserId == _admin.Id)
            {
                MessageBox.Show(
                    "Bạn không thể tự khóa tài khoản của chính mình để tránh mất quyền truy cập hệ thống!",
                    "Cảnh báo bảo mật",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            // 3. Xác nhận hành động (Khóa hoặc Mở khóa)
            // Dựa trên text hiển thị ở cột IsActive để xác định trạng thái hiện tại
            bool currentlyActive = (_dgvUsers.CurrentRow.Cells["IsActive"].Value?.ToString() ?? string.Empty).Contains("Hoạt động");
            string action = currentlyActive ? "khóa" : "mở khóa";

            var confirm = MessageBox.Show(
                $"Bạn có chắc muốn {action} tài khoản '{targetUserName}'?",
                "Xác nhận thay đổi",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            try
            {
                // 4. Gọi Service thực hiện
                if (currentlyActive)
                {
                    await _adminService.DeactivateUser(targetUserId, _admin.Id);
                }
                else
                {
                    await _adminService.ActivateUser(targetUserId);
                }
                // 5. Reload lại danh sách
                await LoadUsers();

                MessageBox.Show($"Đã {action} tài khoản thành công.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                ShowError($"Không thể thực hiện: {ex.Message}");
            }
        }

        // ─── Tab: Chuyến đi ──────────────────────────────────────────────────────

        private TabPage BuildTripsTab()
        {
            var page = new TabPage("🚗  Chuyến đi");

            var reportPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 64,
                BackColor = AppTheme.CardBg,
                Padding = new Padding(16, 8, 16, 8)
            };

            _lblTotalTrips = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = AppTheme.TextPrimary,
                Location = new Point(16, 12),
                Text = "Tổng chuyến: --"
            };

            _lblTotalRevenue = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = AppTheme.TextMuted,
                Location = new Point(16, 34),
                Text = "Doanh thu: --"
            };

            _lblTotalDriverIncome = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = AppTheme.TextMuted,
                Location = new Point(280, 34),
                Text = "Thu nhập tài xế: --"
            };

            _lblTotalCommission = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = AppTheme.TextMuted,
                Location = new Point(560, 34),
                Text = "Hoa hồng: --"
            };

            reportPanel.Controls.Add(_lblTotalTrips);
            reportPanel.Controls.Add(_lblTotalRevenue);
            reportPanel.Controls.Add(_lblTotalDriverIncome);
            reportPanel.Controls.Add(_lblTotalCommission);

            var toolbar = MakeToolbar();

            var btnRefresh = MakeToolbarButton("🔄  Làm mới", Blue);
            btnRefresh.Click += async (s, e) => await LoadTrips();

            toolbar.Controls.Add(btnRefresh);

            _dgvTrips = MakeGrid();
            _dgvTrips.Columns.AddRange(
                MakeCol("TripId", "ID", 60, hidden: true),
                MakeCol("PassengerId", "Hành khách ID", 120, hidden: true),
                MakeCol("DriverId", "Tài xế ID", 120, hidden: true),
                MakeCol("Passenger", "Hành khách", 160),
                MakeCol("Driver", "Tài xế", 160),
                MakeCol("VehicleType", "Loại xe", 90),
                MakeCol("Pickup", "Điểm đón", 200),
                MakeCol("Destination", "Điểm đến", 200),
                MakeCol("Distance", "Khoảng cách", 110),
                MakeCol("Duration", "Thời gian", 90),
                MakeCol("Fare", "Cước phí", 110),
                MakeCol("Status", "Trạng thái", 110),
                MakeCol("RequestedAt", "Thời gian", 140)
            );

            page.Controls.Add(_dgvTrips);
            page.Controls.Add(toolbar);
            page.Controls.Add(reportPanel);
            toolbar.BringToFront();
            reportPanel.BringToFront();
            return page;
        }

        private async Task LoadTrips()
        {
            try
            {
                var trips = await _adminService.GetAllTrips();
                var users = await _adminService.GetAllUsers();

                // Build lookup name map để hiển thị tên thay vì Guid
                var nameMap = users.ToDictionary(u => u.Id, u => u.Name);

                _dgvTrips.Rows.Clear();
                foreach (var t in trips.OrderByDescending(t => t.RequestedAt))
                {
                    string passengerName = nameMap.TryGetValue(t.PassengerId, out var pn)
                                          ? pn : t.PassengerId.ToString()[..8];
                    string driverName = t.DriverId.HasValue &&
                                          nameMap.TryGetValue(t.DriverId.Value, out var dn)
                                          ? dn : "Chưa có";

                    _dgvTrips.Rows.Add(
                        t.Id,
                        t.PassengerId,
                        t.DriverId ?? Guid.Empty,
                        passengerName,
                        driverName,
                        t.VehicleType.ToString(),
                        t.PickupLocation?.Address ?? "–",
                        t.DestinationLocation?.Address ?? "–",
                        t.Distance > 0 ? $"{t.Distance:F1} km" : "–",
                        t.Duration > 0 ? $"{t.Duration:F0} phút" : "–",
                        t.Fare > 0 ? $"{t.Fare:N0} đ" : "–",
                        StatusLabel(t.Status),
                        t.RequestedAt.ToString("dd/MM/yyyy HH:mm"));
                }

                await LoadTripReport();
            }
            catch (Exception ex) { ShowError(ex.Message); }
        }

        private async Task LoadTripReport()
        {
            if (_lblTotalTrips == null) return;

            try
            {
                TripReport report = await _adminService.GetTripReport();

                _lblTotalTrips.Text = $"Tổng chuyến: {report.TotalTrips}";
                _lblTotalRevenue.Text = $"Doanh thu: {report.TotalRevenue:N0} đ";
                _lblTotalDriverIncome.Text = $"Thu nhập tài xế: {report.TotalDriverIncome:N0} đ";
                _lblTotalCommission.Text = $"Hoa hồng: {report.TotalCommission:N0} đ";
            }
            catch
            {
                _lblTotalTrips.Text = "Tổng chuyến: --";
                _lblTotalRevenue.Text = "Doanh thu: --";
                _lblTotalDriverIncome.Text = "Thu nhập tài xế: --";
                _lblTotalCommission.Text = "Hoa hồng: --";
            }
        }

        // ─── Tab: Bảng giá ───────────────────────────────────────────────────────

        private TabPage BuildFareRulesTab()
        {
            var page = new TabPage("💰  Bảng giá");

            var toolbar = MakeToolbar();

            var btnRefresh = MakeToolbarButton("🔄  Làm mới", Blue);
            btnRefresh.Click += async (s, e) => await LoadFareRules();

            var btnAdd = MakeToolbarButton("➕  Thêm", Green);
            btnAdd.Click += async (s, e) => await OnAddFareRuleClicked();

            var btnEdit = MakeToolbarButton("✏️  Chỉnh sửa", Green);
            btnEdit.Click += async (s, e) => await OnEditFareRuleClicked();

            toolbar.Controls.Add(btnRefresh);
            toolbar.Controls.Add(btnAdd);
            toolbar.Controls.Add(btnEdit);

            _dgvFareRules = MakeGrid();
            _dgvFareRules.Columns.AddRange(
                MakeCol("FareRuleId", "ID", 60, hidden: true),
                MakeCol("VehicleType", "Loại xe", 100),
                MakeCol("BaseFare", "Giá mở cửa", 120),
                MakeCol("PricePerKm", "Giá / km", 120),
                MakeCol("MinimumFare", "Giá tối thiểu", 130),
                MakeCol("CommissionRate", "Hoa hồng (%)", 110),
                MakeCol("UpdatedAt", "Cập nhật lúc", 140)
            );

            page.Controls.Add(_dgvFareRules);
            page.Controls.Add(toolbar);
            toolbar.BringToFront();
            return page;
        }

        private async Task LoadFareRules()
        {
            try
            {
                var rules = await _adminService.GetFareRules();
                _dgvFareRules.Rows.Clear();

                foreach (var r in rules)
                {
                    _dgvFareRules.Rows.Add(
                        r.Id,
                        r.VehicleType.ToString(),
                        $"{r.BaseFare:N0} đ",
                        $"{r.PricePerKm:N0} đ",
                        $"{r.CommissionRate * 100:F0}%",
                        r.UpdatedAt.ToString("dd/MM/yyyy HH:mm"));
                }
            }
            catch (Exception ex) { ShowError(ex.Message); }
        }

        // ─── Events ──────────────────────────────────────────────────────────────

        private async Task LoadAllData()
        {
            var loadUsers = LoadUsers();
            var loadTrips = LoadTrips();
            var loadFareRules = LoadFareRules();

            try
            {
                await Task.WhenAll(loadUsers, loadTrips, loadFareRules);
            }
            catch
            {
                var errors = new List<string>();
                if (loadUsers.Exception != null)
                    errors.AddRange(loadUsers.Exception.InnerExceptions.Select(e => e.Message));
                if (loadTrips.Exception != null)
                    errors.AddRange(loadTrips.Exception.InnerExceptions.Select(e => e.Message));
                if (loadFareRules.Exception != null)
                    errors.AddRange(loadFareRules.Exception.InnerExceptions.Select(e => e.Message));

                ShowError(errors.Count > 0
                    ? string.Join("\n", errors.Distinct())
                    : "Không thể tải dữ liệu.");
            }
        }

        private async Task OnEditFareRuleClicked()
        {
            if (_dgvFareRules.CurrentRow == null)
            {
                MessageBox.Show("Vui lòng chọn một dòng bảng giá để chỉnh sửa.",
                    "Chưa chọn", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var ruleId = (Guid)(_dgvFareRules.CurrentRow.Cells["FareRuleId"].Value ?? Guid.Empty);

            var rules = await _adminService.GetFareRules();
            var selectedRule = rules.FirstOrDefault(r => r.Id == ruleId);
            if (selectedRule == null) return;

            using var editForm = new EditFareRuleForm(selectedRule);
            if (editForm.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                selectedRule.UpdateRule(
                    editForm.NewBaseFare,
                    editForm.NewPricePerKm,
                    editForm.NewCommissionRate);

                await _adminService.UpdateFareRule(selectedRule);

                await LoadFareRules();

                MessageBox.Show("Cập nhật bảng giá thành công!",
                    "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (ArgumentException ex)
            {
                ShowError($"Dữ liệu không hợp lệ: {ex.Message}");
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private async Task OnAddFareRuleClicked()
        {
            using var addForm = new AddFareRuleForm();
            if (addForm.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                var rule = new Fare(
                    addForm.NewVehicleType,
                    addForm.NewBaseFare,
                    addForm.NewPricePerKm,
                    addForm.NewCommissionRate);

                await _adminService.CreateFareRule(rule);
                await LoadFareRules();

                MessageBox.Show("Tạo bảng giá thành công!",
                    "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (ArgumentException ex)
            {
                ShowError($"Dữ liệu không hợp lệ: {ex.Message}");
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private void OnLogoutClicked(object? sender, EventArgs e)
        {
            var confirm = MessageBox.Show(
                "Bạn có chắc muốn đăng xuất?",
                "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirm == DialogResult.Yes)
                Close(); // MainForm vẫn đang chạy bên dưới
        }

        // ─── UI Helpers ──────────────────────────────────────────────────────────

        private static Panel MakeToolbar()
        {
            return new Panel
            {
                Dock = DockStyle.Top,
                Height = 48,
                BackColor = AppTheme.CardBg,
                Padding = new Padding(8, 6, 8, 6)
            };
        }

        private static Button MakeToolbarButton(string text, Color color)
        {
            var btn = new Button
            {
                Text = text,
                Height = 34,
                Width = 150,
                Dock = DockStyle.Left,
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

        private static DataGridView MakeGrid()
        {
            return new DataGridView
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
        }

        // Tạo cột DataGridView — dùng DataGridViewTextBoxColumn vì data được bind thủ công
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
                DefaultCellStyle = new DataGridViewCellStyle { Padding = new Padding(4, 0, 4, 0) }
            };
        }

        private static string StatusLabel(TripStatus status) => status switch
        {
            TripStatus.Requested => "⏳ Đang tìm tài xế",
            TripStatus.Searching => "🔎 Đang tìm tài xế",
            TripStatus.Matched => "🤝 Đã ghép",
            TripStatus.Arrived => "📍 Tài xế đã đến",
            TripStatus.Started => "🚗 Đang chạy",
            TripStatus.Completed => "✅ Hoàn thành",
            TripStatus.Cancelled => "❌ Đã hủy",
            TripStatus.Timeout => "⌛ Hết thời gian",
            _ => status.ToString()
        };

        private static void ShowError(string message) =>
            MessageBox.Show(message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    // ─── EditFareRuleForm (dialog nội tuyến) ──────────────────────────────────────

    public class EditFareRuleForm : Form
    {
        // Kết quả sau khi nhấn OK — AdminDashboardForm đọc từ đây
        public decimal NewBaseFare { get; private set; }
        public decimal NewPricePerKm { get; private set; }
        public decimal NewMinimumFare { get; private set; }
        public decimal NewCommissionRate { get; private set; }  // 0..1

        private NumericUpDown _numBaseFare = null!;
        private NumericUpDown _numPricePerKm = null!;
        private NumericUpDown _numMinimumFare = null!;
        private NumericUpDown _numCommission = null!;  // hiển thị 0..100, lưu /100

        private static readonly Color Blue = Color.FromArgb(0, 122, 255);
        private static readonly Color Green = Color.FromArgb(0, 150, 80);

        public EditFareRuleForm(Fare rule)
        {
            Text = $"Chỉnh sửa bảng giá – {rule.VehicleType}";
            Size = new Size(440, 440);
            MinimumSize = new Size(400, 400);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;
            Font = new Font("Segoe UI", 10F);

            BuildUI(rule);
        }

        private void BuildUI(Fare rule)
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 6,
                ColumnCount = 2,
                Padding = new Padding(24, 20, 24, 12)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 46));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 54));
            for (int i = 0; i < 6; i++)
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

            // Row 0 — header label
            var lblHeader = new Label
            {
                Text = $"Loại xe: {rule.VehicleType}",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Blue,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            layout.Controls.Add(lblHeader, 0, 0);
            layout.SetColumnSpan(lblHeader, 2);

            // Row 1..4 — input fields
            _numBaseFare = AddNumRow(layout, "Giá mở cửa (đ):", 1, rule.BaseFare, 0, 500_000);
            _numPricePerKm = AddNumRow(layout, "Giá / km (đ):", 2, rule.PricePerKm, 0, 100_000);
            _numCommission = AddNumRow(layout, "Hoa hồng (%):", 4, rule.CommissionRate * 100, 0, 100,
                                           decimalPlaces: 0);

            // Row 5 — buttons
            var btnPanel = new FlowLayoutPanel
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
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10)
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);

            var btnSave = new Button
            {
                Text = "Lưu",
                Width = 90,
                Height = 34,
                BackColor = Green,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += OnSaveClicked;

            btnPanel.Controls.Add(btnCancel);
            btnPanel.Controls.Add(btnSave);

            layout.Controls.Add(btnPanel, 0, 5);
            layout.SetColumnSpan(btnPanel, 2);

            Controls.Add(layout);
            AcceptButton = btnSave;
            CancelButton = btnCancel;
        }

        private void OnSaveClicked(object? sender, EventArgs e)
        {
            // Đọc giá trị và validate sơ bộ trước khi đóng
            decimal baseFare = _numBaseFare.Value;
            decimal perKm = _numPricePerKm.Value;
            decimal minFare = _numMinimumFare.Value;
            decimal commission = _numCommission.Value / 100m;  // % → 0..1

            if (minFare < baseFare)
            {
                MessageBox.Show("Giá tối thiểu không được thấp hơn giá mở cửa.",
                    "Không hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _numMinimumFare.Focus();
                return;
            }

            // Gán kết quả — AdminDashboardForm sẽ đọc
            NewBaseFare = baseFare;
            NewPricePerKm = perKm;
            NewMinimumFare = minFare;
            NewCommissionRate = commission;

            DialogResult = DialogResult.OK;
        }

        // Thêm một hàng label + NumericUpDown vào TableLayoutPanel, trả về NumericUpDown
        private static NumericUpDown AddNumRow(
            TableLayoutPanel layout,
            string labelText,
            int row,
            decimal value,
            decimal min,
            decimal max,
            int decimalPlaces = 0)
        {
            var lbl = new Label
            {
                Text = labelText,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Color.FromArgb(70, 70, 70)
            };

            var num = new NumericUpDown
            {
                Dock = DockStyle.Fill,
                Minimum = min,
                Maximum = max,
                Value = Math.Clamp(value, min, max),
                DecimalPlaces = decimalPlaces,
                ThousandsSeparator = decimalPlaces == 0,
                Font = new Font("Segoe UI", 10.5f)
            };

            layout.Controls.Add(lbl, 0, row);
            layout.Controls.Add(num, 1, row);
            return num;
        }
    }

    public class AddFareRuleForm : Form
    {
        public VehicleType NewVehicleType { get; private set; }
        public decimal NewBaseFare { get; private set; }
        public decimal NewPricePerKm { get; private set; }
        public decimal NewMinimumFare { get; private set; }
        public decimal NewCommissionRate { get; private set; }  // 0..1

        private ComboBox _cmbVehicleType = null!;
        private NumericUpDown _numBaseFare = null!;
        private NumericUpDown _numPricePerKm = null!;
        private NumericUpDown _numMinimumFare = null!;
        private NumericUpDown _numCommission = null!;

        private static readonly Color Blue = Color.FromArgb(0, 122, 255);
        private static readonly Color Green = Color.FromArgb(0, 150, 80);

        public AddFareRuleForm()
        {
            Text = "Tạo bảng giá mới";
            Size = new Size(440, 460);
            MinimumSize = new Size(400, 420);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;
            Font = new Font("Segoe UI", 10F);

            BuildUI();
        }

        private void BuildUI()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 7,
                ColumnCount = 2,
                Padding = new Padding(24, 20, 24, 12)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 46));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 54));
            for (int i = 0; i < 7; i++)
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

            var lblHeader = new Label
            {
                Text = "Tạo bảng giá mới",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Blue,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            layout.Controls.Add(lblHeader, 0, 0);
            layout.SetColumnSpan(lblHeader, 2);

            // Vehicle type
            var lblType = new Label
            {
                Text = "Loại xe:",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Color.FromArgb(70, 70, 70)
            };
            _cmbVehicleType = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10.5f)
            };
            _cmbVehicleType.Items.AddRange(Enum.GetValues(typeof(VehicleType))
                .Cast<object>().ToArray());
            _cmbVehicleType.SelectedIndex = 0;

            layout.Controls.Add(lblType, 0, 1);
            layout.Controls.Add(_cmbVehicleType, 1, 1);

            _numBaseFare = AddNumRow(layout, "Giá mở cửa (đ):", 2, 10000, 0, 500_000);
            _numPricePerKm = AddNumRow(layout, "Giá / km (đ):", 3, 5000, 0, 100_000);
            _numMinimumFare = AddNumRow(layout, "Giá tối thiểu (đ):", 4, 10000, 0, 500_000);
            _numCommission = AddNumRow(layout, "Hoa hồng (%):", 5, 20, 0, 100, decimalPlaces: 0);

            var btnPanel = new FlowLayoutPanel
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
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10)
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);

            var btnSave = new Button
            {
                Text = "Lưu",
                Width = 90,
                Height = 34,
                BackColor = Green,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += OnSaveClicked;

            btnPanel.Controls.Add(btnCancel);
            btnPanel.Controls.Add(btnSave);

            layout.Controls.Add(btnPanel, 0, 6);
            layout.SetColumnSpan(btnPanel, 2);

            Controls.Add(layout);
            AcceptButton = btnSave;
            CancelButton = btnCancel;
        }

        private void OnSaveClicked(object? sender, EventArgs e)
        {
            decimal baseFare = _numBaseFare.Value;
            decimal perKm = _numPricePerKm.Value;
            decimal minFare = _numMinimumFare.Value;
            decimal commission = _numCommission.Value / 100m;

            if (minFare < baseFare)
            {
                MessageBox.Show("Giá tối thiểu không được thấp hơn giá mở cửa.",
                    "Không hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _numMinimumFare.Focus();
                return;
            }

            NewVehicleType = (VehicleType)_cmbVehicleType.SelectedItem!;
            NewBaseFare = baseFare;
            NewPricePerKm = perKm;
            NewMinimumFare = minFare;
            NewCommissionRate = commission;

            DialogResult = DialogResult.OK;
        }

        private static NumericUpDown AddNumRow(
            TableLayoutPanel layout,
            string labelText,
            int row,
            decimal value,
            decimal min,
            decimal max,
            int decimalPlaces = 0)
        {
            var lbl = new Label
            {
                Text = labelText,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Color.FromArgb(70, 70, 70)
            };

            var num = new NumericUpDown
            {
                Dock = DockStyle.Fill,
                Minimum = min,
                Maximum = max,
                Value = Math.Clamp(value, min, max),
                DecimalPlaces = decimalPlaces,
                ThousandsSeparator = decimalPlaces == 0,
                Font = new Font("Segoe UI", 10.5f)
            };

            layout.Controls.Add(lbl, 0, row);
            layout.Controls.Add(num, 1, row);
            return num;
        }
    }
}
