using OOP.Domain.Entities;
using System.Windows.Forms;
using System.Drawing;
using OOP.Presentation.Common.Theme;

namespace OOP.Presentation.Controls
{
    /// <summary>
    /// UserControl hiển thị thông tin địa điểm với style nhất quán.
    /// Thay thế việc dùng TextBox để hiển thị thông tin địa điểm.
    /// </summary>
    public partial class LocationCard : UserControl
    {
        private readonly Label _lblName;
        private readonly Label _lblAddress;
        private readonly Label _lblCoords;
        private readonly Panel _iconPanel;

        /// <summary>
        /// Sự kiện khi người dùng click vào location card
        /// </summary>
        public event System.Action<LocationCard>? Clicked;

        /// <summary>
        /// Địa điểm được hiển thị
        /// </summary>
        public GeoLocation? GeoLocation { get; private set; }

        public LocationCard()
        {
            // Icon panel
            _iconPanel = new Panel
            {
                Width = 40,
                Height = 40,
                BackColor = AppTheme.Highlight,
                Location = new Point(12, 12)
            };
            var iconLabel = new Label
            {
                Text = "📍",
                Font = new Font("Segoe UI", 16),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };
            _iconPanel.Controls.Add(iconLabel);

            // Name label
            _lblName = new Label
            {
                Font = AppTheme.SectionFont,
                ForeColor = AppTheme.TextPrimary,
                AutoSize = false,
                Location = new Point(60, 10),
                Width = 200,
                Height = 20,
                Text = "Tên địa điểm"
            };

            // Address label
            _lblAddress = new Label
            {
                Font = AppTheme.SmallFont,
                ForeColor = AppTheme.TextMuted,
                AutoSize = false,
                Location = new Point(60, 32),
                Width = 280,
                Height = 16,
                Text = "Địa chỉ chi tiết"
            };

            // Coordinates label
            _lblCoords = new Label
            {
                Font = new Font("Segoe UI", 8),
                ForeColor = AppTheme.TextSubtle,
                AutoSize = false,
                Location = new Point(60, 50),
                Width = 100,
                Height = 14,
                Text = ""
            };

            // Main container
            this.Size = new Size(360, 64);
            this.BackColor = AppTheme.CardBg;
            this.Controls.Add(_iconPanel);
            this.Controls.Add(_lblName);
            this.Controls.Add(_lblAddress);
            this.Controls.Add(_lblCoords);

            // Border
            this.Padding = new Padding(0);
            this.Margin = new Padding(0, 0, 0, AppTheme.ControlGap);

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
        /// Set thông tin địa điểm từ GeoLocation object
        /// </summary>
        public void SetLocation(GeoLocation location)
        {
            GeoLocation = location;
            _lblName.Text = location.Name;
            _lblAddress.Text = location.Address;
            _lblCoords.Text = $"{location.Lat:F5}, {location.Lng:F5}";
        }

        /// <summary>
        /// Set thông tin địa điểm từ các tham số riêng
        /// </summary>
        public void SetLocation(string name, string address, double lat, double lng)
        {
            GeoLocation = new GeoLocation(name, address, lat, lng);
            _lblName.Text = name;
            _lblAddress.Text = address;
            _lblCoords.Text = $"{lat:F5}, {lng:F5}";
        }

        /// <summary>
        /// Set icon cho card (pickup/destination marker)
        /// </summary>
        public void SetIcon(string emoji)
        {
            if (_iconPanel.Controls[0] is Label iconLabel)
            {
                iconLabel.Text = emoji;
            }
        }

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
