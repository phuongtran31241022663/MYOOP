﻿using System.Drawing.Drawing2D;

namespace OOP.Presentation
{
    /// <summary>
    /// Helper methods dùng chung cho tất cả Forms.
    /// Trước đây bị copy-paste trong LoginForm, PassengerForm, DriverForm, v.v.
    /// </summary>
    public static class FormHelper
    {
        private static readonly Color InputBorder = Color.FromArgb(210, 214, 221);

        // ── Factory methods ──────────────────────────────────────────────────

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
                ForeColor = foreColor ?? SystemColors.ControlText,
                BackColor = Color.Transparent,
                AutoSize = false
            };
        }

        public static TextBox MakeInput(string placeholder, bool isPassword = false)
        {
            return new TextBox
            {
                PlaceholderText = placeholder,
                UseSystemPasswordChar = isPassword,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10.5f),
                BackColor = Color.White,
                ForeColor = AppTheme.TextPrimary
            };
        }

        /// <summary>
        /// Tạo Button với style phẳng, màu nền và màu hover.
        /// </summary>
        public static Button MakeButton(
            string text,
            Color bgColor,
            Color hoverColor,
            int height = AppTheme.ButtonHeight,
            Color? foreColor = null,
            float fontSize = 10.5f,
            FontStyle fontStyle = FontStyle.Bold)
        {
            var btn = new Button
            {
                Text = text,
                Height = height,
                BackColor = bgColor,
                ForeColor = foreColor ?? Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", fontSize, fontStyle)
            };
            btn.FlatAppearance.BorderSize = 0;

            AttachHover(btn, bgColor, hoverColor);
            return btn;
        }

        /// <summary>
        /// Tạo Button outline (viền, nền trắng).
        /// </summary>
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

            AttachHover(btn, Color.White, Color.FromArgb(245, 245, 245));
            return btn;
        }

        /// <summary>
        /// Tạo panel dạng card với bo góc và viền mảnh.
        /// </summary>
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
            return p;
        }

        /// <summary>
        /// Tạo TextBox với chiều cao cố định và border nhã.
        /// </summary>
        public static TextBox MakeInputSized(string placeholder, int width, int height = AppTheme.InputHeight)
        {
            var tb = MakeInput(placeholder);
            tb.Width = width;
            tb.Height = height;
            return tb;
        }

        // ── Layout ───────────────────────────────────────────────────────────

        /// <summary>
        /// Đặt control vào parent với vị trí và kích thước cho trước.
        /// </summary>
        public static void Place(Control control, Control parent, int x, int y, int width, int height)
        {
            control.Left = x;
            control.Top = y;
            control.Width = width;
            control.Height = height;
            parent.Controls.Add(control);
        }

        /// <summary>
        /// Căn giữa card theo chiều ngang và dọc trong parent.
        /// </summary>
        public static void CenterInParent(Control card, Control parent, int topOffset = 0)
        {
            card.Location = new Point(
                (parent.ClientSize.Width - card.Width) / 2,
                topOffset + (parent.ClientSize.Height - topOffset - card.Height) / 2);
        }

        // ── Drawing ──────────────────────────────────────────────────────────

        /// <summary>
        /// Tạo GraphicsPath hình chữ nhật bo góc — dùng cho Panel.Paint / Region.
        /// </summary>
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

        /// <summary>
        /// Tạo PaintEventHandler vẽ viền bo góc cho Panel — gán trực tiếp vào panel.Paint.
        /// </summary>
        public static PaintEventHandler RoundedBorderPainter(int radius = AppTheme.CardRadius, Color? borderColor = null)
        {
            return (s, e) =>
            {
                if (s is not Control ctrl) return;
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(borderColor ?? AppTheme.BorderLight);
                using var path = RoundedRect(new Rectangle(0, 0, ctrl.Width - 1, ctrl.Height - 1), radius);
                g.DrawPath(pen, path);
            };
        }

        // ── Behavior ─────────────────────────────────────────────────────────

        /// <summary>
        /// Gắn hiệu ứng hover màu — chỉ kích hoạt khi button đang Enabled.
        /// </summary>
        public static void AttachHover(Button btn, Color normal, Color hover)
        {
            btn.MouseEnter += (_, _) => { if (btn.Enabled) btn.BackColor = hover; };
            btn.MouseLeave += (_, _) => { if (btn.Enabled) btn.BackColor = normal; };
        }

        /// <summary>
        /// Toggle hiển thị mật khẩu và cập nhật icon nút.
        /// </summary>
        public static void TogglePasswordVisibility(TextBox txtPassword, Button btnToggle)
        {
            txtPassword.UseSystemPasswordChar = !txtPassword.UseSystemPasswordChar;
            btnToggle.Text = txtPassword.UseSystemPasswordChar ? "👁" : "🔒";
            txtPassword.Focus();
        }
    }
}