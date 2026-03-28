using OOP.Domain.Entities;
using OOP.Presentation.BaseForms;
using OOP.Presentation.Common.Components;
using OOP.Presentation.Common.Theme;

namespace OOP.Presentation.Shells
{
    /// <summary>
    /// Entry point form — đăng nhập / đăng ký / quick-test.
    /// FIX: Layout dùng TableLayoutPanel để tránh bị overflow + đảm bảo
    ///      mọi nút đều hiển thị đúng khi resize.
    /// </summary>
    public class MainForm : BaseForm
    {
        private readonly Func<LoginForm> _loginFormFactory;
        private readonly Func<RegisterForm> _registerFormFactory;
        private readonly IUserService _userService;
        private readonly Func<Passenger, Form> _passengerDashboardFactory;
        private readonly Func<Driver, Form> _driverDashboardFactory;

        private Button ButtonLogin = null!;
        private Button ButtonRegister = null!;
        private Button ButtonDual = null!;
        private Button ButtonExit = null!;
        private StatusPanel _statusPanel = null!;

        private Form? _testPassengerForm;
        private Form? _testDriverForm;

        public MainForm(
            Func<LoginForm> loginFormFactory,
            Func<RegisterForm> registerFormFactory,
            IUserService userService,
            Func<Passenger, Form> passengerDashboardFactory,
            Func<Driver, Form> driverDashboardFactory)
        {
            _loginFormFactory = loginFormFactory ?? throw new ArgumentNullException(nameof(loginFormFactory));
            _registerFormFactory = registerFormFactory ?? throw new ArgumentNullException(nameof(registerFormFactory));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _passengerDashboardFactory = passengerDashboardFactory ?? throw new ArgumentNullException(nameof(passengerDashboardFactory));
            _driverDashboardFactory = driverDashboardFactory ?? throw new ArgumentNullException(nameof(driverDashboardFactory));

            InitForm();
            BuildUI();
        }

        private void InitForm()
        {
            Text = "OOP Ride-Hailing";
            // FIX: Chiều cao đủ chứa header (100) + card (300) + status (120) + chrome (~38)
            Size = new Size(540, 620);
            MinimumSize = new Size(420, 540);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 10.5f);
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = false;
            BackColor = AppTheme.PageBg;
        }

        private void BuildUI()
        {
            // ── Status panel (Bottom) ─── thêm TRƯỚC các control khác để dock đúng
            _statusPanel = new StatusPanel
            {
                Dock = DockStyle.Bottom,
                Height = 120          // thu nhỏ để không chiếm quá nhiều không gian
            };

            // ── Header (Top) ─────────────────────────────────────────────────
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 96,
                BackColor = AppTheme.Primary,
                Padding = new Padding(28, 16, 28, 10)
            };

            var lblTitle = new Label
            {
                Text = "OOP",
                Dock = DockStyle.Top,
                Height = 40,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 22, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent
            };
            var lblSub = new Label
            {
                Text = "Nền tảng đặt xe nội bộ — demo hệ thống",
                Dock = DockStyle.Top,
                Height = 20,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(220, 235, 255),
                BackColor = Color.Transparent
            };
            header.Controls.Add(lblSub);
            header.Controls.Add(lblTitle);

