using OOP.Presentation.Common.Theme;

namespace OOP.Presentation.BaseForms
{
    /// <summary>
    /// Lớp cơ sở trừu tượng cho tất cả Form trong hệ thống.
    /// Cung cấp factory methods và theme aliases — KHÔNG tự động build layout.
    /// Mỗi subclass tự quyết định cách build UI của mình.
    /// </summary>
    public abstract class BaseForm : Form
    {
        // ── Theme aliases ─────────────────────────────────────────────────────
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

        // ── Sizing aliases ────────────────────────────────────────────────────
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
        protected const int RowHeight = AppTheme.RowHeight;

        // ── Font aliases ──────────────────────────────────────────────────────
        protected static Font TitleFont => AppTheme.TitleFont;
        protected static Font BodyFont => AppTheme.DefaultFont;
        protected static Font SmallFont => AppTheme.SmallFont;

        // ─────────────────────────────────────────────────────────────────────
        protected BaseForm()
        {
            Font = BodyFont;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = PageBg;
            AutoScaleMode = AutoScaleMode.Font;
            DoubleBuffered = true; // Reduces flicker when repainting controls
        }

        /// <summary>
        /// Hook post-load — override in subclasses that need it.
        /// Intentionally NOT called automatically from BaseForm.
        /// </summary>
        protected virtual void ApplyBaseStyles() { }

        // ── Factory methods ───────────────────────────────────────────────────

        protected Button CreateStyledButton(string text, Color color, Color hoverColor,
            int width, int height)
        {
            var btn = new Button
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
            btn.FlatAppearance.BorderSize = 0;
            btn.MouseEnter += (_, _) => btn.BackColor = hoverColor;
            btn.MouseLeave += (_, _) => btn.BackColor = color;
            return btn;
        }

        protected TextBox CreateStyledTextBox(string placeholder, int width, int height)
        {
            return new TextBox
            {
                Font = BodyFont,
                Width = width,
                Height = height,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                ForeColor = TextPrimary,
                PlaceholderText = placeholder
            };
        }

        protected Label CreateStyledLabel(string text, Font font, Color foreColor)
        {
            return new Label
            {
                Text = text,
                Font = font,
                ForeColor = foreColor,
                AutoSize = true
            };
        }

        protected Panel CreateStyledPanel(int width, int height, Color backColor)
        {
            return new Panel
            {
                Width = width,
                Height = height,
                BackColor = backColor,
                BorderStyle = BorderStyle.None
            };
        }

        protected DataGridView CreateStyledDataGridView()
        {
            var dgv = new DataGridView
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

            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            dgv.ColumnHeadersDefaultCellStyle.BackColor = AppTheme.SidebarBg;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = AppTheme.SidebarHover;
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            dgv.DefaultCellStyle.BackColor = Color.White;
            dgv.DefaultCellStyle.ForeColor = TextPrimary;
            dgv.DefaultCellStyle.SelectionBackColor = AppTheme.Highlight;
            dgv.DefaultCellStyle.SelectionForeColor = TextPrimary;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = AppTheme.CardAlt;

            return dgv;
        }

        protected void ApplyHoverEffect(Control ctrl, Color normal, Color hover)
        {
            ctrl.MouseEnter += (_, _) => ctrl.BackColor = hover;
            ctrl.MouseLeave += (_, _) => ctrl.BackColor = normal;
        }
    }
}
