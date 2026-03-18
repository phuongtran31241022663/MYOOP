using OOP.Application.Services.Interfaces;
using OOP.Application.Validators;
using OOP.Domain.Entities;
using OOP.Domain.Enums;

namespace OOP.Presentation
{
    public class RegisterForm : Form
    {
        // --- Dependencies ---
        private readonly IAuthService _authService;

        /// <summary>
        /// Phát ra khi đăng ký thành công để caller (ví dụ MainForm) có thể auto-login.
        /// </summary>
        public event Action<string, string, UserRole>? RegisteredSuccessfully;

        // --- Controls: common ---
        private RadioButton _rdoPassenger = null!;
        private RadioButton _rdoDriver = null!;
        private TextBox _txtName = null!;
        private TextBox _txtPhone = null!;
        private TextBox _txtPassword = null!;
        private Button _btnTogglePass = null!;
        private Button _btnRegister = null!;
        private Button _btnBack = null!;
        private Label _lblError = null!;
        private ErrorProvider _errorProvider = null!;

        // --- Controls: vehicle (driver only) ---
        private ComboBox _cmbVehicleType = null!;
        private TextBox _txtPlate = null!;
        private TextBox _txtLicense = null!;
        private TextBox _txtBrand = null!;
        private TextBox _txtModel = null!;
        private TextBox _txtColor = null!;
        private NumericUpDown _numCapacity = null!;

        // --- Layout ---
        private Panel _cardCommon = null!;
        private Panel _cardVehicle = null!;

        // --- Constants ---
        private const int CardWidth = 360;
        private const int CardPad = 24;
        private const int InputHeight = 38;
        private const int LabelHeight = 20;
        private const int Gap = 10;

        public RegisterForm(IAuthService authService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            InitForm();
            BuildUI();

            _rdoPassenger.Checked = true;
            OnRoleChanged();
        }

        // ── Setup ───────────────────────────────────────────────────────────

        private void InitForm()
        {
            Text = "RideGo – Đăng ký tài khoản";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(960, 700);
            MinimumSize = new Size(760, 600);
            BackColor = AppTheme.PageBg;
            Font = new Font("Segoe UI", 10F);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;

            _errorProvider = new ErrorProvider
            {
                ContainerControl = this,
                BlinkStyle = ErrorBlinkStyle.NeverBlink
            };
        }

        private void BuildUI()
        {
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 110,
                BackColor = AppTheme.Primary,
                Padding = new Padding(28, 18, 28, 12)
            };

            var lblTitle = new Label
            {
                Text = "Tạo tài khoản",
                Dock = DockStyle.Top,
                Height = 38,
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var lblSub = new Label
            {
                Text = "Hoàn tất thông tin để bắt đầu sử dụng RideGo",
                Dock = DockStyle.Top,
                Height = 22,
                Font = new Font("Segoe UI", 10f),
                ForeColor = Color.FromArgb(220, 235, 255),
                TextAlign = ContentAlignment.MiddleLeft
            };

            header.Controls.Add(lblSub);
            header.Controls.Add(lblTitle);
            Controls.Add(header);

            _cardCommon = FormHelper.MakeCard(CardWidth, 520);
            BuildCommonCard(_cardCommon);

            _cardVehicle = FormHelper.MakeCard(CardWidth, 520);
            BuildVehicleCard(_cardVehicle);

            Controls.Add(_cardCommon);
            Controls.Add(_cardVehicle);

            Resize += (s, e) => LayoutCards();
            Shown += (s, e) =>
            {
                LayoutCards();
                _txtName.Focus();
            };
        }

        // ── Card thông tin chung ───────────────────────────────────────────

