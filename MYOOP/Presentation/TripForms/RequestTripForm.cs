using GMap.NET;
using OOP.Application.Services.Interfaces;
using OOP.Domain.Enums;
using OOP.Presentation.Map;
using DomainLocation = OOP.Domain.Entities.Location;

namespace OOP.Presentation.TripForms
{
    public class RequestTripForm : Form
    {
        private readonly Guid _passengerId;
        private readonly ITripService _tripService;
        private readonly IRouteService _routeService;
        private readonly IFareRuleService _fareRuleService;
        private readonly HttpClient _http;

        private MapControl _mapControl = null!;
        private TextBox TextBoxPickup = null!;
        private TextBox TextBoxDestination = null!;
        private ComboBox ComboVehicleType = null!;
        private Label LabelDistance = null!;
        private Label LabelEstimatedFare = null!;
        private Button ButtonRequestTrip = null!;
        private Button ButtonBack = null!;

        // FIX: nhận HttpClient từ ngoài thay vì new HttpClient() inline —
        // tránh socket exhaustion khi form được mở nhiều lần.
        public RequestTripForm(
            Guid passengerId,
            ITripService tripService,
            IRouteService routeService,
            IFareRuleService fareRuleService,
            HttpClient http)
        {
            _passengerId = passengerId;
            _tripService = tripService;
            _routeService = routeService;
            _fareRuleService = fareRuleService;
            _http = http;

            InitForm();
            BuildUI();

            // Sau khi UI dựng xong, set sẵn điểm đón = vị trí hiện tại
            this.Load += async (_, _) =>
            {
                await _mapControl.ZoomToMyLocation();
                UpdateRequestButton();
            };
        }

        private void InitForm()
        {
            Text = "RideGo - Đặt chuyến xe";
            Size = new Size(1100, 750);
            StartPosition = FormStartPosition.CenterScreen;
        }

        private void BuildUI()
        {
            // ── Toolbar panel (Dock Top) ──────────────────────────────────────
            var panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 56,
                Padding = new Padding(8, 8, 8, 0),
                BackColor = Color.WhiteSmoke
            };

            TextBoxPickup = new TextBox
            {
                Width = 220,
                PlaceholderText = "Điểm đón...",
                Font = new Font("Segoe UI", 10)
            };
            TextBoxDestination = new TextBox
            {
                Width = 220,
                PlaceholderText = "Điểm đến...",
                Font = new Font("Segoe UI", 10)
            };

            ComboVehicleType = new ComboBox
            {
                Width = 110,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10)
            };
            ComboVehicleType.DataSource = Enum.GetValues(typeof(VehicleType));
            ComboVehicleType.SelectedIndexChanged += async (_, _) => await UpdateEstimation();

            LabelDistance = new Label
            {
                Text = "Cách: --",
                AutoSize = true,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };
            LabelEstimatedFare = new Label
            {
                Text = "Giá: --",
                AutoSize = true,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.DarkGreen,
                TextAlign = ContentAlignment.MiddleLeft
            };

            ButtonRequestTrip = new Button
            {
                Text = "ĐẶT XE",
                Width = 90,
                Height = 34,
                BackColor = Color.FromArgb(25, 135, 84),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Enabled = false   // FIX: disable cho đến khi chọn đủ 2 điểm
            };
            ButtonRequestTrip.FlatAppearance.BorderSize = 0;
            ButtonRequestTrip.Click += async (_, _) => await OnRequestTripClicked();

