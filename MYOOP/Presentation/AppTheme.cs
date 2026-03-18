﻿namespace OOP.Presentation
{
    /// <summary>
    /// Tập trung toàn bộ màu sắc và hằng số UI — thay thế các hardcode rải rác trong từng Form.
    /// </summary>
    public static class AppTheme
    {
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

        // --- Sizing ---
        public const int InputHeight = 38;
        public const int ButtonHeight = 46;
        public const int SmallButton = 36;
        public const int CardRadius = 16;
        public const int CardPadding = 24;
        public const int SectionGap = 16;
    }
}