        private void BuildCommonCard(Panel card)
        {
            int y = CardPad;

            var lblRole = MakeLabel("Bạn là:");
            Place(lblRole, card, CardPad, y, CardWidth - CardPad * 2, LabelHeight); y += LabelHeight + 4;

            var radioPanel = new Panel
            {
                Left = CardPad,
                Top = y,
                Width = CardWidth - CardPad * 2,
                Height = 30,
                BackColor = Color.Transparent
            };

            _rdoPassenger = new RadioButton
            {
                Text = "Hành khách",
                Checked = true,
                Left = 0,
                Top = 4,
                Width = 140,
                Font = new Font("Segoe UI", 10)
            };
            _rdoDriver = new RadioButton
            {
                Text = "Tài xế",
                Left = 150,
                Top = 4,
                Width = 100,
                Font = new Font("Segoe UI", 10)
            };
            _rdoPassenger.CheckedChanged += (s, e) => OnRoleChanged();
            _rdoDriver.CheckedChanged += (s, e) => OnRoleChanged();

            radioPanel.Controls.Add(_rdoPassenger);
            radioPanel.Controls.Add(_rdoDriver);
            card.Controls.Add(radioPanel);
            y += 38;

            var sep = MakeSep(card, y); y += 16;

            AddField(card, "Họ và tên", ref y,
                _txtName = MakeInput("Nhập họ và tên đầy đủ"));
            AttachEnterToNext(_txtName);

            AddField(card, "Số điện thoại", ref y,
                _txtPhone = MakeInput("VD: 0901234567"));
            _txtPhone.KeyPress += (s, e) =>
            {
                if (!char.IsDigit(e.KeyChar) && e.KeyChar != (char)Keys.Back)
                    e.Handled = true;
            };

            AddLabel(card, "Mật khẩu", ref y);
            var passRow = new Panel
            {
                Left = CardPad,
                Top = y,
                Width = CardWidth - CardPad * 2,
                Height = InputHeight,
                BackColor = Color.Transparent
            };

            _txtPassword = MakeInput("Ít nhất 6 ký tự");
            _txtPassword.UseSystemPasswordChar = true;
            _txtPassword.Width = passRow.Width - 44;
            _txtPassword.Left = 0;
            _txtPassword.Top = 0;
            _txtPassword.Height = InputHeight;

            _btnTogglePass = new Button
            {
                Text = "👁",
                Left = passRow.Width - 40,
                Top = 0,
                Width = 40,
                Height = InputHeight,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                Cursor = Cursors.Hand,
                TabStop = false,
                Font = new Font("Segoe UI", 11)
            };
            _btnTogglePass.FlatAppearance.BorderSize = 1;
            _btnTogglePass.FlatAppearance.BorderColor = AppTheme.BorderLight;
            _btnTogglePass.Click += (s, e) =>
            {
                _txtPassword.UseSystemPasswordChar = !_txtPassword.UseSystemPasswordChar;
                _btnTogglePass.Text = _txtPassword.UseSystemPasswordChar ? "👁" : "🔒";
                _txtPassword.Focus();
            };

            passRow.Controls.Add(_txtPassword);
            passRow.Controls.Add(_btnTogglePass);
            card.Controls.Add(passRow);
            y += InputHeight + Gap;
            AttachEnterToNext(_txtPhone);
            AttachEnterToNext(_txtPassword);

            _lblError = new Label
            {
                Text = "",
                ForeColor = AppTheme.Danger,
                Font = new Font("Segoe UI", 9),
                BackColor = Color.Transparent,
                AutoSize = false,
                Left = CardPad,
                Top = y,
                Width = CardWidth - CardPad * 2,
                Height = 18
            };
            card.Controls.Add(_lblError);
            y += 22;

            _btnRegister = FormHelper.MakeButton("Đăng ký", AppTheme.Success, AppTheme.SuccessHover, height: 44);
            _btnRegister.Left = CardPad;
            _btnRegister.Top = y;
            _btnRegister.Width = CardWidth - CardPad * 2;
            _btnRegister.Click += OnRegisterClicked;
            card.Controls.Add(_btnRegister);
            y += 52;

            _btnBack = FormHelper.MakeOutlineButton("← Quay lại");
            _btnBack.Left = CardPad;
            _btnBack.Top = y;
            _btnBack.Width = CardWidth - CardPad * 2;
            _btnBack.Click += OnBackClicked;
            card.Controls.Add(_btnBack);
        }

        // ── Card thông tin xe ───────────────────────────────────────────────

        private void BuildVehicleCard(Panel card)
        {
            int y = CardPad;

            var lblHeader = MakeLabel("Thông tin phương tiện", 11, FontStyle.Bold);
            Place(lblHeader, card, CardPad, y, CardWidth - CardPad * 2, 22);
            y += 30;

            AddLabel(card, "Loại xe", ref y);
            _cmbVehicleType = new ComboBox
            {
                Left = CardPad,
                Top = y,
                Width = CardWidth - CardPad * 2,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10.5f)
            };
            _cmbVehicleType.Items.AddRange(new object[]
            {
                new VehicleItem("Xe máy (Motorbike)", VehicleType.Motorbike),
                new VehicleItem("Ô tô (Car)", VehicleType.Car)
            });
            _cmbVehicleType.SelectedIndex = 0;
            _cmbVehicleType.SelectedIndexChanged += (s, e) => UpdateVehicleTypeUI();
            card.Controls.Add(_cmbVehicleType);
            y += InputHeight + Gap;

            AddField(card, "Biển số xe", ref y, _txtPlate = MakeInput("VD: 59X1-12345"));
            _txtPlate.CharacterCasing = CharacterCasing.Upper;
            AddField(card, "Số bằng lái xe", ref y, _txtLicense = MakeInput("VD: A1-12345"));
            _txtLicense.CharacterCasing = CharacterCasing.Upper;