            ButtonBack = new Button
            {
                Text = "Thoát",
                Width = 70,
                Height = 34,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            ButtonBack.Click += (_, _) => Close();

            panel.Controls.Add(MakeLabel("Từ:"));
            panel.Controls.Add(TextBoxPickup);
            panel.Controls.Add(MakeLabel("Đến:"));
            panel.Controls.Add(TextBoxDestination);
            panel.Controls.Add(ComboVehicleType);
            panel.Controls.Add(LabelDistance);
            panel.Controls.Add(LabelEstimatedFare);
            panel.Controls.Add(ButtonRequestTrip);
            panel.Controls.Add(ButtonBack);

            // ── MapControl ────────────────────────────────────────────────────
            // ── MapControl ────────────────────────────────────────────────────
            _mapControl = new MapControl(_http, _routeService)
            {
                Dock = DockStyle.Fill
            };
            _mapControl.LocationSelected += (point, address) =>
            {
                // Điền lần lượt: click đầu = điểm đón, click sau = điểm đến
                if (string.IsNullOrEmpty(TextBoxPickup.Text))
                    TextBoxPickup.Text = address;
                else
                    TextBoxDestination.Text = address;

                UpdateRequestButton();
                _ = UpdateEstimation();
            };

            // FIX: thứ tự Add quan trọng — panel Dock=Top phải Add trước,
            // map Dock=Fill Add sau để Fill lấp phần còn lại bên dưới panel.
            Controls.Add(_mapControl);  // Fill → thêm trước
            Controls.Add(panel);        // Top  → thêm sau (WinForms tính layout từ cuối lên)

            // ═══════════════════════════════════════════════════════════════════
            // FIX: gọi Show() — đây là bước BẮT BUỘC khi nhúng Form với
            // TopLevel = false. Nếu bỏ qua, form con tồn tại trong bộ nhớ
            // nhưng không render → chỉ thấy nền trắng.
            // ═══════════════════════════════════════════════════════════════════
            _mapControl.Show();
        }

        // ── Logic ─────────────────────────────────────────────────────────────

        private void UpdateRequestButton()
        {
            // Chỉ cần người dùng chọn điểm đến trên bản đồ
            bool ready = _mapControl.DropoffPoint != null;
            ButtonRequestTrip.Enabled = ready;
            ButtonRequestTrip.BackColor = ready
                ? Color.FromArgb(25, 135, 84)
                : Color.FromArgb(180, 180, 180);
        }

        private async Task UpdateEstimation()
        {
            var p1 = _mapControl.PickupPoint;
            var p2 = _mapControl.DropoffPoint;
            if (p2 == null) return;

            try
            {
                var pickup = new DomainLocation("P", "P", p1.Lat, p1.Lng);
                var dest = new DomainLocation("D", "D", p2.Value.Lat, p2.Value.Lng);

                var route = await _routeService.GetFullRouteAsync(pickup, dest);
                if (route == null) return;

                LabelDistance.Text = $"Cách: {route.Distance:F2} km";

                var vehicle = (VehicleType)ComboVehicleType.SelectedItem!;
                var fareRule = await _fareRuleService.GetFareRule(vehicle);
                if (fareRule != null)
                {
                    var estimatedFare = fareRule.CalculateFare(route.Distance);
                    LabelEstimatedFare.Text = $"Giá: {estimatedFare:N0} VNĐ";
                }
            }
            catch { /* estimation là best-effort, không crash nếu lỗi network */ }
        }

        private async Task OnRequestTripClicked()
        {
            var p1 = _mapControl.PickupPoint;
            var p2 = _mapControl.DropoffPoint;

            if (p2 == null)
            {
                MessageBox.Show("Vui lòng chọn điểm đến trên bản đồ!");
                return;
            }

            string pickupAddr = string.IsNullOrWhiteSpace(TextBoxPickup.Text)
                ? $"{p1.Lat:F5},{p1.Lng:F5}" : TextBoxPickup.Text;
            string destAddr = string.IsNullOrWhiteSpace(TextBoxDestination.Text)
                ? $"{p2.Value.Lat:F5},{p2.Value.Lng:F5}" : TextBoxDestination.Text;

            var pickup = new DomainLocation("Pickup", pickupAddr, p1.Lat, p1.Lng);
            var destination = new DomainLocation("Destination", destAddr, p2.Value.Lat, p2.Value.Lng);
            var vehicle = (VehicleType)ComboVehicleType.SelectedItem!;

            ButtonRequestTrip.Enabled = false;
            ButtonRequestTrip.Text = "Đang xử lý...";

            try
            {
                await _tripService.RequestTrip(_passengerId, pickup, destination, vehicle);
                MessageBox.Show("Yêu cầu thành công! Đang tìm tài xế...",
                    "Đặt xe", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Đặt xe thất bại",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                ButtonRequestTrip.Enabled = true;
                ButtonRequestTrip.Text = "ĐẶT XE";
            }
        }

        private static Label MakeLabel(string text) => new Label
        {
            Text = text,
            AutoSize = false,
            Width = 28,
            Height = 34,
            TextAlign = ContentAlignment.MiddleRight,
            Font = new Font("Segoe UI", 9)
        };

        protected override void Dispose(bool disposing)
        {
            // Không dispose _http — lifetime do caller quản lý
            base.Dispose(disposing);
        }
    }
}