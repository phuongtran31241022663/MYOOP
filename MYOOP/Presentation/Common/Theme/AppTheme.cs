namespace OOP.Presentation.Common.Theme
{
    public static class AppTheme
    {
        // Window sizing (standard dashboard)
        public static readonly Size StandardSize = new(1200, 800);
        public static readonly Size StandardMinSize = new(1000, 700);

        // ── Brand colors ──────────────────────────────────────────────────────

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

        // ── Neutral / Background ──────────────────────────────────────────────

        /// <summary>Nền trang chính (light gray)</summary>
        public static readonly Color PageBg = Color.FromArgb(243, 245, 249);

        /// <summary>Card / panel nền trắng</summary>
        public static readonly Color CardBg = Color.White;

        /// <summary>Card phụ, xen kẽ</summary>
        public static readonly Color CardAlt = Color.FromArgb(251, 252, 253);

        /// <summary>Nền tối (DarkBg header, info block)</summary>
        public static readonly Color DarkBg = Color.FromArgb(33, 37, 41);

        /// <summary>Sidebar bên trái của dashboard</summary>
        public static readonly Color SidebarBg = Color.FromArgb(30, 39, 56);

        /// <summary>Sidebar header — tối hơn SidebarBg</summary>
        public static readonly Color SidebarDark = Color.FromArgb(22, 28, 42);

        /// <summary>Stat card bên trong sidebar (nổi nhẹ trên SidebarBg)</summary>
        public static readonly Color SidebarCard = Color.FromArgb(45, 57, 80);

        /// <summary>Hover trên sidebar</summary>
        public static readonly Color SidebarHover = Color.FromArgb(55, 70, 95);

        // ── Borders ───────────────────────────────────────────────────────────

        public static readonly Color BorderLight = Color.FromArgb(220, 224, 230);
        public static readonly Color BorderStrong = Color.FromArgb(190, 196, 204);

        // ── Text ──────────────────────────────────────────────────────────────

        public static readonly Color TextPrimary = Color.FromArgb(28, 32, 38);
        public static readonly Color TextMuted = Color.FromArgb(110, 118, 129);
        public static readonly Color TextSubtle = Color.FromArgb(150, 156, 165);

        // ── State / misc ──────────────────────────────────────────────────────

        /// <summary>Input/button khi Disabled</summary>
        public static readonly Color Disabled = Color.FromArgb(205, 210, 218);

        /// <summary>Highlight nhạt (hover card, selected row)</summary>
        public static readonly Color Highlight = Color.FromArgb(230, 243, 255);

        /// <summary>Highlight xanh lá (matched/driver info)</summary>
        public static readonly Color HighlightGreen = Color.FromArgb(232, 245, 233);

        /// <summary>Highlight vàng (proximity / warning soft)</summary>
        public static readonly Color HighlightYellow = Color.FromArgb(255, 243, 205);

        /// <summary>Màu chữ khi dùng trên nền Primary/Danger/Success.</summary>
        public static readonly Color OnDark = Color.White;

        // ── Fonts ─────────────────────────────────────────────────────────────
        // NOTE: Các Font static này tồn tại suốt vòng đời app — không Dispose.
        // Dùng FontOf() khi cần font tạm thời (caller tự Dispose).

        public static readonly Font DefaultFont = new("Segoe UI", 10f);
        public static readonly Font TitleFont = new("Segoe UI", 18f, FontStyle.Bold);
        public static readonly Font SectionFont = new("Segoe UI", 11f, FontStyle.Bold);
        public static readonly Font LabelFont = new("Segoe UI", 10f, FontStyle.Bold);
        public static readonly Font ButtonFont = new("Segoe UI", 10.5f, FontStyle.Bold);
        public static readonly Font SmallFont = new("Segoe UI", 9f);
        public static readonly Font CaptionFont = new("Segoe UI", 7.5f, FontStyle.Bold);

        /// <summary>Tạo Font tùy chỉnh — caller chịu trách nhiệm Dispose.</summary>
        public static Font FontOf(float size, FontStyle style = FontStyle.Regular) =>
            new("Segoe UI", size, style);

        // ── Sizing — controls & inputs ────────────────────────────────────────

        public const int InputHeight = 38;
        public const int ButtonHeight = 46;
        public const int SmallButton = 36;
        public const int RowHeight = 30;
        public const int HeaderRowH = 36;

        // ── Sizing — forms ────────────────────────────────────────────────────

        public static readonly Size DialogSize = new(960, 700);
        public static readonly Size DialogMinSize = new(760, 640);

        // ── Layout ───────────────────────────────────────────────────────────

        public const int HeaderHeight = 56;
        public const int SidebarWidth = 200;

        // ── Spacing ───────────────────────────────────────────────────────────

        /// <summary>Đơn vị grid cơ bản (8-pt grid)</summary>
        public const int GridUnit = 8;

        /// <summary>Khoảng cách nhỏ giữa các control liên quan</summary>
        public const int ControlGap = 10;

        /// <summary>Padding bên trong card</summary>
        public const int CardGap = 12;

        /// <summary>Khoảng cách giữa các section lớn</summary>
        public const int LargeGap = 24;

        // ── Shape ─────────────────────────────────────────────────────────────

        public const int CardRadius = 12;
        public const int CardPadding = 20;
        public const int SectionGap = 16;

        // ── Convenience ───────────────────────────────────────────────────────

        /// <summary>Map TripStatus / DriverStatus string → Color (dùng chung cho card & indicator).</summary>
        public static Color StatusColor(string status) => status switch
        {
            "Active" => Success,
            "OnTrip" => Warning,
            "Inactive" => TextSubtle,
            "Requested" => Warning,
            "Searching" => Accent,
            "Matched" => Primary,
            "Arrived" => Primary,
            "Started" => Success,
            "Completed" => Success,
            "Cancelled" => Danger,
            "Timeout" => Danger,
            _ => TextMuted
        };
    }
}