            AddField(card, "Hãng xe", ref y, _txtBrand = MakeInput("VD: Honda, Toyota"));
            AddField(card, "Dòng xe (Model)", ref y, _txtModel = MakeInput("VD: Vision, Vios"));
            AddField(card, "Màu xe", ref y, _txtColor = MakeInput("VD: Đỏ, Trắng"));

            AddLabel(card, "Số chỗ ngồi", ref y);
            _numCapacity = new NumericUpDown
            {
                Left = CardPad,
                Top = y,
                Width = CardWidth - CardPad * 2,
                Font = new Font("Segoe UI", 10.5f)
            };
            card.Controls.Add(_numCapacity);

            UpdateVehicleTypeUI();
        }

        // ── Events ─────────────────────────────────────────────────────────

        private void OnRoleChanged()
        {
            bool isDriver = _rdoDriver.Checked;
            _cardVehicle.Visible = isDriver;
            LayoutCards();
        }

        private async void OnRegisterClicked(object? sender, EventArgs e)
        {
            _lblError.Text = "";
            _errorProvider.Clear();

            if (!Validate_Fields()) return;

            SetLoading(true);
            try
            {
                string name = _txtName.Text.Trim();
                string phone = _txtPhone.Text.Trim();
                string password = _txtPassword.Text;

                if (_rdoPassenger.Checked)
                {
                    await _authService.RegisterPassenger(name, phone, password);
                    ShowSuccess(
                        "Đăng ký hành khách thành công! Đang đăng nhập...",
                        phone,
                        password,
                        UserRole.Passenger);
                }
                else
                {
                    var vehicleItem = (VehicleItem)_cmbVehicleType.SelectedItem!;
                    var plate = _txtPlate.Text.Trim();
                    var brand = _txtBrand.Text.Trim();
                    var model = _txtModel.Text.Trim();
                    var color = _txtColor.Text.Trim();
                    var capacity = (byte)_numCapacity.Value;
                    string license = _txtLicense.Text.Trim();

                    Vehicle vehicle = vehicleItem.Type switch
                    {
                        VehicleType.Motorbike => new Motorbike(Guid.NewGuid(), plate, brand, model, color),
                        VehicleType.Car => new Car(Guid.NewGuid(), plate, brand, model, color, capacity),
                        _ => throw new InvalidOperationException("Loại xe không hỗ trợ")
                    };

                    var defaultLocation = new OOP.Domain.Entities.Location(
                        name: "Vị trí hiện tại",
                        address: "TP. Hồ Chí Minh",
                        lat: 10.7769,
                        lng: 106.7009);

                    await _authService.RegisterDriver(
                        name,
                        phone,
                        password,
                        vehicle,
                        defaultLocation,
                        license);
                    ShowSuccess(
                        "Đăng ký tài xế thành công! Đang đăng nhập...",
                        phone,
                        password,
                        UserRole.Driver);
                }
            }
            catch (Exception ex)
            {
                _lblError.Text = ex.Message;
                if (!(ex is ArgumentException || ex is InvalidOperationException))
                {
                    MessageBox.Show($"Lỗi hệ thống: {ex.Message}", "Lỗi",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            finally
            {
                SetLoading(false);
            }
        }

        private void OnBackClicked(object? sender, EventArgs e)
        {
            Close();
        }

        // ── Validation ─────────────────────────────────────────────────────

        private bool Validate_Fields()
        {
            bool ok = true;

            if (_txtName.Text.Trim().Length < 2)
            {
                _errorProvider.SetError(_txtName, "Tên phải có ít nhất 2 ký tự");
                ok = false;
            }

            try { UserValidator.ValidatePhone(_txtPhone.Text.Trim()); }
            catch (ArgumentException ex)
            {
                _errorProvider.SetError(_txtPhone, ex.Message);
                ok = false;
            }

            try { UserValidator.ValidatePassword(_txtPassword.Text); }
            catch (ArgumentException ex)
            {
                _errorProvider.SetError(_txtPassword, ex.Message);
                ok = false;
            }

            if (_rdoDriver.Checked)
            {
                if (string.IsNullOrWhiteSpace(_txtPlate.Text))
                { _errorProvider.SetError(_txtPlate, "Vui lòng nhập biển số xe"); ok = false; }

                if (string.IsNullOrWhiteSpace(_txtBrand.Text))
                { _errorProvider.SetError(_txtBrand, "Vui lòng nhập hãng xe"); ok = false; }

                if (string.IsNullOrWhiteSpace(_txtModel.Text))
                { _errorProvider.SetError(_txtModel, "Vui lòng nhập dòng xe"); ok = false; }

                if (string.IsNullOrWhiteSpace(_txtColor.Text))
                { _errorProvider.SetError(_txtColor, "Vui lòng nhập màu xe"); ok = false; }

                if (string.IsNullOrWhiteSpace(_txtLicense.Text))
                {
                    _errorProvider.SetError(_txtLicense, "Vui lòng nhập số bằng lái xe");
                    ok = false;
                }
            }

            return ok;
        }

        // ── Helpers ────────────────────────────────────────────────────────

        private void LayoutCards()
        {
            const int gap = 24;
            const int top = 140;

            if (_cardVehicle.Visible)
            {
                int total = CardWidth * 2 + gap;
                int left = (ClientSize.Width - total) / 2;
                _cardCommon.Location = new Point(left, top);
                _cardVehicle.Location = new Point(left + CardWidth + gap, top);
            }
            else
            {
                _cardCommon.Location = new Point(
                    (ClientSize.Width - CardWidth) / 2, top);
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Enter && _btnRegister.Enabled)
            {
                _btnRegister.PerformClick();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void UpdateVehicleTypeUI()
        {
            var item = (VehicleItem)_cmbVehicleType.SelectedItem!;

            if (item.Type == VehicleType.Motorbike)
            {
                _numCapacity.Minimum = 2;
                _numCapacity.Maximum = 2;
                _numCapacity.Value = 2;
                _numCapacity.Enabled = false;
            }
            else
            {
                _numCapacity.Enabled = true;
                _numCapacity.Minimum = 4;
                _numCapacity.Maximum = 7;

                if (_numCapacity.Value < 4)
                    _numCapacity.Value = 4;
            }
        }

        private void ShowSuccess(string message, string phone, string password, UserRole role)
        {
            MessageBox.Show(message, "Thành công",
                MessageBoxButtons.OK, MessageBoxIcon.Information);

            RegisteredSuccessfully?.Invoke(phone, password, role);

            ResetForm();
            Close();
        }

        private void ResetForm()
        {
            _txtName.Clear();
            _txtPhone.Clear();
            _txtPassword.Clear();
            _txtPlate.Clear();
            _txtBrand.Clear();
            _txtModel.Clear();
            _txtColor.Clear();
            _txtLicense.Clear();
            _rdoPassenger.Checked = true;
            _cmbVehicleType.SelectedIndex = 0;
            _numCapacity.Value = 2;
            _lblError.Text = "";
            _errorProvider.Clear();
        }

        private void AttachEnterToNext(Control control)
        {
            control.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    SelectNextControl((Control)s!, true, true, true, true);
                }
            };
        }

        private void SetLoading(bool loading)
        {
            _btnRegister.Enabled = !loading;
            _btnRegister.Text = loading ? "Đang đăng ký..." : "Đăng ký";
            _btnBack.Enabled = !loading;
            Cursor = loading ? Cursors.WaitCursor : Cursors.Default;
        }

        private static Label MakeLabel(string text, float size = 9.5f,
            FontStyle style = FontStyle.Bold)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI", size, style),
                ForeColor = AppTheme.TextMuted,
                BackColor = Color.Transparent,
                AutoSize = false
            };
        }

        private static TextBox MakeInput(string placeholder)
        {
            return new TextBox
            {
                PlaceholderText = placeholder,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10.5f)
            };
        }

