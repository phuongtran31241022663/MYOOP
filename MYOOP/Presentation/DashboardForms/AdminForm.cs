using OOP.Application.Interfaces;
using OOP.Application.Services.Interfaces;
using OOP.Application.Validators;
using OOP.Domain.Entities;
using OOP.Domain.Enums;

namespace OOP.Presentation
{
    public class AdminDashboardForm : Form
    {
        // --- Dependencies ---
        private readonly Admin _admin;
        private readonly IAdminService _adminService;

        // --- Tab controls ---
        private DataGridView _dgvUsers = null!;
        private DataGridView _dgvTrips = null!;
        private DataGridView _dgvFareRules = null!;

        // --- Constants ---
        private static readonly Color Blue = Color.FromArgb(0, 122, 255);
        private static readonly Color BlueHover = Color.FromArgb(0, 100, 220);
        private static readonly Color Red = Color.FromArgb(200, 50, 50);
        private static readonly Color RedHover = Color.FromArgb(170, 30, 30);
        private static readonly Color Green = Color.FromArgb(0, 150, 80);
        private static readonly Color Orange = Color.FromArgb(200, 100, 0);
        private static readonly Color BgPage = Color.FromArgb(245, 247, 250);

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
                BackColor = Color.FromArgb(200, 50, 50),
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
                MakeCol("IsActive", "Hoạt động", 90),
                MakeCol("TotalTrips", "Tổng chuyến", 100)
            );

            page.Controls.Add(_dgvUsers);
            page.Controls.Add(toolbar);
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

