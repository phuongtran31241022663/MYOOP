namespace OOP.Presentation.BaseForms
{
    /// <summary>
    /// Lớp cơ sở cho các dashboard form (Passenger, Driver, Admin).
    /// Có sidebar điều hướng và hỗ trợ responsive layout.
    /// </summary>
    public abstract class BaseDashboardForm : BaseForm
    {
        protected Panel SidebarPanel { get; private set; } = null!;
        protected Panel MainPanel { get; private set; } = null!;
        protected Panel HeaderPanel { get; private set; } = null!;

        protected BaseDashboardForm()
        {
            // Thiết lập riêng cho dashboard
            FormBorderStyle = FormBorderStyle.Sizable;
            WindowState = FormWindowState.Normal;
            Size = new Size(1280, 800);
            MinimumSize = new Size(1000, 600);
        }

        protected override void ApplyBaseStyles()
        {
            base.ApplyBaseStyles();
            
            // Thiết lập layout chính
            SetupMainLayout();
            
            // Thiết lập sidebar
            SetupSidebar();
            
            // Thiết lập header
            SetupHeader();
        }

        /// <summary>
        /// Thiết lập layout chính (sidebar + main content)
        /// </summary>
        private void SetupMainLayout()
        {
            // Sidebar
            SidebarPanel = CreateStyledPanel(260, ClientSize.Height, Color.FromArgb(33, 37, 41));
            SidebarPanel.Dock = DockStyle.Left;
            SidebarPanel.Padding = new Padding(PaddingLarge);
            SidebarPanel.Width = 260;

            // Main content
            MainPanel = CreateStyledPanel(ClientSize.Width - 260, ClientSize.Height, PageBg);
            MainPanel.Dock = DockStyle.Fill;
            MainPanel.Padding = new Padding(PaddingLarge);

            // Header
            HeaderPanel = CreateStyledPanel(MainPanel.Width, 80, Color.White);
            HeaderPanel.Dock = DockStyle.Top;
            HeaderPanel.Height = 80;
            HeaderPanel.Padding = new Padding(PaddingLarge);

            // Add header to main panel (before main content)
            MainPanel.Controls.Add(HeaderPanel);

            // Thêm các panel vào form
            Controls.Add(MainPanel);
            Controls.Add(SidebarPanel);
        }

        /// <summary>
        /// Thiết lập sidebar điều hướng
        /// </summary>
        protected virtual void SetupSidebar()
        {
            // Logo/Title
            var logoLabel = CreateStyledLabel("RideGo", TitleFont, Color.White);
            logoLabel.Font = new Font("Segoe UI", 16f, FontStyle.Bold);
            logoLabel.Dock = DockStyle.Top;
            logoLabel.Height = 50;
            logoLabel.Padding = new Padding(0, 20, 0, 0);

            // Separator
            var separator1 = new Panel
            {
                Dock = DockStyle.Top,
                Height = 1,
                BackColor = Color.FromArgb(60, 60, 60),
                Margin = new Padding(0, 10, 0, 10)
            };

            SidebarPanel.Controls.Add(separator1);
            SidebarPanel.Controls.Add(logoLabel);

            // Các nút điều hướng sẽ được override bởi các lớp con
        }

        /// <summary>
        /// Thiết lập header dashboard
        /// </summary>
        protected virtual void SetupHeader()
        {
            var titleLabel = CreateStyledLabel(Text, TitleFont, TextPrimary);
            titleLabel.Font = new Font("Segoe UI", 14f, FontStyle.Bold);
            titleLabel.Dock = DockStyle.Left;
            titleLabel.Padding = new Padding(0, 20, 0, 0);

            var userInfoPanel = CreateStyledPanel(300, 40, Color.Transparent);
            userInfoPanel.Dock = DockStyle.Right;
            userInfoPanel.Padding = new Padding(0, 20, 0, 0);

            var logoutButton = CreateStyledButton("Đăng xuất", DangerColor, DangerHover, 100, ButtonHeight);
            logoutButton.Dock = DockStyle.Right;
            logoutButton.Click += (s, e) => Close();

            userInfoPanel.Controls.Add(logoutButton);
            HeaderPanel.Controls.Add(userInfoPanel);
            HeaderPanel.Controls.Add(titleLabel);
        }

        /// <summary>
        /// Tạo một nút điều hướng cho sidebar
        /// </summary>
        /// <param name="text">Văn bản hiển thị</param>
        /// <param name="icon">Icon (nếu có)</param>
        /// <param name="clickHandler">Xử lý sự kiện click</param>
        /// <returns>Button đã được cấu hình</returns>
        protected Button CreateSidebarButton(string text, string? icon = null, EventHandler? clickHandler = null)
        {
            var button = new Button
            {
                Text = icon != null ? $"{icon}  {text}" : text,
                Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                Dock = DockStyle.Top,
                Height = 46,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                BackColor = Color.FromArgb(33, 37, 41),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(16, 0, 0, 0)
            };

            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(52, 58, 64);
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(40, 45, 50);

            if (clickHandler != null)
                button.Click += clickHandler;

            return button;
        }

        /// <summary>
        /// Tạo một panel chứa nội dung chính
        /// </summary>
        /// <param name="title">Tiêu đề panel</param>
        /// <returns>Panel đã được cấu hình</returns>
        protected Panel CreateContentPanel(string title)
        {
            var panel = CreateStyledPanel(MainPanel.Width - (PaddingLarge * 2), 400, Color.White);
            panel.Dock = DockStyle.Top;
            panel.Padding = new Padding(PaddingLarge);
            panel.Height = 400;

            var titleBar = CreateStyledPanel(panel.Width, 50, Color.FromArgb(248, 249, 250));
            titleBar.Dock = DockStyle.Top;
            titleBar.Height = 50;

            var titleLabel = CreateStyledLabel(title, TitleFont, TextPrimary);
            titleLabel.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            titleLabel.Dock = DockStyle.Left;
            titleLabel.Padding = new Padding(16, 14, 0, 0);

            titleBar.Controls.Add(titleLabel);
            panel.Controls.Add(titleBar);

            return panel;
        }

        /// <summary>
        /// Cập nhật thông tin người dùng trên header
        /// </summary>
        /// <param name="userName">Tên người dùng</param>
        /// <param name="userRole">Vai trò người dùng</param>
        protected void UpdateUserInfo(string userName, string userRole)
        {
            if (HeaderPanel.Controls.Count > 1)
            {
                var userInfoPanel = HeaderPanel.Controls[1] as Panel;
                if (userInfoPanel != null)
                {
                    // Xóa các control cũ
                    userInfoPanel.Controls.Clear();
                    
                    // Thêm thông tin người dùng
                    var userInfoLabel = CreateStyledLabel($"{userName} ({userRole})", BodyFont, TextMuted);
                    userInfoLabel.Dock = DockStyle.Left;
                    userInfoLabel.Padding = new Padding(0, 10, 16, 0);

                    var logoutButton = CreateStyledButton("Đăng xuất", DangerColor, DangerHover, 100, ButtonHeight);
                    logoutButton.Dock = DockStyle.Right;
                    logoutButton.Click += (s, e) => Close();

                    userInfoPanel.Controls.Add(logoutButton);
                    userInfoPanel.Controls.Add(userInfoLabel);
                }
            }
        }

        /// <summary>
        /// Thiết lập layout responsive khi form thay đổi kích thước
        /// </summary>
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            
            // Kiểm tra null trước khi truy cập - tránh lỗi khi form chưa load xong
            if (SidebarPanel == null || MainPanel == null || HeaderPanel == null)
                return;
            
            // Đảm bảo sidebar luôn có chiều rộng cố định
            SidebarPanel.Width = 260;
            
            // Main panel tự động co giãn
            MainPanel.Width = ClientSize.Width - 260;
            MainPanel.Height = ClientSize.Height;
            
            // Header tự động co giãn
            HeaderPanel.Width = MainPanel.Width;
        }
    }
}