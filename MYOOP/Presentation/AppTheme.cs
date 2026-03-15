namespace OOP.Presentation
{
    /// <summary>
    /// Tập trung toàn bộ màu sắc và hằng số UI — thay thế các hardcode rải rác trong từng Form.
    /// </summary>
    public static class AppTheme
    {
        // --- Brand colors ---
        public static readonly Color Primary = Color.FromArgb(0, 122, 255);
        public static readonly Color PrimaryHover = Color.FromArgb(0, 100, 220);

        public static readonly Color Success = Color.FromArgb(25, 135, 84);
        public static readonly Color SuccessHover = Color.FromArgb(20, 110, 68);

        public static readonly Color Warning = Color.FromArgb(253, 126, 20);
        public static readonly Color WarningHover = Color.FromArgb(210, 100, 10);

        public static readonly Color Danger = Color.FromArgb(220, 53, 69);
        public static readonly Color DangerHover = Color.FromArgb(185, 30, 46);

        public static readonly Color Purple = Color.FromArgb(111, 66, 193);
        public static readonly Color PurpleHover = Color.FromArgb(88, 44, 160);

        // --- Neutrals ---
        public static readonly Color DarkBg = Color.FromArgb(24, 27, 31);
        public static readonly Color PageBg = Color.FromArgb(245, 247, 250);
        public static readonly Color CardBg = Color.White;
        public static readonly Color BorderLight = Color.FromArgb(220, 220, 220);
        public static readonly Color TextMuted = Color.FromArgb(130, 130, 130);
        public static readonly Color TextSubtle = Color.FromArgb(180, 180, 180);
        public static readonly Color Disabled = Color.FromArgb(200, 200, 200);

        // --- Sizing ---
        public const int InputHeight = 36;
        public const int ButtonHeight = 44;
        public const int SmallButton = 36;
        public const int CardRadius = 14;
    }
}