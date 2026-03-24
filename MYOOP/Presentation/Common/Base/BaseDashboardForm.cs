using OOP.Presentation.Common.Theme;

namespace OOP.Presentation.BaseForms
{
    /// <summary>
    /// Lớp cơ sở cho các dashboard form (Passenger, Driver, Admin).
    /// Chỉ cung cấp helper factories — KHÔNG tự build layout.
    /// Các helper method (CreateSidebarButton, CreateContentPanel...) vẫn giữ lại
    /// để subclass dùng nếu muốn — nhưng không bắt buộc.
    /// </summary>
    public abstract class BaseDashboardForm : BaseForm
    {
        protected BaseDashboardForm()
        {
            FormBorderStyle = FormBorderStyle.Sizable;
            WindowState = FormWindowState.Normal;
            Size = AppTheme.DashboardSize;
            MinimumSize = AppTheme.DashboardMinSize;
        }

        // ── Sidebar button factory ────────────────────────────────────────────

        /// <summary>
        /// Tạo nút điều hướng sidebar (Dock=Top, dark background, hover effect).
        /// </summary>
        protected Button CreateSidebarButton(string text, string? icon = null,
            EventHandler? clickHandler = null)
        {
            var btn = new Button
            {
                Text = icon != null ? $"{icon}  {text}" : text,
                Font = new Font("Segoe UI", 10f),
                Dock = DockStyle.Top,
                Height = 46,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(16, 0, 0, 0),
                Margin = new Padding(0)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.MouseEnter += (_, _) => btn.BackColor = AppTheme.SidebarHover;
            btn.MouseLeave += (_, _) => btn.BackColor = Color.Transparent;

            if (clickHandler != null) btn.Click += clickHandler;
            return btn;
        }
    }
}