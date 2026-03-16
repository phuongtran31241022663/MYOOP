using OOP.Application.Services.Interfaces;
using OOP.Domain.Entities;

namespace OOP.Presentation
{
    public class LoginForm : Form
    {
        private readonly IAuthService _authService;
        private readonly Func<Passenger, Form> _passengerFormFactory;
        private readonly Func<Driver, Form> _driverFormFactory;
        private readonly Func<Admin, Form> _adminFormFactory;

        private TextBox _txtPhone = null!;
        private TextBox _txtPassword = null!;
        private Button _btnLogin = null!;
        private Button _btnBack = null!;
        private Button _btnTogglePassword = null!;
        private Label _lblError = null!;
        private ErrorProvider _errorProvider = null!;

        private const int CardWidth = 380;
        private static readonly Color Blue = Color.FromArgb(0, 122, 255);
        private static readonly Color BlueHover = Color.FromArgb(0, 100, 220);
        private static readonly Color Gray = Color.FromArgb(200, 200, 200);

        public LoginForm(
            IAuthService authService,
            Func<Passenger, Form> passengerFormFactory,
            Func<Driver, Form> driverFormFactory,
            Func<Admin, Form> adminFormFactory)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _passengerFormFactory = passengerFormFactory ?? throw new ArgumentNullException(nameof(passengerFormFactory));
            _driverFormFactory = driverFormFactory ?? throw new ArgumentNullException(nameof(driverFormFactory));
            _adminFormFactory = adminFormFactory ?? throw new ArgumentNullException(nameof(adminFormFactory));

            InitForm();
            BuildUI();
        }

