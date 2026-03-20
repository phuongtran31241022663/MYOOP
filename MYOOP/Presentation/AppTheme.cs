﻿using System.Drawing;

namespace OOP.Presentation
{
    /// <summary>
    /// Tập trung toàn bộ màu sắc, font, spacing và hằng số UI — thay thế các hardcode rải rác trong từng Form.
    /// </summary>
    public static class AppTheme
    {
        // ═══════════════════════════════════════════════════════════════
        // COLORS
        // ═══════════════════════════════════════════════════════════════

        // --- Brand colors ---
        public static readonly Color Primary = Color.FromArgb(18, 113, 255);
        public static readonly Color PrimaryHover = Color.FromArgb(12, 92, 210);

        public static readonly Color Success = Color.FromArgb(16, 140, 92);
        public static readonly Color SuccessHover = Color.FromArgb(12, 115, 76);

        public static readonly Color Warning = Color.FromArgb(232, 124, 26);
        public static readonly Color WarningHover = Color.FromArgb(200, 98, 18);

        public static readonly Color Danger = Color.FromArgb(210, 64, 70);
        public static readonly Color DangerHover = Color.FromArgb(175, 42, 48);

        public static readonly Color Accent = Color.FromArgb(46, 196, 182);
        public static readonly Color AccentHover = Color.FromArgb(34, 166, 154);

        // --- Neutrals ---
        public static readonly Color DarkBg = Color.FromArgb(19, 22, 26);
        public static readonly Color PageBg = Color.FromArgb(243, 245, 249);
        public static readonly Color PageBgAlt = Color.FromArgb(236, 239, 245);
        public static readonly Color CardBg = Color.White;
        public static readonly Color CardAlt = Color.FromArgb(251, 252, 253);
        public static readonly Color BorderLight = Color.FromArgb(220, 224, 230);
        public static readonly Color BorderStrong = Color.FromArgb(190, 196, 204);
        public static readonly Color TextPrimary = Color.FromArgb(28, 32, 38);
        public static readonly Color TextMuted = Color.FromArgb(110, 118, 129);
        public static readonly Color TextSubtle = Color.FromArgb(150, 156, 165);
        public static readonly Color Disabled = Color.FromArgb(205, 210, 218);
        public static readonly Color Highlight = Color.FromArgb(230, 243, 255);

        // ═══════════════════════════════════════════════════════════════
        // FONTS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>Font mặc định cho toàn bộ ứng dụng</summary>
        public static readonly Font DefaultFont = new Font("Segoe UI", 10f);

        /// <summary>Font cho tiêu đề form</summary>
        public static readonly Font TitleFont = new Font("Segoe UI", 18f, FontStyle.Bold);

        /// <summary>Font cho tiêu đề section</summary>
        public static readonly Font SectionFont = new Font("Segoe UI", 11f, FontStyle.Bold);

        /// <summary>Font cho nhãn input</summary>
        public static readonly Font LabelFont = new Font("Segoe UI", 10f, FontStyle.Bold);

        /// <summary>Font cho button</summary>
        public static readonly Font ButtonFont = new Font("Segoe UI", 10.5f, FontStyle.Bold);

        /// <summary>Font cho text nhỏ, subtitle</summary>
        public static readonly Font SmallFont = new Font("Segoe UI", 9f);

        // ═══════════════════════════════════════════════════════════════
        // SIZING
        // ═══════════════════════════════════════════════════════════════

        public const int InputHeight = 38;
        public const int ButtonHeight = 46;
        public const int SmallButton = 36;
        public const int CardRadius = 16;
        public const int CardPadding = 24;
        public const int SectionGap = 16;

        // ═══════════════════════════════════════════════════════════════
        // FORM SIZING
        // ═══════════════════════════════════════════════════════════════

        /// <summary>Kích thước mặc định cho dialog forms</summary>
        public static readonly Size DialogSize = new Size(960, 700);

        /// <summary>Kích thước tối thiểu cho dialog forms</summary>
        public static readonly Size DialogMinSize = new Size(760, 600);

        /// <summary>Kích thước mặc định cho main forms</summary>
        public static readonly Size MainFormSize = new Size(1280, 800);

        /// <summary>Kích thước tối thiểu cho main forms</summary>
        public static readonly Size MainFormMinSize = new Size(1024, 768);

        // ═══════════════════════════════════════════════════════════════
        // SPACING
        // ═══════════════════════════════════════════════════════════════

        /// <summary>Đơn vị grid cơ bản (8pt grid system)</summary>
        public const int GridUnit = 8;

        /// <summary>Khoảng cách giữa các control</summary>
        public const int ControlGap = 10;

        /// <summary>Khoảng cách padding trong card</summary>
        public const int CardGap = 12;

        /// <summary>Khoảng cách giữa các section</summary>
        public const int LargeGap = 24;
    }
}


