﻿﻿using OOP.Presentation;
using OOP.Domain.Entities;
using OOP.Application.Services.Interfaces;

namespace OOP
{
    public class MainForm : Form
    {
        // Sử dụng Delegate/Factory để khởi tạo Form mà không cần quan tâm dependencies bên trong
        private readonly Func<LoginForm> _loginFormFactory;
        private readonly Func<RegisterForm> _registerFormFactory;
        private readonly IAuthService _authService;
        private readonly Func<Passenger, Form> _passengerDashboardFactory;
        private readonly Func<Driver, Form> _driverDashboardFactory;

        private Label LabelTitle = null!;
        private Button ButtonLogin = null!;
        private Button ButtonRegister = null!;
        private Button ButtonDual = null!;
        private Button ButtonExit = null!;

        private Form? _testPassengerForm;
        private Form? _testDriverForm;

        // Constructor mới: Nhận các Factory từ Program.cs
        public MainForm(
            Func<LoginForm> loginFormFactory,
            Func<RegisterForm> registerFormFactory,
            IAuthService authService,
            Func<Passenger, Form> passengerDashboardFactory,
            Func<Driver, Form> driverDashboardFactory)
        {
            _loginFormFactory = loginFormFactory ?? throw new ArgumentNullException(nameof(loginFormFactory));
            _registerFormFactory = registerFormFactory ?? throw new ArgumentNullException(nameof(registerFormFactory));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _passengerDashboardFactory = passengerDashboardFactory ?? throw new ArgumentNullException(nameof(passengerDashboardFactory));
            _driverDashboardFactory = driverDashboardFactory ?? throw new ArgumentNullException(nameof(driverDashboardFactory));

            InitForm();
            BuildUI();
        }

        private void InitForm()
        {
            Text = "RideGo";
            Size = new Size(520, 440);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 10.5f);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            BackColor = AppTheme.PageBg;
        }

        private void BuildUI()
        {
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 130,
                BackColor = AppTheme.Primary,
                Padding = new Padding(28, 22, 28, 16)
            };

            LabelTitle = new Label
            {
                Text = "RideGo",
                Dock = DockStyle.Top,
                Height = 44,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 24, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent
            };

            var lblSub = new Label
            {
                Text = "Nền tảng đặt xe nội bộ — demo hệ thống",
                Dock = DockStyle.Top,
                Height = 22,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(220, 235, 255),
                BackColor = Color.Transparent
            };

            header.Controls.Add(lblSub);
            header.Controls.Add(LabelTitle);

            var card = FormHelper.MakeCard(360, 240);
            FormHelper.CenterInParent(card, this, topOffset: 150);
            Resize += (_, _) => FormHelper.CenterInParent(card, this, topOffset: 150);

            ButtonLogin = FormHelper.MakeButton("Đăng nhập", AppTheme.Primary, AppTheme.PrimaryHover, height: 46);
            ButtonRegister = FormHelper.MakeButton("Đăng ký", AppTheme.Accent, AppTheme.AccentHover, height: 46);
            ButtonDual = FormHelper.MakeButton("Mở song song KH + TX", AppTheme.Success, AppTheme.SuccessHover, height: 42);
            ButtonExit = FormHelper.MakeOutlineButton("Thoát", height: 38);

            ButtonLogin.Width = 280;
            ButtonRegister.Width = 280;
            ButtonDual.Width = 280;
            ButtonExit.Width = 280;

            ButtonLogin.Click += (s, e) => OnLoginClicked();
            ButtonRegister.Click += (s, e) => OnRegisterClicked();
            ButtonDual.Click += async (s, e) => await OnOpenDualClicked();
            ButtonExit.Click += (s, e) => OnExitClicked();

            var stack = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                Padding = new Padding(32, 24, 32, 16),
                WrapContents = false
            };
            stack.Controls.Add(ButtonLogin);
            stack.Controls.Add(ButtonRegister);
            stack.Controls.Add(ButtonDual);

            var lblTest = new Label
            {
                Text = "Test nhanh: KH 0900000001 / 123456 • TX 0900000003 / 123456",
                AutoSize = false,
                Width = 280,
                Height = 34,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = AppTheme.TextMuted,
                Font = new Font("Segoe UI", 8.5f)
            };
            stack.Controls.Add(lblTest);

            var chkSim = new CheckBox
            {
                Text = "Bật mô phỏng tự động",
                Checked = Program.SimulationConfig.Enabled,
                AutoSize = false,
                Width = 280,
                Height = 24,
                TextAlign = ContentAlignment.MiddleCenter
            };
            chkSim.CheckedChanged += (_, _) =>
                Program.SimulationConfig.Enabled = chkSim.Checked;
            stack.Controls.Add(chkSim);
            stack.Controls.Add(ButtonExit);

            ButtonLogin.Margin = new Padding(0, 0, 0, 12);
            ButtonRegister.Margin = new Padding(0, 0, 0, 12);
            ButtonDual.Margin = new Padding(0, 0, 0, 12);
            lblTest.Margin = new Padding(0, 0, 0, 8);
            chkSim.Margin = new Padding(0, 0, 0, 8);

            card.Controls.Add(stack);
            Controls.Add(card);
            Controls.Add(header);
        }

        // ── EVENTS ─────────────────────────────

        private void OnLoginClicked()
        {
            // Tạo form thông qua factory đã được setup ở Program.cs
            using var loginForm = _loginFormFactory();
            this.Hide();
            loginForm.ShowDialog();
            this.Show();
        }

        private void OnRegisterClicked()
        {
            using var regForm = _registerFormFactory();
            this.Hide();
            regForm.ShowDialog();
            this.Show();
        }

        private void OnExitClicked()
        {
            System.Windows.Forms.Application.Exit();
        }

        private async Task OnOpenDualClicked()
        {
            try
            {
                if (_testPassengerForm == null || _testPassengerForm.IsDisposed)
                {
                    var user = await _authService.Login("0900000001", "123456");
                    if (user is not Passenger p)
                        throw new InvalidOperationException("Tài khoản KH test không hợp lệ.");

                    _testPassengerForm = _passengerDashboardFactory(p);
                    _testPassengerForm.FormClosed += (_, _) => _testPassengerForm = null;
                    _testPassengerForm.Show();
                }

                if (_testDriverForm == null || _testDriverForm.IsDisposed)
                {
                    var user = await _authService.Login("0900000003", "123456");
                    if (user is not Driver d)
                        throw new InvalidOperationException("Tài khoản TX test không hợp lệ.");

                    _testDriverForm = _driverDashboardFactory(d);
                    _testDriverForm.FormClosed += (_, _) => _testDriverForm = null;
                    _testDriverForm.Show();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Không thể mở song song",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}




