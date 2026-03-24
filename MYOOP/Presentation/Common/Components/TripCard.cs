using OOP.Domain.Entities;
using OOP.Domain.Enums;
using System.Windows.Forms;
using System.Drawing;
using OOP.Presentation.Common.Theme;

namespace OOP.Presentation.Common.Components
{
    /// <summary>
    /// UserControl hiển thị thông tin chuyến đi với style nhất quán.
    /// Dùng với FlowLayoutPanel để hiển thị danh sách chuyến.
    /// </summary>
    public partial class TripCard : UserControl
    {
        private readonly Label _lblStatus;
        private readonly Label _lblRoute;
        private readonly Label _lblInfo;
        private readonly Label _lblTime;
        private readonly Panel _statusIndicator;

        /// <summary>
        /// Sự kiện khi người dùng click vào trip card
        /// </summary>
        public event System.Action<TripCard>? Clicked;

        /// <summary>
        /// Trip được hiển thị
        /// </summary>
        public Trip? Trip { get; private set; }

        public TripCard()
        {
            // Status indicator (colored bar)
            _statusIndicator = new Panel
            {
                Width = 4,
                Height = 70,
                Location = new Point(0, 0),
                BackColor = AppTheme.Primary
            };

            // Status label
            _lblStatus = new Label
            {
                Font = AppTheme.LabelFont,
                ForeColor = AppTheme.TextPrimary,
                Location = new Point(16, 8),
                AutoSize = true,
                Text = "Chờ xử lý"
            };

            // Route label
            _lblRoute = new Label
            {
                Font = AppTheme.DefaultFont,
                ForeColor = AppTheme.TextPrimary,
                Location = new Point(16, 30),
                AutoSize = false,
                Width = 320,
                Height = 18,
                Text = "Điểm đón → Điểm đến"
            };

            // Info label (fare, vehicle type)
            _lblInfo = new Label
            {
                Font = AppTheme.SmallFont,
                ForeColor = AppTheme.TextMuted,
                Location = new Point(16, 52),
                AutoSize = true,
                Text = "15.000 VNĐ • Xe máy"
            };

            // Time label
            _lblTime = new Label
            {
                Font = new Font("Segoe UI", 8),
                ForeColor = AppTheme.TextSubtle,
                Location = new Point(250, 52),
                AutoSize = true,
                Text = "10:30"
            };

            // Main container
            this.Size = new Size(360, 70);
            this.BackColor = AppTheme.CardBg;
            this.Padding = new Padding(0);
            this.Margin = new Padding(0, 0, 0, AppTheme.ControlGap);

            this.Controls.Add(_statusIndicator);
            this.Controls.Add(_lblStatus);
            this.Controls.Add(_lblRoute);
            this.Controls.Add(_lblInfo);
            this.Controls.Add(_lblTime);

            // Enable click events
            this.Cursor = Cursors.Hand;
            this.Click += OnCardClick;
            foreach (Control ctrl in this.Controls)
            {
                ctrl.Click += OnCardClick;
                ctrl.Cursor = Cursors.Hand;
            }

            // Hover effect
            this.MouseEnter += OnMouseEnter;
            this.MouseLeave += OnMouseLeave;
        }

        /// <summary>
        /// Set thông tin trip từ Trip object
        /// </summary>
        public void SetTrip(Trip trip)
        {
            Trip = trip;

            // Status
            _lblStatus.Text = GetStatusText(trip.Status);
            _statusIndicator.BackColor = GetStatusColor(trip.Status);

            // Route
            _lblRoute.Text = $"{trip.Pickup.Name} → {trip.Destination.Name}";

            // Info
            string vehicleType = trip.VehicleType == "Motorbike" ? "Xe máy" : "Ô tô";
            string fare = $"{trip.Fare:N0} VNĐ";
            _lblInfo.Text = $"{fare} • {vehicleType}";

            // Time
            _lblTime.Text = trip.RequestedAt.ToString("HH:mm");
        }

        private static string GetStatusText(TripStatus status) => status switch
        {
            TripStatus.Requested => "Chờ tài xế",
            TripStatus.Searching => "Đang tìm tài xế",
            TripStatus.Matched => "Đã ghép",
            TripStatus.Arrived => "Tài xế đến",
            TripStatus.Started => "Đang chạy",
            TripStatus.Completed => "Hoàn thành",
            TripStatus.Cancelled => "Đã hủy",
            TripStatus.Timeout => "Hết thời gian",
            _ => "Không xác định"
        };

        private static Color GetStatusColor(TripStatus status) => status switch
        {
            TripStatus.Requested => AppTheme.Warning,
            TripStatus.Searching => AppTheme.Accent,
            TripStatus.Matched => AppTheme.Primary,
            TripStatus.Arrived => AppTheme.Primary,
            TripStatus.Started => AppTheme.Primary,
            TripStatus.Completed => AppTheme.Success,
            TripStatus.Cancelled => AppTheme.Danger,
            TripStatus.Timeout => AppTheme.Danger,
            _ => AppTheme.TextMuted
        };

        private void OnCardClick(object? sender, System.EventArgs e)
        {
            Clicked?.Invoke(this);
        }

        private void OnMouseEnter(object? sender, System.EventArgs e)
        {
            this.BackColor = AppTheme.Highlight;
        }

        private void OnMouseLeave(object? sender, System.EventArgs e)
        {
            this.BackColor = AppTheme.CardBg;
        }
    }
}
