using OOP.Presentation;

namespace OOP.Presentation.BaseForms
{
    /// <summary>
    /// Lớp cơ sở trừu tượng cho tất cả các Form trong hệ thống RideGo.
    /// Đảm bảo tính đồng nhất về giao diện và hành vi.
    /// Sử dụng AppTheme cho tất cả các hằng số style.
    /// </summary>
    public abstract class BaseForm : Form
    {
        // Aliases cho tương thích ngược
        protected static Color PrimaryColor => AppTheme.Primary;
        protected static Color PrimaryHover => AppTheme.PrimaryHover;
        protected static Color SuccessColor => AppTheme.Success;
        protected static Color SuccessHover => AppTheme.SuccessHover;
        protected static Color DangerColor => AppTheme.Danger;
        protected static Color DangerHover => AppTheme.DangerHover;
        protected static Color WarningColor => AppTheme.Warning;
        protected static Color WarningHover => AppTheme.WarningHover;
        protected static Color TextPrimary => AppTheme.TextPrimary;
        protected static Color TextMuted => AppTheme.TextMuted;
        protected static Color BorderLight => AppTheme.BorderLight;
        protected static Color CardBg => AppTheme.CardBg;
        protected static Color PageBg => AppTheme.PageBg;

        // Aliases cho sizing
        protected const int PaddingSmall = AppTheme.ControlGap;
        protected const int PaddingMedium = AppTheme.CardGap;
        protected const int PaddingLarge = AppTheme.LargeGap;
        protected const int SpacingSmall = AppTheme.ControlGap;
        protected const int SpacingMedium = AppTheme.CardGap;
        protected const int SpacingLarge = AppTheme.LargeGap;
        protected const int ControlHeight = AppTheme.InputHeight;
        protected const int ButtonHeight = AppTheme.ButtonHeight;
        protected const int InputHeight = AppTheme.InputHeight;
        protected const int HeaderHeight = 40;
        protected const int RowHeight = 35;

        // Aliases cho fonts
        protected static Font TitleFont => AppTheme.TitleFont;
        protected static Font BodyFont => AppTheme.DefaultFont;
        protected static Font SmallFont => AppTheme.SmallFont;

        protected BaseForm()
        {
            // Thiết lập cơ bản
            Font = BodyFont;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = PageBg;
            FormBorderStyle = FormBorderStyle.Sizable;
            AutoScaleMode = AutoScaleMode.Font;
            
            // Thiết lập sự kiện
            Load += BaseForm_Load;
            Shown += BaseForm_Shown;
        }

        private void BaseForm_Load(object? sender, EventArgs e)
        {
            ApplyBaseStyles();
        }

        private void BaseForm_Shown(object? sender, EventArgs e)
        {
            // Có thể override để thực hiện logic sau khi form hiển thị
        }

        /// <summary>
        /// Áp dụng các style cơ bản cho form
        /// </summary>
        protected virtual void ApplyBaseStyles()
        {
            // Override trong các lớp con nếu cần style đặc biệt
        }

        /// <summary>
        /// Tạo một nút bấm với style chuẩn
        /// </summary>
        /// <param name="text">Văn bản hiển thị</param>
        /// <param name="color">Màu nền</param>
        /// <param name="hoverColor">Màu nền khi hover</param>
        /// <param name="width">Chiều rộng</param>
        /// <param name="height">Chiều cao</param>
        /// <returns>Button đã được cấu hình</returns>
        protected Button CreateStyledButton(string text, Color color, Color hoverColor, int width, int height)
        {
            var button = new Button
            {
                Text = text,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Width = width,
                Height = height,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                BackColor = color,
                ForeColor = Color.White
            };

            button.FlatAppearance.BorderSize = 0;
            
            // Thêm hiệu ứng hover
            button.MouseEnter += (s, e) => button.BackColor = hoverColor;
            button.MouseLeave += (s, e) => button.BackColor = color;

            return button;
        }

        /// <summary>
        /// Tạo một TextBox với style chuẩn
        /// </summary>
        /// <param name="placeholder">Văn bản placeholder</param>
        /// <param name="width">Chiều rộng</param>
        /// <param name="height">Chiều cao</param>
        /// <returns>TextBox đã được cấu hình</returns>
        protected TextBox CreateStyledTextBox(string placeholder, int width, int height)
        {
            var textBox = new TextBox
            {
                Font = BodyFont,
                Width = width,
                Height = height,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                ForeColor = TextPrimary,
                PlaceholderText = placeholder
            };

            return textBox;
        }

        /// <summary>
        /// Tạo một Label với style chuẩn
        /// </summary>
        /// <param name="text">Văn bản hiển thị</param>
        /// <param name="font">Font chữ</param>
        /// <param name="foreColor">Màu chữ</param>
        /// <returns>Label đã được cấu hình</returns>
        protected Label CreateStyledLabel(string text, Font font, Color foreColor)
        {
            var label = new Label
            {
                Text = text,
                Font = font,
                ForeColor = foreColor,
                AutoSize = true
            };

            return label;
        }

        /// <summary>
        /// Tạo một Panel với style chuẩn
        /// </summary>
        /// <param name="width">Chiều rộng</param>
        /// <param name="height">Chiều cao</param>
        /// <param name="backColor">Màu nền</param>
        /// <returns>Panel đã được cấu hình</returns>
        protected Panel CreateStyledPanel(int width, int height, Color backColor)
        {
            var panel = new Panel
            {
                Width = width,
                Height = height,
                BackColor = backColor,
                BorderStyle = BorderStyle.None
            };

            return panel;
        }

        /// <summary>
        /// Tạo một DataGridView với style chuẩn
        /// </summary>
        /// <returns>DataGridView đã được cấu hình</returns>
        protected DataGridView CreateStyledDataGridView()
        {
            var dataGridView = new DataGridView
            {
                Font = BodyFont,
                RowTemplate = { Height = RowHeight },
                ColumnHeadersHeight = HeaderHeight,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                BackgroundColor = CardBg,
                BorderStyle = BorderStyle.None,
                GridColor = BorderLight
            };

            // Thiết lập style cho header
            dataGridView.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            dataGridView.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(33, 37, 41);
            dataGridView.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dataGridView.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(52, 58, 64);

            // Thiết lập style cho các hàng
            dataGridView.DefaultCellStyle.BackColor = Color.White;
            dataGridView.DefaultCellStyle.ForeColor = TextPrimary;
            dataGridView.DefaultCellStyle.SelectionBackColor = Color.FromArgb(233, 236, 239);
            dataGridView.DefaultCellStyle.SelectionForeColor = TextPrimary;

            return dataGridView;
        }

        /// <summary>
        /// Áp dụng hiệu ứng hover cho bất kỳ control nào
        /// </summary>
        /// <param name="control">Control cần áp dụng</param>
        /// <param name="normalColor">Màu bình thường</param>
        /// <param name="hoverColor">Màu khi hover</param>
        protected void ApplyHoverEffect(Control control, Color normalColor, Color hoverColor)
        {
            control.MouseEnter += (s, e) => control.BackColor = hoverColor;
            control.MouseLeave += (s, e) => control.BackColor = normalColor;
        }

        /// <summary>
        /// Thiết lập layout responsive cho control
        /// </summary>
        /// <param name="control">Control cần thiết lập</param>
        /// <param name="anchor">Anchor style</param>
        /// <param name="dock">Dock style</param>
        protected void SetLayout(Control control, AnchorStyles anchor, DockStyle dock)
        {
            control.Anchor = anchor;
            control.Dock = dock;
        }
    }
}