                    _dgvUsers.Rows.Add(
                        u.Id,
                        u.Name,
                        u.Phone,
                        u.Role.ToString(),
                        u.IsActive ? "✅ Hoạt động" : "🔒 Đã khóa",
                        trips);
                }
            }
            catch (Exception ex) { ShowError(ex.Message); }
        }

        private async Task ToggleUserActive()
        {
            if (_dgvUsers.CurrentRow == null) return;

            // 1. Lấy ID của User đang được chọn trên Grid
            var targetUserId = (Guid)_dgvUsers.CurrentRow.Cells["UserId"].Value;
            var targetUserName = _dgvUsers.CurrentRow.Cells["Name"].Value.ToString();

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
            bool currentlyActive = _dgvUsers.CurrentRow.Cells["IsActive"].Value.ToString()!.Contains("Hoạt động");
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
                    // Chặn ngay lập tức nếu Admin tự chọn chính mình để khóa
                    if (targetUserId == _admin.Id)
                    {
                        ShowError("Hệ thống ngăn chặn hành động tự khóa tài khoản của chính mình.");
                        return;
                    }
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
                MakeCol("Distance", "Khoảng cách", 110),
                MakeCol("Fare", "Cước phí", 110),
                MakeCol("Status", "Trạng thái", 110),
                MakeCol("RequestedAt", "Thời gian", 140)
            );

            page.Controls.Add(_dgvTrips);
            page.Controls.Add(toolbar);
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
                        t.DriverId,
                        passengerName,
                        driverName,
                        t.VehicleType.ToString(),
                        t.Distance > 0 ? $"{t.Distance:F1} km" : "–",
                        t.Fare > 0 ? $"{t.Fare:N0} đ" : "–",
                        StatusLabel(t.Status),
                        t.RequestedAt.ToString("dd/MM/yyyy HH:mm"));
                }
            }
            catch (Exception ex) { ShowError(ex.Message); }
        }

        // ─── Tab: Bảng giá ───────────────────────────────────────────────────────

        private TabPage BuildFareRulesTab()
        {
            var page = new TabPage("💰  Bảng giá");

            var toolbar = MakeToolbar();

            var btnRefresh = MakeToolbarButton("🔄  Làm mới", Blue);
            btnRefresh.Click += async (s, e) => await LoadFareRules();

            var btnEdit = MakeToolbarButton("✏️  Chỉnh sửa", Green);
            btnEdit.Click += async (s, e) => await OnEditFareRuleClicked();

            toolbar.Controls.Add(btnRefresh);
            toolbar.Controls.Add(btnEdit);

            _dgvFareRules = MakeGrid();
            _dgvFareRules.Columns.AddRange(
                MakeCol("FareRuleId", "ID", 60, hidden: true),
                MakeCol("VehicleType", "Loại xe", 100),
                MakeCol("BaseFare", "Giá mở cửa", 120),
                MakeCol("PricePerKm", "Giá / km", 120),
                MakeCol("PricePerMinute", "Giá / phút", 120),
                MakeCol("MinimumFare", "Giá tối thiểu", 130),
                MakeCol("CommissionRate", "Hoa hồng (%)", 110),
                MakeCol("UpdatedAt", "Cập nhật lúc", 140)
            );

            page.Controls.Add(_dgvFareRules);
            page.Controls.Add(toolbar);
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
                        $"{r.MinimumFare:N0} đ",
                        $"{r.CommissionRate * 100:F0}%",
                        r.UpdatedAt.ToString("dd/MM/yyyy HH:mm"));
                }
            }
            catch (Exception ex) { ShowError(ex.Message); }
        }

        // ─── Events ──────────────────────────────────────────────────────────────

        private async Task LoadAllData()
        {
            try
            {
                await Task.WhenAll(LoadUsers(), LoadTrips(), LoadFareRules());
            }
            catch (Exception ex) { ShowError(ex.Message); }
        }

        private async Task OnEditFareRuleClicked()
        {
            if (_dgvFareRules.CurrentRow == null)
            {
                MessageBox.Show("Vui lòng chọn một dòng bảng giá để chỉnh sửa.",
                    "Chưa chọn", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var ruleId = (Guid)_dgvFareRules.CurrentRow.Cells["FareRuleId"].Value;

            // Lấy rule hiện tại để điền sẵn giá trị vào form
            var rules = await _adminService.GetFareRules();
            var selectedRule = rules.FirstOrDefault(r => r.Id == ruleId);
            if (selectedRule == null) return;

            // Mở dialog chỉnh sửa
            using var editForm = new EditFareRuleForm(selectedRule);
            if (editForm.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                // Update rule với giá trị mới từ dialog
                selectedRule.Update(
                    editForm.NewBaseFare,
                    editForm.NewPricePerKm,
                    editForm.NewMinimumFare,
                    editForm.NewCommissionRate);

                FareRuleValidator.ValidateRule(selectedRule);
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
                BackColor = Color.White,
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
                BackgroundColor = Color.White,
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
            TripStatus.Matched => "🤝 Đã ghép",
            TripStatus.Arrived => "📍 Tài xế đã đến",
            TripStatus.Ongoing => "🚗 Đang chạy",
            TripStatus.Completed => "✅ Hoàn thành",
            TripStatus.Cancelled => "❌ Đã hủy",
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
        public decimal NewPricePerMinute { get; private set; }
        public decimal NewMinimumFare { get; private set; }
        public decimal NewCommissionRate { get; private set; }  // 0..1

        private NumericUpDown _numBaseFare = null!;
        private NumericUpDown _numPricePerKm = null!;
        private NumericUpDown _numPricePerMinute = null!;
        private NumericUpDown _numMinimumFare = null!;
        private NumericUpDown _numCommission = null!;  // hiển thị 0..100, lưu /100

        private static readonly Color Blue = Color.FromArgb(0, 122, 255);
        private static readonly Color Green = Color.FromArgb(0, 150, 80);

        public EditFareRuleForm(FareRule rule)
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

        private void BuildUI(FareRule rule)
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

            // Row 1..5 — input fields
            _numBaseFare = AddNumRow(layout, "Giá mở cửa (đ):", 1, rule.BaseFare, 0, 500_000);
            _numPricePerKm = AddNumRow(layout, "Giá / km (đ):", 2, rule.PricePerKm, 0, 100_000);
            _numMinimumFare = AddNumRow(layout, "Giá tối thiểu (đ):", 4, rule.MinimumFare, 0, 500_000);
            _numCommission = AddNumRow(layout, "Hoa hồng (%):", 5, rule.CommissionRate * 100, 0, 100,
                                           decimalPlaces: 0);

            // Row 6 — buttons
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
            // Đọc giá trị và validate sơ bộ trước khi đóng
            decimal baseFare = _numBaseFare.Value;
            decimal perKm = _numPricePerKm.Value;
            decimal perMin = _numPricePerMinute.Value;
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
            NewPricePerMinute = perMin;
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
}