            // ── Content area (Fill) — TableLayoutPanel để responsive ──────────
            var contentHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.PageBg,
                Padding = new Padding(28, 20, 28, 16)
            };

            // Card chứa các nút — anchor Left+Right+Top+Bottom → tự giãn
            var card = new Panel
            {
                BackColor = Color.White,
                BorderStyle = BorderStyle.None,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom,
                Location = new Point(0, 0)
            };

            // Dùng TableLayoutPanel bên trong card để nút không bao giờ bị overflow
            var stack = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 6,
                Padding = new Padding(28, 20, 28, 16)
            };
            stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            // Mỗi row: các nút cao cố định, còn lại tự fill
            stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));  // Login
            stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));  // Register
            stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));  // Dual
            stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));  // hint
            stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));  // chkSim
            stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));  // Exit

            ButtonLogin = FormHelper.MakeButton("Đăng nhập", AppTheme.Primary, AppTheme.PrimaryHover, height: 42);
            ButtonRegister = FormHelper.MakeButton("Đăng ký", AppTheme.Accent, AppTheme.AccentHover, height: 42);
            ButtonDual = FormHelper.MakeButton("Mở song song KH + TX", AppTheme.Success, AppTheme.SuccessHover, height: 38);
            ButtonExit = FormHelper.MakeOutlineButton("Thoát", height: 36);

            foreach (var btn in new[] { ButtonLogin, ButtonRegister, ButtonDual, ButtonExit })
            {
                btn.Dock = DockStyle.Fill;
                btn.Margin = new Padding(0, 4, 0, 4);
            }

            ButtonLogin.Click += (_, _) => OnLoginClicked();
            ButtonRegister.Click += (_, _) => OnRegisterClicked();
            ButtonDual.Click += async (_, _) => await OnOpenDualClicked();
            ButtonExit.Click += (_, _) => OnExitClicked();

            var lblTest = new Label
            {
                Text = "Test: KH 0900000001 / 123456  •  TX 0900000003 / 123456",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = AppTheme.TextMuted,
                Font = new Font("Segoe UI", 8.5f)
            };

            var chkSim = new CheckBox
            {
                Text = "Bật mô phỏng tự động",
                Checked = AppRuntime.SimulationConfig.Enabled,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            chkSim.CheckedChanged += (_, _) =>
                AppRuntime.SimulationConfig.Enabled = chkSim.Checked;

            stack.Controls.Add(ButtonLogin, 0, 0);
            stack.Controls.Add(ButtonRegister, 0, 1);
            stack.Controls.Add(ButtonDual, 0, 2);
            stack.Controls.Add(lblTest, 0, 3);
            stack.Controls.Add(chkSim, 0, 4);
            stack.Controls.Add(ButtonExit, 0, 5);

            card.Controls.Add(stack);

            // card lấp đầy contentHost
            card.Dock = DockStyle.Fill;
            contentHost.Controls.Add(card);

            // ── Thứ tự Add quan trọng: Bottom trước, Top tiếp, Fill cuối ─────
            Controls.Add(contentHost);   // Fill → phần còn lại
            Controls.Add(header);        // Top
            Controls.Add(_statusPanel);  // Bottom
        }

        // ── Event handlers ────────────────────────────────────────────────────

        private void OnLoginClicked()
        {
            using var loginForm = _loginFormFactory();
            Hide();
            loginForm.ShowDialog();
            Show();
        }

        private void OnRegisterClicked()
        {
            using var regForm = _registerFormFactory();
            Hide();
            regForm.ShowDialog();
            Show();
        }

        private void OnExitClicked() => System.Windows.Forms.Application.Exit();

        private async Task OnOpenDualClicked()
        {
            try
            {
                if (_userService == null)
                    throw new InvalidOperationException("UserService chưa được khởi tạo.");
                if (_passengerDashboardFactory == null)
                    throw new InvalidOperationException("Passenger factory chưa được khởi tạo.");
                if (_driverDashboardFactory == null)
                    throw new InvalidOperationException("Driver factory chưa được khởi tạo.");

                if (_testPassengerForm == null || _testPassengerForm.IsDisposed)
                {
                    var user = await _userService.Login("0900000001", "123456");
                    if (user is not Passenger p)
                        throw new InvalidOperationException("Tài khoản KH test không hợp lệ.");
                    _testPassengerForm = _passengerDashboardFactory(p)
                        ?? throw new InvalidOperationException("Không thể tạo Passenger form.");
                    _testPassengerForm.FormClosed += (_, _) => _testPassengerForm = null;
                    _testPassengerForm.Show();
                }

                if (_testDriverForm == null || _testDriverForm.IsDisposed)
                {
                    var user = await _userService.Login("0900000003", "123456");
                    if (user is not Driver d)
                        throw new InvalidOperationException("Tài khoản TX test không hợp lệ.");
                    _testDriverForm = _driverDashboardFactory(d)
                        ?? throw new InvalidOperationException("Không thể tạo Driver form.");
                    _testDriverForm.FormClosed += (_, _) => _testDriverForm = null;
                    _testDriverForm.Show();
                }
            }
            catch (Exception ex)
            {
                FormHelper.ShowError(ex.Message + "\n\nStack: " + ex.StackTrace);
            }
        }
    }
}