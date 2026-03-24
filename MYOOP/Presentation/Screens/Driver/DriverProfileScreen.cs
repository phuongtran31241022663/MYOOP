
// ─────────────────────────────────────────────────────────────────────────────
// DriverProfileScreen.cs
// ─────────────────────────────────────────────────────────────────────────────

using OOP.Presentation.Base;
using OOP.Presentation.Common.Theme;

namespace OOP.Presentation.Screens.Driver
{
    /// <summary>
    /// Thông tin cá nhân + thông tin xe của tài xế.
    /// Thay thế ProfileForm mở bằng ShowDialog trong DriverDashboardForm.
    /// </summary>
    public class DriverProfileScreen : UserControl, IScreen
    {
        private readonly OOP.Domain.Entities.Driver _driver;
        private readonly IUserService _userService;

        private TextBox _txtName = null!;
        private TextBox _txtPhone = null!;
        private Label _lblSaved = null!;

        // Vehicle info (read-only)
        private Label _lblVehicleType = null!;
        private Label _lblPlate = null!;
        private Label _lblBrand = null!;
        private Label _lblWallet = null!;
        private Label _lblRating = null!;
        private Button _btnTopUp = null!;
        private Button _btnUpdateVehicle = null!;

        public string ScreenTitle => "Tài khoản";

        public Task OnNavigatedTo(object? parameter = null)
        {
            _txtName.Text = _driver.Name;
            _txtPhone.Text = _driver.Phone;
            RefreshVehicleInfo();
            _lblSaved.Visible = false;
            return Task.CompletedTask;
        }

        public bool OnNavigatingFrom() => true;

        public DriverProfileScreen(OOP.Domain.Entities.Driver driver, IUserService userService)
        {
            _driver = driver;
            _userService = userService;
            BuildUI();
        }

        private void BuildUI()
        {
            BackColor = AppTheme.PageBg;
            Padding = new Padding(20);

            // Profile card
            var cardProfile = FormHelper.MakeCard(380, 230);
            cardProfile.Location = new Point(20, 20);
            AddCardTitle(cardProfile, "Thông tin cá nhân", new Point(20, 14));

            int y = 44;
            AddFieldLabel(cardProfile, "Họ và tên", y); y += 20;
            _txtName = FormHelper.MakeInputSized("Nhập họ tên", 330);
            FormHelper.Place(_txtName, cardProfile, 20, y, 330, AppTheme.InputHeight); y += 46;

            AddFieldLabel(cardProfile, "Số điện thoại", y); y += 20;
            _txtPhone = FormHelper.MakeInputSized("Nhập số điện thoại", 330);
            FormHelper.Place(_txtPhone, cardProfile, 20, y, 330, AppTheme.InputHeight); y += 48;

            var btnSave = FormHelper.MakeButton("Lưu thay đổi", AppTheme.Success, AppTheme.SuccessHover);
            btnSave.Width = 150;
            FormHelper.Place(btnSave, cardProfile, 20, y, 150, AppTheme.ButtonHeight);
            btnSave.Click += async (_, _) => await OnSave();

            _lblSaved = new Label
            {
                Text = "✅ Đã lưu",
                ForeColor = AppTheme.Success,
                Font = AppTheme.SmallFont,
                AutoSize = true,
                Location = new Point(180, y + 12),
                Visible = false
            };
            cardProfile.Controls.Add(_lblSaved);
            Controls.Add(cardProfile);

            // Vehicle + stats card
            var cardVehicle = FormHelper.MakeCard(380, 220);
            cardVehicle.Location = new Point(420, 20);
            AddCardTitle(cardVehicle, "Thông tin xe & Ví", new Point(20, 14));

            int vy = 44;
            _lblVehicleType = MakeInfoRow(cardVehicle, "Loại xe", vy); vy += 36;
            _lblPlate = MakeInfoRow(cardVehicle, "Biển số", vy); vy += 36;
            _lblBrand = MakeInfoRow(cardVehicle, "Hãng / Màu", vy); vy += 36;
            _lblWallet = MakeInfoRow(cardVehicle, "Số dư ví", vy); vy += 36;
            
            // Nạp tiền button
            _btnTopUp = new Button
            {
                Text = "Nạp tiền",
                Font = new Font("Segoe UI", 8f),
                ForeColor = Color.White,
                BackColor = AppTheme.Primary,
                FlatStyle = FlatStyle.Flat,
                Width = 80,
                Height = 26,
                Location = new Point(280, vy - 30),
                Cursor = Cursors.Hand
            };
            _btnTopUp.FlatAppearance.BorderSize = 0;
            _btnTopUp.Click += OnTopUp;
            cardVehicle.Controls.Add(_btnTopUp);
            
            vy += 36;
            _lblRating = MakeInfoRow(cardVehicle, "Đánh giá TB", vy); vy += 36;
            
            // Cập nhật xe button
            _btnUpdateVehicle = new Button
            {
                Text = "📝 Cập nhật xe",
                Font = new Font("Segoe UI", 8f),
                ForeColor = Color.White,
                BackColor = AppTheme.Warning,
                FlatStyle = FlatStyle.Flat,
                Width = 120,
                Height = 28,
                Location = new Point(240, vy - 10),
                Cursor = Cursors.Hand
            };
            _btnUpdateVehicle.FlatAppearance.BorderSize = 0;
            _btnUpdateVehicle.Click += OnUpdateVehicle;
            cardVehicle.Controls.Add(_btnUpdateVehicle);
            
            Controls.Add(cardVehicle);
        }

