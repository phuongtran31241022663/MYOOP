using OOP.Presentation.Common.Theme;
using OOP.Domain.Entities;
using OOP.Presentation.BaseForms;

namespace OOP.Presentation
{
    public class LoginForm : BaseDialogForm
    {
        private readonly Func<Passenger, Form> _passengerFormFactory;
        private readonly Func<Driver, Form> _driverFormFactory;
        private readonly Func<Admin, Form> _adminFormFactory;
        private readonly IUserService _userService;

        private TextBox _txtPhone = null!;
        private TextBox _txtPassword = null!;
        private Button _btnLogin = null!;
        private Button _btnBack = null!;
        private Button _btnTogglePassword = null!;
        private Label _lblError = null!;
        private ErrorProvider _errorProvider = null!;

        private const int CardWidth = 380;

        public LoginForm(
       IUserService userService,
       Func<Passenger, Form> passengerFormFactory,
       Func<Driver, Form> driverFormFactory,
       Func<Admin, Form> adminFormFactory)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _passengerFormFactory = passengerFormFactory ?? throw new ArgumentNullException(nameof(passengerFormFactory));
            _driverFormFactory = driverFormFactory ?? throw new ArgumentNullException(nameof(driverFormFactory));
            _adminFormFactory = adminFormFactory ?? throw new ArgumentNullException(nameof(adminFormFactory));

            InitForm();
            BuildUI();
        }

        private void InitForm()
        {
            Text = "Đăng nhập";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(980, 650);
            MinimumSize = new Size(760, 560);
            BackColor = AppTheme.CardBg;
            Font = new Font("Segoe UI", 10.5f);
            _errorProvider = new ErrorProvider
            {
                ContainerControl = this,
                BlinkStyle = ErrorBlinkStyle.NeverBlink
            };
        }

        private void BuildUI()
        {
            var card = new Panel
            {
                Width = CardWidth,
                Height = 460,
                BackColor = AppTheme.CardBg
            };
            CenterCard(card);
            Resize += (s, e) => CenterCard(card);

            int y = 20;
            var lblLogo = new Label
            {
                Text = "🚗",
                Font = new Font("Segoe UI", 34),
                TextAlign = ContentAlignment.MiddleCenter,
                Left = 0,
                Top = y,
                Width = CardWidth,
                Height = 52
            };
            y += 58;

            var title = new Label
            {
                Text = "Đăng nhập",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Left = 0,
                Top = y,
                Width = CardWidth,
                Height = 32
            };
            y += 40;

            var subtitle = new Label
            {
                Text = "Tiếp tục sử dụng hệ thống",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = AppTheme.TextMuted,
                TextAlign = ContentAlignment.MiddleCenter,
                Left = 0,
                Top = y,
                Width = CardWidth,
                Height = 20
            };
            y += 30;

            var lblPhone = new Label
            {
                Text = "Số điện thoại",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Left = 0,
                Top = y,
                Width = CardWidth,
                Height = 20
            };
            y += 24;

            _txtPhone = FormHelper.MakeInput("Nhập số điện thoại");
            _txtPhone.Left = 0;
            _txtPhone.Top = y;
            _txtPhone.Width = CardWidth;
            _txtPhone.Height = AppTheme.InputHeight;
            _txtPhone.TextChanged += OnInputChanged;
            y += 40;

            var lblPass = new Label
            {
                Text = "Mật khẩu",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Left = 0,
                Top = y,
                Width = CardWidth,
                Height = 20
            };
            y += 24;

            var passRow = new Panel { Left = 0, Top = y, Width = CardWidth, Height = AppTheme.InputHeight };

            _txtPassword = FormHelper.MakeInput("Nhập mật khẩu", isPassword: true);
            _txtPassword.Left = 0;
            _txtPassword.Top = 0;
            _txtPassword.Width = CardWidth - 38;
            _txtPassword.Height = AppTheme.InputHeight;
            _txtPassword.TextChanged += OnInputChanged;

            _btnTogglePassword = new Button
            {
                Text = "👁",
                Left = CardWidth - 36,
                Top = 1,
                Width = 36,
                Height = AppTheme.InputHeight - 2,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                Cursor = Cursors.Hand,
                TabStop = false
            };
            _btnTogglePassword.FlatAppearance.BorderSize = 1;
            _btnTogglePassword.FlatAppearance.BorderColor = AppTheme.BorderLight;
            _btnTogglePassword.Click += (s, e) =>
                FormHelper.TogglePasswordVisibility(_txtPassword, _btnTogglePassword);

            passRow.Controls.Add(_txtPassword);
            passRow.Controls.Add(_btnTogglePassword);
            y += 44;

            _lblError = new Label
            {
                Text = "",
                ForeColor = AppTheme.Danger,
                Font = new Font("Segoe UI", 9),
                Left = 0,
                Top = y,
                Width = CardWidth,
                Height = 20,
                TextAlign = ContentAlignment.MiddleLeft
            };
            y += 30;

            _btnLogin = FormHelper.MakeButton("Đăng nhập", AppTheme.Primary, AppTheme.PrimaryHover);
            _btnLogin.Left = 0;
            _btnLogin.Top = y;
            _btnLogin.Width = CardWidth;
            _btnLogin.Enabled = false;
            _btnLogin.BackColor = AppTheme.Disabled;
            _btnLogin.Click += OnLoginClicked;
            y += 56;

            _btnBack = FormHelper.MakeOutlineButton("Quay lại");
            _btnBack.Left = 0;
            _btnBack.Top = y;
            _btnBack.Width = CardWidth;
            _btnBack.Click += (s, e) => Close();

            card.Controls.AddRange(new Control[]
            {
                lblLogo, title, subtitle, lblPhone, _txtPhone, lblPass,
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
            _btnLogin.BackColor = ok ? AppTheme.Primary : AppTheme.Disabled;
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
                var user = await _userService.Login(phone, password);

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
            catch (ArgumentException ex)
            {
                string errorMsg = ex.Message;
                if (errorMsg.Contains("điện thoại"))
                {
                    _errorProvider.SetError(_txtPhone, errorMsg);
                    _txtPhone.Focus();
                }
                else if (errorMsg.Contains("mật khẩu"))
                {
                    _errorProvider.SetError(_txtPassword, errorMsg);
                    _txtPassword.Focus();
                }
                else
                {
                    _lblError.Text = errorMsg;
                }
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
                140 + (ClientSize.Height - 140 - card.Height) / 2);

        protected override void Dispose(bool disposing)
        {
            if (disposing) _errorProvider?.Dispose();
            base.Dispose(disposing);
        }
    }
}