using OOP.Presentation;
using OOP.Domain.Entities;

namespace OOP
{
    public class MainForm : Form
    {
        // Sử dụng Delegate/Factory để khởi tạo Form mà không cần quan tâm dependencies bên trong
        private readonly Func<LoginForm> _loginFormFactory;
        private readonly Func<RegisterForm> _registerFormFactory;

        private Label LabelTitle = null!;
        private Button ButtonLogin = null!;
        private Button ButtonRegister = null!;
        private Button ButtonExit = null!;

        // Constructor mới: Nhận các Factory từ Program.cs
        public MainForm(Func<LoginForm> loginFormFactory, Func<RegisterForm> registerFormFactory)
        {
            _loginFormFactory = loginFormFactory ?? throw new ArgumentNullException(nameof(loginFormFactory));
            _registerFormFactory = registerFormFactory ?? throw new ArgumentNullException(nameof(registerFormFactory));

            InitForm();
            BuildUI();
        }

        private void InitForm()
        {
            Text = "RideGo";
            Size = new Size(420, 360);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 11);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
        }

        private void BuildUI()
        {
            LabelTitle = new Label
            {
                Text = "RideGo System",
                Dock = DockStyle.Top,
                Height = 80,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 18, FontStyle.Bold)
            };

            ButtonLogin = CreateButton("Đăng nhập");
            ButtonRegister = CreateButton("Đăng ký");
            ButtonExit = CreateButton("Thoát");

            ButtonLogin.Click += (s, e) => OnLoginClicked();
            ButtonRegister.Click += (s, e) => OnRegisterClicked();
            ButtonExit.Click += (s, e) => OnExitClicked();

            var panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                Padding = new Padding(110, 20, 100, 20),
                Controls = { ButtonLogin, ButtonRegister, ButtonExit }
            };

            Controls.Add(panel);
            Controls.Add(LabelTitle);
        }

        private Button CreateButton(string text) =>
            new Button
            {
                Text = text,
                Width = 180,
                Height = 45,
                Margin = new Padding(0, 0, 0, 15),
                Cursor = Cursors.Hand
            };

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
    }
}