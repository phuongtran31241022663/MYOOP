using GMap.NET;
using OOP.Application.Services.Interfaces;
using OOP.Domain.Entities;
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
        private Button _btnCancelTrip = null!;
        private Button ButtonBack = null!;
        private Label _lblTripStatus = null!;
        private Label _lblMapHint = null!;

        private Guid _currentTripId = Guid.Empty;

        private readonly List<DomainLocation> _searchHistory = new();
        private static readonly List<DomainLocation> _fixedLocations = new()
        {
            new DomainLocation("UEH Cơ sở A", "59C Nguyễn Đình Chiểu, Q.3", 10.7826, 106.6954),
            new DomainLocation("UEH Cơ sở B", "279 Nguyễn Tri Phương, Q.10", 10.7679, 106.6707),
            new DomainLocation("UEH Cơ sở N", "78 Nguyễn Văn Thủ, Q.1", 10.7840, 106.6968)
        };

        private enum SuggestionKind { Header, Location }
        private sealed class SuggestionItem
        {
            public SuggestionKind Kind { get; }
            public string Header { get; }
            public DomainLocation? Location { get; }

            public SuggestionItem(string header)
            {
                Kind = SuggestionKind.Header;
                Header = header;
            }

            public SuggestionItem(DomainLocation location)
            {
                Kind = SuggestionKind.Location;
                Location = location;
                Header = "";
            }
        }

        private TextBox _activeTextBox = null!;

        private readonly ListBox _lstGlobalSuggestions = new ListBox
        {
            Visible = false,
            DrawMode = DrawMode.OwnerDrawFixed,
            ItemHeight = 42
        };

        private readonly System.Windows.Forms.Timer _searchDebounceTimer =
            new System.Windows.Forms.Timer { Interval = 500 };

        private readonly System.Windows.Forms.Timer _nearbyTimer =
            new System.Windows.Forms.Timer { Interval = 3000 };

        private readonly System.Windows.Forms.Timer _tripPollTimer =
            new System.Windows.Forms.Timer { Interval = 2000 };

        private const double NearbyRadiusKm = 3.0;

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

            _nearbyTimer.Tick += async (_, _) => await RefreshNearbyDrivers();
            _nearbyTimer.Start();

            _tripPollTimer.Tick += async (_, _) => await RefreshTripOnMap();
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
            var panel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 96,
                Padding = new Padding(12, 8, 12, 8),
                BackColor = AppTheme.CardBg
            };

            var row1 = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 40,
                WrapContents = false,
                AutoSize = false
            };

            var row2 = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 40,
                WrapContents = false,
                AutoSize = false
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

            _lblTripStatus = new Label
            {
                Text = "Trạng thái: --",
                AutoSize = true,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = AppTheme.Primary,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _lblMapHint = new Label
            {
                Text = "Chọn điểm: nhấp chuột phải trên bản đồ",
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = AppTheme.TextMuted,
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

            _btnCancelTrip = new Button
            {
                Text = "HỦY",
                Width = 80,
                Height = 36,
                BackColor = AppTheme.Danger,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Enabled = false
            };
            _btnCancelTrip.FlatAppearance.BorderSize = 0;
            _btnCancelTrip.Click += async (_, _) => await OnCancelTripClicked();

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
                if (_lstGlobalSuggestions.SelectedItem is not SuggestionItem item) return;
                if (item.Kind == SuggestionKind.Header || item.Location == null) return;
                var selected = item.Location;

                string displayText = string.IsNullOrEmpty(selected.Address)
                    ? selected.Name
                    : $"{selected.Name}, {selected.Address}";

                _activeTextBox.Text = displayText;
                _lstGlobalSuggestions.Visible = false;

                bool isPickup = (_activeTextBox == TextBoxPickup);
                await _mapControl.SelectLocation(selected, isPickup);

                AddToHistory(selected);
                await UpdateEstimation();
                UpdateRequestButton();
            };

            Click += (_, _) => _lstGlobalSuggestions.Visible = false;

            row1.Controls.Add(MakeLabel("Từ:"));
            row1.Controls.Add(TextBoxPickup);
            row1.Controls.Add(MakeLabel("Đến:"));
            row1.Controls.Add(TextBoxDestination);
            row1.Controls.Add(ComboVehicleType);

            row2.Controls.Add(LabelDistance);
            row2.Controls.Add(LabelEstimatedFare);
            row2.Controls.Add(_lblTripStatus);
            row2.Controls.Add(_lblMapHint);
            row2.Controls.Add(ButtonRequestTrip);
            row2.Controls.Add(_btnCancelTrip);
            row2.Controls.Add(ButtonBack);

            panel.Controls.Add(row2);
            panel.Controls.Add(row1);

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
            TextBoxPickup.GotFocus += (_, _) => ShowHistorySuggestionsIfEmpty(TextBoxPickup);
            TextBoxDestination.GotFocus += (_, _) => ShowHistorySuggestionsIfEmpty(TextBoxDestination);
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
                ShowHistorySuggestions();
                return;
            }

            var results = await _mapControl.GetSuggestions(query);
            BuildSuggestionList(query, results ?? new List<DomainLocation>());
        }

        private void LstGlobalSuggestions_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            e.DrawBackground();

            if (_lstGlobalSuggestions.Items[e.Index] is not SuggestionItem item) return;

            if (item.Kind == SuggestionKind.Header)
            {
                using var fontHeader = new Font(e.Font!, FontStyle.Bold);
                using var brush = new SolidBrush(Color.FromArgb(90, 90, 90));
                e.Graphics.DrawString(item.Header, fontHeader, brush, e.Bounds.X + 5, e.Bounds.Y + 10);
                e.DrawFocusRectangle();
                return;
            }

            var loc = item.Location!;
            using var fontName = new Font(e.Font!, FontStyle.Bold);
            using var fontAddr = new Font(e.Font!.FontFamily, 8);

            string name = loc.Name ?? "";
            string address = loc.Address ?? "";

            e.Graphics.DrawString(name, fontName, Brushes.Black, e.Bounds.X + 5, e.Bounds.Y + 2);
            e.Graphics.DrawString(address, fontAddr, Brushes.Gray, e.Bounds.X + 5, e.Bounds.Y + 22);

            e.DrawFocusRectangle();
        }

        private void ShowHistorySuggestionsIfEmpty(TextBox tb)
        {
            if (!string.IsNullOrWhiteSpace(tb.Text)) return;
            _activeTextBox = tb;
            ShowHistorySuggestions();
        }

        private void ShowHistorySuggestions()
        {
            _lstGlobalSuggestions.Items.Clear();

            var history = _searchHistory.Take(5).ToList();
            if (history.Count > 0)
            {
                _lstGlobalSuggestions.Items.Add(new SuggestionItem("History"));
                foreach (var h in history) _lstGlobalSuggestions.Items.Add(new SuggestionItem(h));
            }

            if (_fixedLocations.Count > 0)
            {
                _lstGlobalSuggestions.Items.Add(new SuggestionItem("Fixed Locations"));
                foreach (var f in _fixedLocations) _lstGlobalSuggestions.Items.Add(new SuggestionItem(f));
            }

            ShowSuggestionList();
        }

        private void BuildSuggestionList(string query, List<DomainLocation> results)
        {
            _lstGlobalSuggestions.Items.Clear();

            var historyMatches = _searchHistory
                .Where(h => h.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                            h.Address.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Take(5)
                .ToList();

            if (historyMatches.Count > 0)
            {
                _lstGlobalSuggestions.Items.Add(new SuggestionItem("History"));
                foreach (var h in historyMatches) _lstGlobalSuggestions.Items.Add(new SuggestionItem(h));
            }

            if (results.Count > 0)
            {
                var ordered = results
                    .OrderByDescending(r => r.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                    .ThenByDescending(r => r.Address.Contains(query, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                _lstGlobalSuggestions.Items.Add(new SuggestionItem("Search Results"));
                foreach (var r in ordered) _lstGlobalSuggestions.Items.Add(new SuggestionItem(r));
            }

            if (_fixedLocations.Count > 0)
            {
                var fixedMatches = _fixedLocations
                    .Where(f => f.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                f.Address.Contains(query, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (fixedMatches.Count > 0)
                {
                    _lstGlobalSuggestions.Items.Add(new SuggestionItem("Fixed Locations"));
                    foreach (var f in fixedMatches) _lstGlobalSuggestions.Items.Add(new SuggestionItem(f));
                }
            }

            ShowSuggestionList();
        }

        private void ShowSuggestionList()
        {
            if (_lstGlobalSuggestions.Items.Count == 0)
            {
                _lstGlobalSuggestions.Visible = false;
                return;
            }

            Point screenPos = _activeTextBox.Parent.PointToScreen(
                new Point(_activeTextBox.Left, _activeTextBox.Bottom));
            _lstGlobalSuggestions.Location = PointToClient(screenPos);
            _lstGlobalSuggestions.Width = _activeTextBox.Width;
            _lstGlobalSuggestions.Height = Math.Min(_lstGlobalSuggestions.Items.Count * 42, 220);
            _lstGlobalSuggestions.Visible = true;
            _lstGlobalSuggestions.BringToFront();
        }

        private void AddToHistory(DomainLocation location)
        {
            _searchHistory.RemoveAll(h => h.Name == location.Name && h.Address == location.Address);
            _searchHistory.Insert(0, location);
            if (_searchHistory.Count > 20)
                _searchHistory.RemoveAt(_searchHistory.Count - 1);
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

        private async Task RefreshNearbyDrivers()
        {
            try
            {
                if (_mapControl.PickupPoint == default(PointLatLng))
                {
                    _mapControl.UpdateNearbyDrivers(Array.Empty<Driver>());
                    return;
                }

                var pickup = new DomainLocation("Pickup", "Pickup",
                    _mapControl.PickupPoint.Lat, _mapControl.PickupPoint.Lng);

                var vehicle = (VehicleType)ComboVehicleType.SelectedItem!;
                var drivers = await _tripService.GetNearbyDrivers(pickup, vehicle, NearbyRadiusKm);
                _mapControl.UpdateNearbyDrivers(drivers);
            }
            catch { /* no-op */ }
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
                var trip = await _tripService.RequestTrip(_passengerId, pickup, destination, vehicle);
                _currentTripId = trip.Id;
                _lblTripStatus.Text = "Trạng thái: Đang tìm tài xế...";

                // Lock inputs while tracking
                TextBoxPickup.Enabled = false;
                TextBoxDestination.Enabled = false;
                ComboVehicleType.Enabled = false;
                ButtonRequestTrip.Enabled = false;
                ButtonRequestTrip.Text = "Đã đặt";
                _btnCancelTrip.Enabled = true;

                _nearbyTimer.Stop();
                _tripPollTimer.Start();

                MessageBox.Show("Yêu cầu thành công! Đang tìm tài xế...",
                    "Đặt xe", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Đặt xe thất bại",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                ButtonRequestTrip.Enabled = true;
                ButtonRequestTrip.Text = "ĐẶT XE";
            }
        }

        private async Task OnCancelTripClicked()
        {
            if (_currentTripId == Guid.Empty) return;

            if (MessageBox.Show("Bạn có chắc muốn hủy chuyến đi?",
                "Xác nhận hủy", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            try
            {
                await _tripService.CancelTrip(_currentTripId, "Hành khách tự hủy");
                _lblTripStatus.Text = "Trạng thái: Đã hủy";
                _btnCancelTrip.Enabled = false;
                _tripPollTimer.Stop();
                ResetForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể hủy: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task RefreshTripOnMap()
        {
            if (_currentTripId == Guid.Empty) return;

            try
            {
                var trip = await _tripService.GetTrip(_currentTripId);
                if (trip == null) return;

                _lblTripStatus.Text = $"Trạng thái: {StatusLabel(trip.Status)}";

                var pickupPoint = new PointLatLng(trip.PickupLocation.Lat, trip.PickupLocation.Lng);
                var destPoint = new PointLatLng(trip.DestinationLocation.Lat, trip.DestinationLocation.Lng);
                _mapControl.SetPickupMarker(pickupPoint);
                await _mapControl.SetDropoffMarker(destPoint);

                var driver = await _tripService.GetDriverForTrip(_currentTripId);
                if (driver != null)
                {
                    _mapControl.UpdateDriverLocation(driver.Id,
                        new PointLatLng(driver.Position.Lat, driver.Position.Lng));

                    if (trip.Status == TripStatus.Matched || trip.Status == TripStatus.Arrived)
                        await _mapControl.DrawDriverToPickupRouteAsync(
                            new PointLatLng(driver.Position.Lat, driver.Position.Lng),
                            pickupPoint);
                    else if (trip.Status == TripStatus.Started || trip.Status == TripStatus.Completed)
                        await _mapControl.DrawTripRouteAsync(pickupPoint, destPoint);
                }

                if (trip.Status == TripStatus.Completed || trip.Status == TripStatus.Cancelled || trip.Status == TripStatus.Timeout)
                {
                    _tripPollTimer.Stop();
                    _lblTripStatus.Text = $"Trạng thái: {StatusLabel(trip.Status)}";
                    _btnCancelTrip.Enabled = false;
                    if (trip.Status == TripStatus.Timeout)
                    {
                        MessageBox.Show("Không có tài xế nhận chuyến. Yêu cầu đã hết thời gian.",
                            "Hết thời gian", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    ResetForm();
                }
            }
            catch { /* swallow */ }
        }

        private void ResetForm()
        {
            _currentTripId = Guid.Empty;
            TextBoxPickup.Enabled = true;
            TextBoxDestination.Enabled = true;
            ComboVehicleType.Enabled = true;
            ButtonRequestTrip.Enabled = true;
            ButtonRequestTrip.Text = "ĐẶT XE";
            _btnCancelTrip.Enabled = false;
            _lblTripStatus.Text = "Trạng thái: --";

            _mapControl.ClearRoute();
            TextBoxPickup.Text = "";
            TextBoxDestination.Text = "";
            _nearbyTimer.Start();
        }

        private static string StatusLabel(TripStatus status) => status switch
        {
            TripStatus.Requested => "⏳ Đang tìm tài xế",
            TripStatus.Searching => "🔎 Đang tìm tài xế",
            TripStatus.Matched => "🤝 Đã ghép tài xế",
            TripStatus.Arrived => "📍 Tài xế đã đến",
            TripStatus.Started => "🚗 Đang di chuyển",
            TripStatus.Completed => "✅ Hoàn thành",
            TripStatus.Cancelled => "❌ Đã hủy",
            TripStatus.Timeout => "⌛ Hết thời gian",
            _ => status.ToString()
        };

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
                _nearbyTimer.Dispose();
                _tripPollTimer.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}