        private void InitForm()
        {
            Text = "RideGo - Đăng nhập";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1000, 700);
            MinimumSize = new Size(700, 550);
            BackColor = Color.White;
            Font = new Font("Segoe UI", 10);
            _errorProvider = new ErrorProvider
            {
                ContainerControl = this,
                BlinkStyle = ErrorBlinkStyle.NeverBlink
            };
        }

        private void BuildUI()
        {
            var card = new Panel { BackColor = Color.White, Width = CardWidth, Height = 460 };
            CenterCard(card);
            Resize += (s, e) => CenterCard(card);

            int y = 0;
            var lblLogo = new Label
            {
                Text = "🚗",
                Font = new Font("Segoe UI", 38),
                TextAlign = ContentAlignment.MiddleCenter,
                Left = 0,
                Top = y,
                Width = CardWidth,
                Height = 60
            };
            y += 65;

            var title = new Label
            {
                Text = "Đăng nhập",
                Font = new Font("Segoe UI", 22, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Left = 0,
                Top = y,
                Width = CardWidth,
                Height = 44
            };
            y += 54;

            var lblPhone = new Label
            {
                Text = "Số điện thoại",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Left = 0,
                Top = y,
                Width = CardWidth,
                Height = 22
            };
            y += 26;

            _txtPhone = new TextBox
            {
                PlaceholderText = "Nhập số điện thoại",
                Left = 0,
                Top = y,
                Width = CardWidth,
                Height = 28,
                BorderStyle = BorderStyle.FixedSingle
            };
            _txtPhone.TextChanged += OnInputChanged;
            y += 38;

            var lblPass = new Label
            {
                Text = "Mật khẩu",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Left = 0,
                Top = y,
                Width = CardWidth,
                Height = 22
            };
            y += 26;

            var passRow = new Panel { Left = 0, Top = y, Width = CardWidth, Height = 28 };

            _txtPassword = new TextBox
            {
                PlaceholderText = "Nhập mật khẩu",
                UseSystemPasswordChar = true,
                Left = 0,
                Top = 0,
                Width = CardWidth - 32,
                Height = 28,
                BorderStyle = BorderStyle.FixedSingle
            };
            _txtPassword.TextChanged += OnInputChanged;

            _btnTogglePassword = new Button
            {
                Text = "👁",
                Left = CardWidth - 30,
                Top = -1,
                Width = 30,
                Height = 27,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                Cursor = Cursors.Hand,
                TabStop = false
            };
            _btnTogglePassword.FlatAppearance.BorderSize = 1;
            _btnTogglePassword.FlatAppearance.BorderColor = Color.LightGray;
            _btnTogglePassword.Click += (s, e) =>
                FormHelper.TogglePasswordVisibility(_txtPassword, _btnTogglePassword);

            passRow.Controls.Add(_txtPassword);
            passRow.Controls.Add(_btnTogglePassword);
            y += 38;

            _lblError = new Label
            {
                Text = "",
                ForeColor = Color.Red,
                Font = new Font("Segoe UI", 9),
                Left = 0,
                Top = y,
                Width = CardWidth,
                Height = 22,
                TextAlign = ContentAlignment.MiddleLeft
            };
            y += 32;

            _btnLogin = new Button
            {
                Text = "Đăng nhập",
                Left = 0,
                Top = y,
                Width = CardWidth,
                Height = 44,
                BackColor = Gray,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = false,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 11, FontStyle.Bold)
            };
            _btnLogin.FlatAppearance.BorderSize = 0;
            FormHelper.AttachHover(_btnLogin, Blue, BlueHover);
            _btnLogin.Click += OnLoginClicked;
            y += 54;

            _btnBack = new Button
            {
                Text = "Quay lại",
                Left = 0,
                Top = y,
                Width = CardWidth,
                Height = 36,
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnBack.FlatAppearance.BorderColor = Color.LightGray;
            _btnBack.FlatAppearance.BorderSize = 1;
            _btnBack.Click += (s, e) => Close();

            card.Controls.AddRange(new Control[]
            {
                lblLogo, title, lblPhone, _txtPhone, lblPass,
                passRow, _lblError, _btnLogin, _btnBack
            });
            Controls.Add(card);
            Shown += (s, e) => _txtPhone.Focus();
            AttachKeyboardShortcuts();
        }

        /// <summary>
        /// Thiết lập hành vi phím Enter:
        /// - Enter tại ô SĐT → chuyển sang ô mật khẩu.
        /// - Enter tại ô mật khẩu (khi hợp lệ) → submit form đăng nhập.
        /// </summary>
        private void AttachKeyboardShortcuts()
        {
            _txtPhone.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    _txtPassword.Focus();
                }
            };

            _txtPassword.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    if (_btnLogin.Enabled)
                    {
                        OnLoginClicked(_btnLogin, EventArgs.Empty);
                    }
                }
            };
        }

        private void OnInputChanged(object? sender, EventArgs e)
        {
            _lblError.Text = "";
            _errorProvider.Clear();
            bool ok = _txtPhone.Text.Trim().Length >= 9 && _txtPassword.Text.Length >= 6;
            _btnLogin.Enabled = ok;
            _btnLogin.BackColor = ok ? Blue : Gray;
            _btnLogin.Cursor = ok ? Cursors.Hand : Cursors.Default;
        }

        private async void OnLoginClicked(object? sender, EventArgs e)
        {
            SetLoading(true);
            _lblError.Text = "";
            _errorProvider.Clear();

            try
            {
                string phone = _txtPhone.Text.Trim();
                string password = _txtPassword.Text;

                if (phone.Length < 9)
                {
                    _errorProvider.SetError(_txtPhone, "Số điện thoại không hợp lệ");
                    _txtPhone.Focus();
                    return;
                }
                if (password.Length < 6)
                {
                    _errorProvider.SetError(_txtPassword, "Mật khẩu ít nhất 6 ký tự");
                    _txtPassword.Focus();
                    return;
                }

                var user = await _authService.Login(phone, password);

                Form? nextForm = user switch
                {
                    Passenger p => _passengerFormFactory(p),
                    Driver d => _driverFormFactory(d),
                    Admin a => _adminFormFactory(a),
                    _ => null
                };

                if (nextForm == null)
                {
                    _lblError.Text = "Vai trò không được hỗ trợ.";
                    return;
                }

                Hide();
                nextForm.ShowDialog();
                nextForm.Dispose();
                _txtPassword.Clear();
                Show();
                _txtPhone.Focus();
            }
            catch (UnauthorizedAccessException)
            {
                _lblError.Text = "Số điện thoại hoặc mật khẩu không đúng.";
                _txtPassword.Clear();
                _txtPassword.Focus();
            }
            catch (InvalidOperationException ex)
            {
                _lblError.Text = ex.Message;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                MessageBox.Show($"Lỗi hệ thống: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetLoading(false);
            }
        }

        private void SetLoading(bool loading)
        {
            _btnLogin.Enabled = !loading;
            _btnLogin.Text = loading ? "Đang đăng nhập..." : "Đăng nhập";
            _btnBack.Enabled = !loading;
            Cursor = loading ? Cursors.WaitCursor : Cursors.Default;
        }

        private void CenterCard(Control card) =>
            card.Location = new Point(
                (ClientSize.Width - card.Width) / 2,
                (ClientSize.Height - card.Height) / 2);

        protected override void Dispose(bool disposing)
        {
            if (disposing) _errorProvider?.Dispose();
            base.Dispose(disposing);
        }
    }
}