        private void AddField(Panel card, string labelText, ref int y, TextBox input)
        {
            AddLabel(card, labelText, ref y);
            input.Left = CardPad;
            input.Top = y;
            input.Width = CardWidth - CardPad * 2;
            input.Height = InputHeight;
            card.Controls.Add(input);
            y += InputHeight + Gap;
        }

        private void AddLabel(Panel card, string text, ref int y)
        {
            var lbl = MakeLabel(text);
            Place(lbl, card, CardPad, y, CardWidth - CardPad * 2, LabelHeight);
            y += LabelHeight + 2;
        }

        private static void Place(Control c, Control parent, int x, int y, int w, int h)
        {
            c.Left = x; c.Top = y; c.Width = w; c.Height = h;
            parent.Controls.Add(c);
        }

        private static Control MakeSep(Panel card, int y)
        {
            var sep = new Label
            {
                Left = CardPad,
                Top = y,
                Width = CardWidth - CardPad * 2,
                Height = 1,
                BackColor = AppTheme.BorderLight
            };
            card.Controls.Add(sep);
            return sep;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _errorProvider?.Dispose();
            base.Dispose(disposing);
        }

        private sealed class VehicleItem
        {
            public string Label { get; }
            public VehicleType Type { get; }
            public VehicleItem(string label, VehicleType type) { Label = label; Type = type; }
            public override string ToString() => Label;
        }
    }
}

