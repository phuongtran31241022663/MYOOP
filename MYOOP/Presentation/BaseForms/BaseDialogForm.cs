namespace OOP.Presentation.BaseForms
{
    /// <summary>
    /// Lớp cơ sở cho các form dialog (Login, Register, Profile, Rating).
    /// Có kích thước cố định, không có nút phóng to.
    /// </summary>
    public abstract class BaseDialogForm : BaseForm
    {
        protected BaseDialogForm()
        {
            // Thiết lập riêng cho dialog
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
        }

        protected override void ApplyBaseStyles()
        {
            base.ApplyBaseStyles();
            
            // Thiết lập style đặc biệt cho dialog
            BackColor = Color.White;
            
            // Tạo header panel
            var headerPanel = CreateHeaderPanel();
            if (headerPanel != null)
            {
                headerPanel.Dock = DockStyle.Top;
                headerPanel.Height = 60;
                Controls.Add(headerPanel);
            }
        }

        /// <summary>
        /// Tạo header panel cho dialog
        /// </summary>
        /// <returns>Panel header</returns>
        protected virtual Panel CreateHeaderPanel()
        {
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.White,
                Padding = new Padding(PaddingLarge, PaddingLarge, PaddingLarge, PaddingSmall)
            };

            var titleLabel = CreateStyledLabel(Text, TitleFont, TextPrimary);
            titleLabel.Dock = DockStyle.Fill;
            titleLabel.TextAlign = ContentAlignment.MiddleLeft;

            header.Controls.Add(titleLabel);
            return header;
        }

        /// <summary>
        /// Tạo footer panel cho dialog (chứa các nút hành động)
        /// </summary>
        /// <returns>Panel footer</returns>
        protected Panel CreateFooterPanel()
        {
            var footer = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.White,
                Padding = new Padding(PaddingLarge, PaddingSmall, PaddingLarge, PaddingLarge)
            };

            return footer;
        }

        /// <summary>
        /// Tạo nút OK chuẩn cho dialog
        /// </summary>
        /// <returns>Button OK</returns>
        protected Button CreateOkButton()
        {
            var okButton = CreateStyledButton("OK", SuccessColor, SuccessHover, 100, ButtonHeight);
            okButton.DialogResult = DialogResult.OK;
            return okButton;
        }

        /// <summary>
        /// Tạo nút Cancel chuẩn cho dialog
        /// </summary>
        /// <returns>Button Cancel</returns>
        protected Button CreateCancelButton()
        {
            var cancelButton = CreateStyledButton("Cancel", Color.White, Color.LightGray, 100, ButtonHeight);
            cancelButton.ForeColor = TextPrimary;
            cancelButton.DialogResult = DialogResult.Cancel;
            return cancelButton;
        }

        /// <summary>
        /// Thiết lập layout chuẩn cho các control trong dialog
        /// </summary>
        /// <param name="controls">Các control cần sắp xếp</param>
        /// <param name="container">Container chứa các control</param>
        protected void SetupDialogLayout(Control[] controls, Panel container)
        {
            int currentY = PaddingLarge;
            
            foreach (var control in controls)
            {
                control.Location = new Point(PaddingLarge, currentY);
                control.Width = container.Width - (PaddingLarge * 2);
                
                if (control is TextBox textBox)
                {
                    textBox.Height = InputHeight;
                    currentY += InputHeight + SpacingMedium;
                }
                else if (control is Label label)
                {
                    label.Height = 20;
                    currentY += 20 + SpacingSmall;
                }
                else if (control is ComboBox comboBox)
                {
                    comboBox.Height = InputHeight;
                    currentY += InputHeight + SpacingMedium;
                }
                else
                {
                    control.Height = ControlHeight;
                    currentY += ControlHeight + SpacingMedium;
                }

                container.Controls.Add(control);
            }
        }
    }
}