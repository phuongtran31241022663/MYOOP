using GMap.NET;
using OOP.Application.Services.Interfaces;
using OOP.Domain.Enums;
using OOP.Infrastructure.Map;
using OOP.Presentation;
using OOP.Presentation.Map;
using DomainLocation = OOP.Domain.Entities.Location;

namespace OOP.Presentation.TripForms
{
    public class RequestTripForm : Form
    {
        private readonly Guid _passengerId;
        private readonly ITripService _tripService;
        private readonly IRouteService _routeService;
        private readonly IFareService _fareRuleService;
        private readonly HttpClient _http;

        private MapControl _mapControl = null!;
        private TextBox TextBoxPickup = null!;
        private TextBox TextBoxDestination = null!;
        private ComboBox ComboVehicleType = null!;
        private Label LabelDistance = null!;
        private Label LabelEstimatedFare = null!;
        private Button ButtonRequestTrip = null!;
        private Button ButtonBack = null!;

        private TextBox _activeTextBox = null!;

        private readonly ListBox _lstGlobalSuggestions = new ListBox
        {
            Visible = false,
            DrawMode = DrawMode.OwnerDrawFixed,
            ItemHeight = 45
        };

        private readonly System.Windows.Forms.Timer _searchDebounceTimer =
            new System.Windows.Forms.Timer { Interval = 500 };

        public RequestTripForm(
            Guid passengerId,
            ITripService tripService,
            IRouteService routeService,
            IFareService fareRuleService,
            HttpClient http)
        {
            _passengerId = passengerId;
            _tripService = tripService;
            _routeService = routeService;
            _fareRuleService = fareRuleService;
            _http = http;

            InitForm();
            BuildUI();

            Load += async (_, _) =>
            {
                await _mapControl.ZoomToMyLocation();
                _mapControl.ClearRoute();
                TextBoxPickup.Text = "";
                TextBoxDestination.Text = "";
                UpdateRequestButton();
            };
        }

        private void InitForm()
        {
            Text = "RideGo - Đặt chuyến xe";
            Size = new Size(1100, 750);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = AppTheme.PageBg;
            Font = new Font("Segoe UI", 10f);
        }

        private void BuildUI()
        {
            var panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 60,
                Padding = new Padding(12, 10, 12, 0),
                BackColor = AppTheme.CardBg
            };

            TextBoxPickup = new TextBox
            {
                Width = 230,
                PlaceholderText = "Điểm đón...",
                Font = new Font("Segoe UI", 10)
            };
            TextBoxDestination = new TextBox
            {
                Width = 230,
                PlaceholderText = "Điểm đến...",
                Font = new Font("Segoe UI", 10)
            };

            ComboVehicleType = new ComboBox
            {
                Width = 130,
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
                ForeColor = AppTheme.Success,
                TextAlign = ContentAlignment.MiddleLeft
            };

            ButtonRequestTrip = new Button
            {
                Text = "ĐẶT XE",
                Width = 100,
                Height = 36,
                BackColor = AppTheme.Success,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Enabled = false
            };
            ButtonRequestTrip.FlatAppearance.BorderSize = 0;
            ButtonRequestTrip.Click += async (_, _) => await OnRequestTripClicked();

            ButtonBack = new Button
            {
                Text = "Thoát",
                Width = 80,
                Height = 36,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            ButtonBack.Click += (_, _) => Close();

            _searchDebounceTimer.Tick += async (_, _) =>
            {
                _searchDebounceTimer.Stop();
                await ExecuteSearch();
            };

            TextBoxPickup.TextChanged += (_, _) => RestartSearchTimer(TextBoxPickup);
            TextBoxDestination.TextChanged += (_, _) => RestartSearchTimer(TextBoxDestination);

            _lstGlobalSuggestions.DrawItem += LstGlobalSuggestions_DrawItem;

            _lstGlobalSuggestions.MouseClick += async (_, _) =>
            {
                if (_lstGlobalSuggestions.SelectedItem is not DomainLocation selected) return;

                string displayText = string.IsNullOrEmpty(selected.Address)
                    ? selected.Name
                    : $"{selected.Name}, {selected.Address}";

                _activeTextBox.Text = displayText;
                _lstGlobalSuggestions.Visible = false;

                bool isPickup = (_activeTextBox == TextBoxPickup);
                await _mapControl.SelectLocation(selected, isPickup);

                await UpdateEstimation();
                UpdateRequestButton();
            };

            Click += (_, _) => _lstGlobalSuggestions.Visible = false;

            panel.Controls.Add(MakeLabel("Từ:"));
            panel.Controls.Add(TextBoxPickup);
            panel.Controls.Add(MakeLabel("Đến:"));
            panel.Controls.Add(TextBoxDestination);
            panel.Controls.Add(ComboVehicleType);
            panel.Controls.Add(LabelDistance);
            panel.Controls.Add(LabelEstimatedFare);
            panel.Controls.Add(ButtonRequestTrip);
            panel.Controls.Add(ButtonBack);

            _mapControl = new MapControl(_http, _routeService) { Dock = DockStyle.Fill };
            _mapControl.SetPickupSelector(() => _activeTextBox == TextBoxPickup || _activeTextBox == null);
            _mapControl.LocationSelected += (point, address, isPickup) =>
            {
                bool usePickup = _activeTextBox == TextBoxPickup || _activeTextBox == null;
                if (usePickup) TextBoxPickup.Text = address;
                else TextBoxDestination.Text = address;

                UpdateRequestButton();
                _ = UpdateEstimation();
            };

            Controls.Add(_lstGlobalSuggestions);
            _lstGlobalSuggestions.BringToFront();

            Controls.Add(_mapControl);
            Controls.Add(panel);
            _mapControl.Show();

            TextBoxPickup.GotFocus += (_, _) => _activeTextBox = TextBoxPickup;
            TextBoxDestination.GotFocus += (_, _) => _activeTextBox = TextBoxDestination;
        }

