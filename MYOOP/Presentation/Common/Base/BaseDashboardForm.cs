using OOP.Presentation.Common.Theme;

namespace OOP.Presentation.BaseForms
{
    /// <summary>
    /// Lớp cơ sở cho các dashboard form (Passenger, Driver, Admin).
    /// Cung cấp layout chuẩn: Header + Sidebar + Content.
    /// </summary>
    public abstract class BaseDashboardForm : BaseForm
    {
        // ── Protected layout panels (subclass đọc để add controls) ────────────
        protected Panel HeaderPanel { get; private set; } = null!;
        protected Panel SidebarPanel { get; private set; } = null!;
        protected Panel ContentPanel { get; private set; } = null!;

        // ── Sidebar nav state ─────────────────────────────────────────────────
        private Button? _activeNavButton;

        protected BaseDashboardForm()
        {
            FormBorderStyle = FormBorderStyle.Sizable;
            WindowState = FormWindowState.Normal;
            Size = AppTheme.StandardSize;
            MinimumSize = AppTheme.StandardMinSize;

            BuildBaseLayout();
        }

        private void BuildBaseLayout()
        {
            SuspendLayout();

            HeaderPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = AppTheme.HeaderHeight,
                BackColor = AppTheme.Primary
            };

            SidebarPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = AppTheme.SidebarWidth,
                BackColor = AppTheme.SidebarBg
            };

            ContentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.PageBg
            };

            // Thứ tự add quan trọng: Fill trước, Left/Top sau
            Controls.Add(ContentPanel);
            Controls.Add(SidebarPanel);
            Controls.Add(HeaderPanel);

            ResumeLayout(false);
        }

        // ── Sidebar nav helper ──────────────────────────────────────────────

        /// <summary>
        /// Tạo nút sidebar chuẩn và đăng ký vào SidebarPanel.
        /// Tự động highlight khi active.
        /// </summary>
        protected Button AddSidebarNav(string icon, string label, EventHandler onClick)
        {
            if (onClick == null) throw new ArgumentNullException(nameof(onClick));

            var btn = new Button
            {
                Text = $"{icon}  {label}",
                Dock = DockStyle.Top,
                Height = 48,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(200, 215, 235),
                Font = new Font("Segoe UI", 10f),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(20, 0, 0, 0),
                Cursor = Cursors.Hand,
                Margin = Padding.Empty
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.MouseEnter += (_, _) => { if (btn != _activeNavButton) btn.BackColor = AppTheme.SidebarHover; };
            btn.MouseLeave += (_, _) => { if (btn != _activeNavButton) btn.BackColor = Color.Transparent; };
            btn.Click += (s, e) =>
            {
                SetActiveNav(btn);
                onClick(s, e);
            };

            SidebarPanel.Controls.Add(btn);
            // Giữ thứ tự từ trên xuống theo thứ tự gọi AddSidebarNav()
            SidebarPanel.Controls.SetChildIndex(btn, 0);
            return btn;
        }

        protected void SetActiveNav(Button btn)
        {
            if (_activeNavButton != null)
            {
                _activeNavButton.BackColor = Color.Transparent;
                _activeNavButton.ForeColor = Color.FromArgb(200, 215, 235);
                _activeNavButton.Font = new Font("Segoe UI", 10f, FontStyle.Regular);
            }
            _activeNavButton = btn;
            btn.BackColor = AppTheme.SidebarHover;
            btn.ForeColor = Color.White;
            btn.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
        }

        /// <summary>
        /// Load một host panel vào ContentPanel nếu cần bọc thêm.
        /// </summary>
        protected void RegisterContentHost(Panel host)
        {
            if (host == null) throw new ArgumentNullException(nameof(host));
            host.Dock = DockStyle.Fill;
            ContentPanel.Controls.Add(host);
        }
    }
}
