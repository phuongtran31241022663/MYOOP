using OOP.Presentation.Common.Theme;

namespace OOP.Presentation.BaseForms
{
    /// <summary>
    /// Lớp cơ sở cho các form dialog (Login, Register, Profile, Rating).
    /// Cung cấp helpers — KHÔNG tự build header hay footer.
    /// </summary>
    public abstract class BaseDialogForm : BaseForm
    {
        protected BaseDialogForm()
        {
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.White;
        }

        // ── Optional helpers subclasses can call if they want ─────────────────

        /// <summary>
        /// Tạo header panel với title — subclass gọi thủ công nếu cần.
        /// </summary>
        protected Panel CreateHeaderPanel(string? title = null)
        {
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = AppTheme.Primary,
                Padding = new Padding(PaddingLarge, 0, PaddingLarge, 0)
            };

            var lbl = new Label
            {
                Text = title ?? Text,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            header.Controls.Add(lbl);
            return header;
        }

        protected Panel CreateFooterPanel()
        {
            return new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.White,
                Padding = new Padding(PaddingLarge, PaddingSmall, PaddingLarge, PaddingLarge)
            };
        }

        protected Button CreateOkButton()
        {
            var btn = CreateStyledButton("OK", SuccessColor, SuccessHover, 100, ButtonHeight);
            btn.DialogResult = DialogResult.OK;
            return btn;
        }

        protected Button CreateCancelButton()
        {
            var btn = new Button
            {
                Text = "Cancel",
                Width = 100,
                Height = ButtonHeight,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = TextPrimary,
                DialogResult = DialogResult.Cancel,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = AppTheme.BorderLight;
            return btn;
        }

        /// <summary>
        /// Tự động đặt controls theo thứ tự dọc trong container.
        /// </summary>
        protected void SetupDialogLayout(Control[] controls, Panel container)
        {
            int y = PaddingLarge;
            foreach (var ctrl in controls)
            {
                ctrl.Location = new Point(PaddingLarge, y);
                ctrl.Width = container.Width - PaddingLarge * 2;

                int step = ctrl switch
                {
                    TextBox tb => (tb.Height = InputHeight, InputHeight + SpacingMedium).Item2,
                    Label lbl => (lbl.Height = 20, 20 + SpacingSmall).Item2,
                    ComboBox cb => (cb.Height = InputHeight, InputHeight + SpacingMedium).Item2,
                    _ => (ctrl.Height = ControlHeight, ControlHeight + SpacingMedium).Item2
                };
                y += step;
                container.Controls.Add(ctrl);
            }
        }
    }
}