        private void RefreshVehicleInfo()
        {
            var v = _driver.Vehicle;
            _lblVehicleType.Text = v?.GetVehicleType() ?? "N/A";
            _lblPlate.Text = v?.PlateNumber ?? "N/A";
            _lblBrand.Text = v != null ? $"{v.Brand} {v.Model} – {v.Color}" : "N/A";
            _lblWallet.Text = $"{_driver.Wallet:N0} VNĐ";
            _lblRating.Text = $"⭐ {_driver.AverageRating:F1}  ({_driver.TotalTrips} chuyến)";
        }

        private static Label MakeInfoRow(Panel parent, string label, int y)
        {
            var lbl = new Label
            {
                Text = label + ":",
                Font = new Font("Segoe UI", 9f),
                ForeColor = AppTheme.TextMuted,
                Location = new Point(20, y),
                Width = 100,
                Height = 22,
                AutoSize = false
            };
            var val = new Label
            {
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = AppTheme.TextPrimary,
                Location = new Point(130, y),
                Width = 230,
                Height = 22,
                AutoEllipsis = true
            };
            parent.Controls.Add(lbl);
            parent.Controls.Add(val);
            return val;
        }

        private static void AddCardTitle(Panel card, string text, Point loc)
        {
            card.Controls.Add(new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = AppTheme.TextPrimary,
                AutoSize = true,
                Location = loc
            });
        }

