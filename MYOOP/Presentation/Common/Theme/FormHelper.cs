using OOP.Presentation.Common.Theme;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace OOP.Presentation
{
    public static class FormHelper
    {
        // ── Form setup ────────────────────────────────────────────────────────

        /// <summary>Thiết lập chuẩn cho Form: kích thước, vị trí, khả năng co giãn.</summary>
        public static void ApplyFormStandard(Form form, bool allowMaximize = true)
        {
            form.Size = AppTheme.StandardSize;
            form.MinimumSize = AppTheme.StandardMinSize;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.BackColor = AppTheme.PageBg;
            form.MaximizeBox = allowMaximize;
            form.MinimizeBox = true;
            form.FormBorderStyle = FormBorderStyle.Sizable;
        }

        // ── Label ─────────────────────────────────────────────────────────────

        public static Label MakeLabel(
            string text,
            float fontSize = 10f,
            FontStyle style = FontStyle.Regular,
            ContentAlignment align = ContentAlignment.MiddleLeft,
            Color? foreColor = null)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI", fontSize, style),
                TextAlign = align,
                ForeColor = foreColor ?? AppTheme.TextPrimary,
                BackColor = Color.Transparent,
                AutoSize = false
            };
        }

        /// <summary>Label nhỏ kiểu section title (uppercase, muted).</summary>
        public static Label MakeSectionLabel(string text) => new()
        {
            Text = text,
            Font = AppTheme.CaptionFont,
            ForeColor = AppTheme.TextMuted,
            BackColor = Color.Transparent,
            AutoSize = true
        };

        // ── Input ─────────────────────────────────────────────────────────────

        /// <summary>TextBox chuẩn với placeholder, border và font nhất quán.</summary>
        public static TextBox MakeInput(string placeholder, bool isPassword = false)
        {
            return new TextBox
            {
                PlaceholderText = placeholder,
                UseSystemPasswordChar = isPassword,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10.5f),
                BackColor = Color.White,
                ForeColor = AppTheme.TextPrimary,
                Height = AppTheme.InputHeight
            };
        }

        /// <summary>TextBox với kích thước cụ thể.</summary>
        public static TextBox MakeInputSized(string placeholder, int width,
            int height = AppTheme.InputHeight, bool isPassword = false)
        {
            var tb = MakeInput(placeholder, isPassword);
            tb.Width = width;
            tb.Height = height;
            return tb;
        }

        // ── Button ────────────────────────────────────────────────────────────

        /// <summary>Button phẳng với màu nền, hover effect và font chuẩn.</summary>
        public static Button MakeButton(
            string text,
            Color bgColor,
            Color hoverColor,
            int height = AppTheme.ButtonHeight,
            Color? fore = null,
            float fontSize = 10.5f,
            FontStyle fontStyle = FontStyle.Bold)
        {
            var btn = new Button
            {
                Text = text,
                Height = height,
                BackColor = bgColor,
                ForeColor = fore ?? Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", fontSize, fontStyle)
            };
            btn.FlatAppearance.BorderSize = 0;
            AttachHover(btn, bgColor, hoverColor);
            return btn;
        }

        /// <summary>Button outline (viền, nền trắng).</summary>
        public static Button MakeOutlineButton(string text, int height = AppTheme.SmallButton)
        {
            var btn = new Button
            {
                Text = text,
                Height = height,
                BackColor = Color.White,
                ForeColor = AppTheme.TextMuted,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10f)
            };
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = AppTheme.BorderLight;
            AttachHover(btn, Color.White, Color.FromArgb(245, 245, 248));
            return btn;
        }

        /// <summary>Button dạng toolbar — Dock=Left, height nhỏ, margin phải.</summary>
        public static Button MakeToolbarButton(string text, Color color, int width = 148)
        {
            var btn = new Button
            {
                Text = text,
                Dock = DockStyle.Left,
                Width = width,
                Height = 36,
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Margin = new Padding(0, 0, 6, 0)
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        /// <summary>Button sidebar — Dock=Top, nền trong suốt, hover effect.</summary>
        public static Button MakeSidebarButton(string text, int height = 46)
        {
            var btn = new Button
            {
                Text = text,
                Dock = DockStyle.Top,
                Height = height,
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10f),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(20, 0, 0, 0),
                Margin = new Padding(0)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.MouseEnter += (_, _) => btn.BackColor = AppTheme.SidebarHover;
            btn.MouseLeave += (_, _) => btn.BackColor = Color.Transparent;
            return btn;
        }

        // ── Containers ────────────────────────────────────────────────────────

        /// <summary>Panel dạng card với viền bo góc.</summary>
        public static Panel MakeCard(int width, int height, int radius = AppTheme.CardRadius)
        {
            var p = new Panel
            {
                Width = width,
                Height = height,
                BackColor = AppTheme.CardBg,
                Padding = new Padding(0)
            };
            p.Paint += RoundedBorderPainter(radius, AppTheme.BorderLight);
            p.Resize += (_, _) => ApplyRoundedRegion(p, radius);
            ApplyRoundedRegion(p, radius);
            return p;
        }

        /// <summary>Panel toolbar chuẩn (Dock=Top, CardBg, padding).</summary>
        public static Panel MakeToolbar(int height = 48)
        {
            return new Panel
            {
                Dock = DockStyle.Top,
                Height = height,
                BackColor = AppTheme.CardBg,
                Padding = new Padding(8, 7, 8, 7)
            };
        }

        /// <summary>
        /// FlowLayoutPanel dạng form — stack controls theo chiều dọc.
        /// Dùng cho các dialog đơn giản thay vì layout thủ công.
        /// </summary>
        public static FlowLayoutPanel MakeFormPanel(int padding = AppTheme.CardPadding)
        {
            return new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                Padding = new Padding(padding),
                WrapContents = false,
                AutoScroll = true
            };
        }

        // ── DataGridView ──────────────────────────────────────────────────────

        /// <summary>DataGridView chuẩn với style nhất quán.</summary>
        public static DataGridView MakeGrid()
        {
            var dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                BackgroundColor = AppTheme.CardBg,
                BorderStyle = BorderStyle.None,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                Font = AppTheme.SmallFont,
                ColumnHeadersHeight = AppTheme.HeaderRowH,
                RowTemplate = { Height = AppTheme.RowHeight },
                GridColor = AppTheme.BorderLight
            };

            dgv.ColumnHeadersDefaultCellStyle.BackColor = AppTheme.SidebarBg;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = AppTheme.SidebarHover;
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            dgv.DefaultCellStyle.SelectionBackColor = AppTheme.Highlight;
            dgv.DefaultCellStyle.SelectionForeColor = AppTheme.TextPrimary;
            dgv.DefaultCellStyle.Padding = new Padding(4, 0, 4, 0);
            dgv.AlternatingRowsDefaultCellStyle.BackColor = AppTheme.CardAlt;

            return dgv;
        }

        /// <summary>DataGridViewTextBoxColumn chuẩn.</summary>
        public static DataGridViewTextBoxColumn MakeGridColumn(
            string name, string header, int width, bool hidden = false)
        {
            return new DataGridViewTextBoxColumn
            {
                Name = name,
                HeaderText = header,
                Width = width,
                Visible = !hidden,
                SortMode = DataGridViewColumnSortMode.Automatic,
                DefaultCellStyle = new DataGridViewCellStyle { Padding = new Padding(4, 0, 4, 0) }
            };
        }

        // ── Layout ────────────────────────────────────────────────────────────

        /// <summary>Đặt control vào parent tại vị trí và kích thước cho trước.</summary>
        public static void Place(Control control, Control parent, int x, int y, int w, int h)
        {
            control.Left = x;
            control.Top = y;
            control.Width = w;
            control.Height = h;
            parent.Controls.Add(control);
        }

        /// <summary>
        /// Căn giữa card theo chiều ngang và dọc trong parent.
        /// topOffset: pixel từ trên không tính vào vùng căn (vd: header height).
        /// </summary>
        public static void CenterInParent(Control card, Control parent, int topOffset = 0)
        {
            card.Location = new Point(
                (parent.ClientSize.Width - card.Width) / 2,
                topOffset + (parent.ClientSize.Height - topOffset - card.Height) / 2);
        }

        // ── Drawing ───────────────────────────────────────────────────────────

        /// <summary>Tạo GraphicsPath hình chữ nhật bo góc.</summary>
        public static GraphicsPath RoundedRect(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        /// <summary>PaintEventHandler vẽ viền bo góc — gắn vào panel.Paint.</summary>
        public static PaintEventHandler RoundedBorderPainter(
            int radius = AppTheme.CardRadius, Color? borderColor = null)
        {
            return (s, e) =>
            {
                if (s is not Control ctrl) return;
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(borderColor ?? AppTheme.BorderLight);
                using var path = RoundedRect(
                    new Rectangle(0, 0, ctrl.Width - 1, ctrl.Height - 1), radius);
                g.DrawPath(pen, path);
            };
        }

        /// <summary>
        /// Áp dụng Region bo góc cho panel (GDI+) để clip children.
        /// Dùng cho Panel/Control thông thường.
        /// </summary>
        public static void ApplyRoundedRegion(Control ctrl, int radius)
        {
            using var path = RoundedRect(
                new Rectangle(0, 0, ctrl.Width, ctrl.Height), radius);
            ctrl.Region = new Region(path);
        }

        /// <summary>
        /// Áp dụng Region bo góc bằng GDI (CreateRoundRectRgn).
        /// Dùng thay thế khi ApplyRoundedRegion không clip đúng trên một số Panel.
        /// Thay thế cho NativeMethods class trong từng Form.
        /// </summary>
        public static void MakeRound(Control ctrl, int radius)
        {
            ctrl.Region = System.Drawing.Region.FromHrgn(
                CreateRoundRectRgn(0, 0, ctrl.Width + 1, ctrl.Height + 1,
                    radius * 2, radius * 2));
        }

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateRoundRectRgn(
            int nLeftRect, int nTopRect, int nRightRect, int nBottomRect,
            int nWidthEllipse, int nHeightEllipse);

        // ── Behavior ──────────────────────────────────────────────────────────

        /// <summary>Gắn hover effect — chỉ kích hoạt khi button đang Enabled.</summary>
        public static void AttachHover(Button btn, Color normal, Color hover)
        {
            btn.MouseEnter += (_, _) => { if (btn.Enabled) btn.BackColor = hover; };
            btn.MouseLeave += (_, _) => { if (btn.Enabled) btn.BackColor = normal; };
        }

        /// <summary>
        /// Toggle hiển thị mật khẩu.
        ///   👁  = đang ẩn (•••), click để xem
        ///   🙈 = đang hiện, click để ẩn
        /// </summary>
        public static void TogglePasswordVisibility(TextBox txtPassword, Button btnToggle)
        {
            txtPassword.UseSystemPasswordChar = !txtPassword.UseSystemPasswordChar;
            btnToggle.Text = txtPassword.UseSystemPasswordChar ? "👁" : "🙈";
            txtPassword.Focus();
        }

        /// <summary>Enter tại control source → focus tới target.</summary>
        public static void AttachEnterFocus(Control source, Control target)
        {
            source.KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    target.Focus();
                }
            };
        }

        /// <summary>Enter tại source → invoke button click nếu button enabled.</summary>
        public static void AttachEnterSubmit(Control source, Button submitBtn)
        {
            source.KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Enter && submitBtn.Enabled)
                {
                    e.SuppressKeyPress = true;
                    submitBtn.PerformClick();
                }
            };
        }

        // ── Stat card ─────────────────────────────────────────────────────────

        /// <summary>Stat card cho admin dashboard (icon + title + value + accent bar).</summary>
        public static Label MakeStatCard(TableLayoutPanel layout, int col,
            string icon, string title, Color accent)
        {
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(6),
                BackColor = AppTheme.CardBg,
                Padding = new Padding(12),
                BorderStyle = BorderStyle.FixedSingle
            };
            var lblIcon = new Label { Text = icon, Font = new Font("Segoe UI", 14f), Location = new Point(12, 10), AutoSize = true };
            var lblTitle = new Label { Text = title, Font = new Font("Segoe UI", 8.5f), ForeColor = AppTheme.TextMuted, Location = new Point(44, 12), AutoSize = true };
            var lblValue = new Label { Text = "--", Font = new Font("Segoe UI", 22f, FontStyle.Bold), ForeColor = accent, Location = new Point(12, 36), AutoSize = true };
            var accentBar = new Panel { Dock = DockStyle.Bottom, Height = 3, BackColor = accent };
            card.Controls.Add(accentBar);
            card.Controls.Add(lblValue);
            card.Controls.Add(lblTitle);
            card.Controls.Add(lblIcon);
            layout.Controls.Add(card, col, 0);
            return lblValue;
        }

        // ── Search panel ──────────────────────────────────────────────────────

        /// <summary>Panel tìm kiếm chuẩn (Dock=Top, CardBg, TextBox fill).</summary>
        public static Panel MakeSearchPanel(out TextBox txt, string placeholder)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 42,
                BackColor = AppTheme.CardBg,
                Padding = new Padding(8, 5, 8, 5)
            };
            txt = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10),
                PlaceholderText = placeholder
            };
            panel.Controls.Add(txt);
            return panel;
        }

        // ── Strip label ──────────────────────────────────────────────────────

        /// <summary>Label cho summary strip (đồng bộ giữa Passenger/Driver history, admin).</summary>
        public static Label MakeStripLabel(string text) => new()
        {
            Text = text,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = AppTheme.TextPrimary,
            BackColor = Color.Transparent
        };

        // ── Navigation ───────────────────────────────────────────────────────

        /// <summary>Button điều hướng bottom nav (icon + label, TableLayoutPanel).</summary>
        public static Button MakeNavButton(string icon, string label,
            Color? backColor = null, Color? foreColor = null)
        {
            var btn = new Button
            {
                Text = $"{icon}\n{label}",
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                BackColor = backColor ?? Color.White,
                ForeColor = foreColor ?? AppTheme.TextMuted,
                Font = new Font("Segoe UI", 8.5f),
                Cursor = Cursors.Hand,
                Margin = Padding.Empty
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.MouseEnter += (_, _) => btn.BackColor = AppTheme.SidebarHover;
            btn.MouseLeave += (_, _) => btn.BackColor = backColor ?? Color.White;
            return btn;
        }

        // ── Status indicator ──────────────────────────────────────────────────

        /// <summary>
        /// Panel status bar đơn giản (Dock=Top, colored background, label bên trong).
        /// Trả về (panel, label) để caller update text/color sau.
        /// </summary>
        public static (Panel panel, Label label) MakeStatusBar(int height = 40)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Top,
                Height = height,
                BackColor = AppTheme.Highlight,
                Visible = false
            };
            var lbl = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = AppTheme.Primary,
                Padding = new Padding(20, 0, 0, 0)
            };
            panel.Controls.Add(lbl);
            return (panel, lbl);
        }

        /// <summary>Cập nhật status bar: set text, màu chữ, và hiện panel.</summary>
        public static void SetStatus(Panel panel, Label label, string text, Color color)
        {
            if (panel.InvokeRequired)
            {
                panel.BeginInvoke(() => SetStatus(panel, label, text, color));
                return;
            }
            label.Text = text;
            label.ForeColor = color;
            panel.Visible = true;
        }

        // ── Interaction helpers ───────────────────────────────────────────────

        /// <summary>Hiển thị lỗi chuẩn hoá toàn hệ thống.</summary>
        public static void ShowError(string message, string title = "Lỗi")
        {
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        /// <summary>Hiển thị thông báo thành công chuẩn hoá.</summary>
        public static void ShowSuccess(string message, string title = "Thành công")
        {
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>Hộp thoại xác nhận Yes/No chuẩn hoá. Trả về true nếu Yes.</summary>
        public static bool ShowConfirm(string message, string title = "Xác nhận")
        {
            return MessageBox.Show(message, title,
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        }

        /// <summary>Đặt trạng thái loading cho button (text + cursor + enabled).</summary>
        public static void SetLoading(Button btn, bool loading,
            string loadingText, string normalText,
            Button? secondaryBtn = null)
        {
            btn.Enabled = !loading;
            btn.Text = loading ? loadingText : normalText;
            if (secondaryBtn != null) secondaryBtn.Enabled = !loading;
            btn.Cursor = loading ? Cursors.Default : Cursors.Hand;
        }
    }
}