        private void RestartSearchTimer(TextBox tb)
        {
            _activeTextBox = tb;
            _searchDebounceTimer.Stop();
            _searchDebounceTimer.Start();
        }

        private async Task ExecuteSearch()
        {
            string query = _activeTextBox?.Text ?? "";
            if (query.Length < 3)
            {
                _lstGlobalSuggestions.Visible = false;
                return;
            }

            var results = await _mapControl.GetSuggestions(query);
            if (results != null && results.Count > 0)
            {
                _lstGlobalSuggestions.Items.Clear();
                foreach (var item in results) _lstGlobalSuggestions.Items.Add(item);

                Point screenPos = _activeTextBox.Parent.PointToScreen(
                    new Point(_activeTextBox.Left, _activeTextBox.Bottom));
                _lstGlobalSuggestions.Location = PointToClient(screenPos);
                _lstGlobalSuggestions.Width = _activeTextBox.Width;
                _lstGlobalSuggestions.Height = Math.Min(results.Count * 45, 180);
                _lstGlobalSuggestions.Visible = true;
                _lstGlobalSuggestions.BringToFront();
            }
            else
            {
                _lstGlobalSuggestions.Visible = false;
            }
        }

        private void LstGlobalSuggestions_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            e.DrawBackground();

            var item = (DomainLocation)_lstGlobalSuggestions.Items[e.Index];

            using var fontName = new Font(e.Font!, FontStyle.Bold);
            using var fontAddr = new Font(e.Font!.FontFamily, 8);

            string name = item.Name ?? "";
            string address = item.Address ?? "";

            e.Graphics.DrawString(name, fontName, Brushes.Black, e.Bounds.X + 5, e.Bounds.Y + 2);
            e.Graphics.DrawString(address, fontAddr, Brushes.Gray, e.Bounds.X + 5, e.Bounds.Y + 22);

            e.DrawFocusRectangle();
        }

        private void UpdateRequestButton()
        {
            bool hasPickup = _mapControl.PickupPoint != default(PointLatLng);
            bool hasDropoff = _mapControl.DropoffPoint != null;

            ButtonRequestTrip.Enabled = hasPickup && hasDropoff;
            ButtonRequestTrip.BackColor = ButtonRequestTrip.Enabled
                ? AppTheme.Success
                : AppTheme.Disabled;
        }

        private async Task UpdateEstimation()
        {
            PointLatLng p1 = _mapControl.PickupPoint;
            PointLatLng? p2 = _mapControl.DropoffPoint;

            if (p2 == null)
            {
                LabelDistance.Text = "Cách: --";
                LabelEstimatedFare.Text = "Giá: --";
                return;
            }

            try
            {
                var pickup = new DomainLocation("Pickup", "Pickup", p1.Lat, p1.Lng);
                var dest = new DomainLocation("Destination", "Destination", p2.Value.Lat, p2.Value.Lng);

                MapRouteResult route = await _routeService.GetFullRouteAsync(pickup, dest);
                if (route == null)
                {
                    LabelDistance.Text = "Cách: Lỗi tính toán";
                    LabelEstimatedFare.Text = "Giá: Không tính được";
                    return;
                }

                LabelDistance.Text = $"Cách: {route.Distance:F2} km";

                VehicleType vehicle = (VehicleType)ComboVehicleType.SelectedItem!;
                var fareRule = await _fareRuleService.GetFareRule(vehicle);
                if (fareRule != null)
                {
                    decimal fare = fareRule.CalculateFare(route.Distance);
                    LabelEstimatedFare.Text = $"Giá: {fare:N0} VNĐ";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateEstimation] {ex}");
                LabelDistance.Text = "Cách: Lỗi";
                LabelEstimatedFare.Text = "Giá: Lỗi";
            }
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
            if (disposing)
            {
                _searchDebounceTimer.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}