        private static void AddFieldLabel(Panel card, string text, int y)
        {
            card.Controls.Add(new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 9f),
                ForeColor = AppTheme.TextMuted,
                Location = new Point(20, y),
                AutoSize = true
            });
        }

        private async Task OnSave()
        {
            try
            {
                await _userService.UpdateUserProfile(_driver.Id, _txtName.Text.Trim(), _txtPhone.Text.Trim());
                _lblSaved.Visible = true;
                await Task.Delay(3000);
                _lblSaved.Visible = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void OnTopUp(object? sender, EventArgs e)
        {
            var input = Microsoft.VisualBasic.Interaction.InputBox(
                "Nhập số tiền cần nạp (VNĐ):",
                "Nạp tiền vào ví",
                "100000");

            if (string.IsNullOrWhiteSpace(input)) return;

            if (!decimal.TryParse(input, out var amount) || amount <= 0)
            {
                MessageBox.Show("Số tiền không hợp lệ.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                await _userService.TopUpDriverWallet(_driver.Id, amount);
                await RefreshDriverFromService();
                MessageBox.Show($"Đã nạp {amount:N0} VNĐ vào ví!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnUpdateVehicle(object? sender, EventArgs e)
        {
            var v = _driver.Vehicle;
            if (v == null)
            {
                MessageBox.Show("Bạn chưa có thông tin xe. Liên hệ quản lý để được hỗ trợ.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var form = new Form
            {
                Text = "Cập nhật thông tin xe",
                Width = 400,
                Height = 280,
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = AppTheme.PageBg
            };

            var lblType = new Label { Text = "Loại xe:", Location = new Point(20, 20), Width = 80 };
            var cmbType = new ComboBox
            {
                Location = new Point(110, 18),
                Width = 250,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbType.Items.AddRange(new[] { "Motorbike", "Car" });
            cmbType.SelectedItem = v.GetVehicleType();
            if (cmbType.SelectedItem == null) cmbType.SelectedIndex = 0;

            var lblPlate = new Label { Text = "Biển số:", Location = new Point(20, 60), Width = 80 };
            var txtPlate = new TextBox { Location = new Point(110, 58), Width = 250, Text = v.PlateNumber };

            var lblBrand = new Label { Text = "Hãng xe:", Location = new Point(20, 100), Width = 80 };
            var txtBrand = new TextBox { Location = new Point(110, 98), Width = 250, Text = v.Brand };

            var lblModel = new Label { Text = "Mẫu xe:", Location = new Point(20, 140), Width = 80 };
            var txtModel = new TextBox { Location = new Point(110, 138), Width = 250, Text = v.Model };

            var lblColor = new Label { Text = "Màu sắc:", Location = new Point(20, 180), Width = 80 };
            var txtColor = new TextBox { Location = new Point(110, 178), Width = 250, Text = v.Color };

            var lblCap = new Label { Text = "Số chỗ:", Location = new Point(20, 220), Width = 80 };
            var numCap = new NumericUpDown { Location = new Point(110, 218), Width = 100, Minimum = 1, Maximum = 50, Value = v.Capacity };

            var btnSave = new Button
            {
                Text = "Lưu",
                Location = new Point(200, 250),
                Width = 80,
                Height = 32,
                BackColor = AppTheme.Success,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnSave.FlatAppearance.BorderSize = 0;

            var btnCancel = new Button
            {
                Text = "Hủy",
                Location = new Point(290, 250),
                Width = 80,
                Height = 32,
                BackColor = AppTheme.Danger,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnCancel.FlatAppearance.BorderSize = 0;

            form.Height = 320;
            form.Controls.AddRange(new Control[] { lblType, cmbType, lblPlate, txtPlate, lblBrand, txtBrand, lblModel, txtModel, lblColor, txtColor, lblCap, numCap, btnSave, btnCancel });

            btnSave.Click += async (_, _) =>
            {
                try
                {
                    string vehicleType = cmbType.SelectedItem?.ToString() ?? "Motorbike";
                    await _userService.UpdateDriverVehicleInfo(
                        _driver.Id,
                        vehicleType,
                        txtPlate.Text.Trim(),
                        txtBrand.Text.Trim(),
                        txtModel.Text.Trim(),
                        txtColor.Text.Trim(),
                        (int)numCap.Value);
                    await RefreshDriverFromService();
                    form.DialogResult = DialogResult.OK;
                    form.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            btnCancel.Click += (_, _) =>
            {
                form.DialogResult = DialogResult.Cancel;
                form.Close();
            };

            form.ShowDialog();
        }

        private async Task RefreshDriverFromService()
        {
            var refreshed = await _userService.GetUserProfile(_driver.Id) as OOP.Domain.Entities.Driver;
            if (refreshed != null)
                _driver.SyncFrom(refreshed);

            RefreshVehicleInfo();
        }
    }
}
