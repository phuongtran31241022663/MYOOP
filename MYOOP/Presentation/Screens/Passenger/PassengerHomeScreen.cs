using OOP.Application.Services.Interfaces;
using OOP.Domain.Entities;
using OOP.Domain.Enums;
using OOP.Presentation.Common.MapComponent;
using OOP.Presentation.Common.Components;
using OOP.Presentation.Common.Theme;
using OOP.Presentation.Base;
using DomainLocation = OOP.Domain.Entities.GeoLocation;
using DriverEntity = OOP.Domain.Entities.Driver;

namespace OOP.Presentation.Screens.Passenger
{
    /// <summary>
    /// Màn hình chủ của Passenger: bản đồ + chọn địa điểm + đặt chuyến.
    /// Port từ RequestTripForm — không còn là Form riêng biệt.
    ///
    /// Khác biệt then chốt so với RequestTripForm:
    ///   - Không Close() sau khi đặt → gọi _shell.OnTripStarted(trip)
    ///   - Không tự poll sau khi đặt → Shell.PollTripStatus() lo việc đó
    ///   - OnNavigatedTo() reset UI nếu không có chuyến đang dở
    /// </summary>
    public class PassengerHomeScreen : UserControl, IScreen
    {
         // ── Dependencies ──────────────────────────────────────────────────────
        private readonly PassengerShell _shell;
        private readonly ITripService _tripService;
        private readonly IUserService _userService;
        private readonly IFareService _fareService;
         private readonly IRouteService _routeService;
         private readonly HttpClient _http;

         // ── Constructor ───────────────────────────────────────────────────────
        public PassengerHomeScreen(
            PassengerShell shell,
            ITripService tripService,
            IUserService userService,
            HttpClient http,
            IRouteService routeService,
            IFareService fareService)
        {
            _shell = shell ?? throw new ArgumentNullException(nameof(shell));
            _tripService = tripService ?? throw new ArgumentNullException(nameof(tripService));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _routeService = routeService ?? throw new ArgumentNullException(nameof(routeService));
            _fareService = fareService ?? throw new ArgumentNullException(nameof(fareService));

             DoubleBuffered = true; // Reduces flicker when repainting cards

             BuildLayout();

             _nearbyTimer.Tick += async (_, _) => await RefreshNearby();
         }

        // ── Map ───────────────────────────────────────────────────────────────
        private MapControl _mapControl = null!;

        // ── Sidebar controls ──────────────────────────────────────────────────
        private LocationPickerControl _locationPicker = null!;
        private Panel _pnlMotorbike = null!;
        private Panel _pnlCar = null!;
        private Panel _pnlFareCard = null!;
        private Label _lblDistance = null!;
        private Label _lblFare = null!;
        private Panel _pnlStatusBar = null!;
        private Label _lblTripStatus = null!;
        private Panel _pnlDriverInfo = null!;
        private Label _lblDriverInfo = null!;
        private Button _btnRequest = null!;
        private Button _btnCancel = null!;

        // ── Suggestion dropdown ───────────────────────────────────────────────
        private readonly ListBox _lstSuggestions = new()
        {
            Visible = false,
            DrawMode = DrawMode.OwnerDrawFixed,
            ItemHeight = 48,
            BorderStyle = BorderStyle.FixedSingle
        };
        private bool _isPickupSlotActive = true;

        // ── Timer ─────────────────────────────────────────────────────────────
        private readonly System.Windows.Forms.Timer _nearbyTimer = new() { Interval = 4000 };

        // ── State ─────────────────────────────────────────────────────────────
        private string _selectedVehicle = "Motorbike";
        private bool _isRequesting = false;

        private readonly List<DomainLocation> _history = new();
        private static readonly List<DomainLocation> _fixedLocations = new()
        {
            new("UEH Cơ sở A", "59C Nguyễn Đình Chiểu, Quận 3",  10.7826, 106.6954),
            new("UEH Cơ sở B", "279 Nguyễn Tri Phương, Quận 10", 10.7679, 106.6707),
            new("UEH Cơ sở N", "Nguyễn Văn Linh, Bình Chánh",    10.7132, 106.6655),
        };

        // ── Suggestion model ──────────────────────────────────────────────────
        private enum SugKind { Header, Location }
        private sealed class SugItem
        {
            public SugKind Kind { get; }
            public string Header { get; }
            public DomainLocation? Location { get; }
            public SugItem(string h) { Kind = SugKind.Header; Header = h; Location = null; }
            public SugItem(DomainLocation loc) { Kind = SugKind.Location; Location = loc; Header = ""; }
        }

        // ── IScreen ───────────────────────────────────────────────────────────
        public string ScreenTitle => "Trang chủ";

        public async Task OnNavigatedTo(object? parameter = null)
        {
            // Nếu đang có chuyến active → chỉ update trạng thái nút, không reset
            if (_shell.CurrentTrip != null)
            {
                ApplyTripUpdate(_shell.CurrentTrip);
                return;
            }

            // Không có chuyến → reset về idle
            if (_isRequesting) ResetToIdle(null);
            _nearbyTimer.Start();
            await _mapControl.ZoomToMyLocation();
        }

        public bool OnNavigatingFrom() => true;

        // ─────────────────────────────────────────────────────────────────────
        // ── Build UI ──────────────────────────────────────────────────────────

        private void BuildLayout()
        {
            _mapControl = new MapControl(_http, _routeService) { Dock = DockStyle.Fill };
            _mapControl.SetPickupSelector(() => _isPickupSlotActive);
            _mapControl.LocationSelected += async (point, address, _) =>
            {
                string name = address.Contains(',')
                    ? address[..address.IndexOf(',')].Trim()
                    : address.Trim();
                string displayAddress = address.StartsWith(name + ",")
                    ? address[(name.Length + 1)..].Trim()
                    : address;
                var loc = new DomainLocation(name, displayAddress, point.Lat, point.Lng);
                await ApplyLocation(loc, _isPickupSlotActive);
            };

            var sidebar = BuildSidebar();

            _lstSuggestions.BackColor = Color.White;
            _lstSuggestions.DrawItem += OnDrawSuggestion;
            _lstSuggestions.MouseClick += OnSuggestionClicked;
            Click += (_, _) => _lstSuggestions.Visible = false;

            Controls.Add(_mapControl);
            Controls.Add(sidebar);
            Controls.Add(_lstSuggestions);
            _lstSuggestions.BringToFront();
        }

        private Panel BuildSidebar()
        {
            var sidebar = new Panel { Dock = DockStyle.Left, Width = 380, BackColor = Color.White };

            // Header
            var header = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = AppTheme.Primary };
            var lblTitle = new Label
            {
                Text = "Đặt chuyến xe",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(20, 0, 0, 0)
            };
            header.Controls.Add(lblTitle);

            // Location picker
            var pnlLoc = new Panel { Dock = DockStyle.Top, Height = 174, BackColor = Color.White };
            var lblLocTitle = new Label
            {
                Text = "CHỌN ĐỊA ĐIỂM",
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(130, 135, 145),
                Location = new Point(16, 8),
                AutoSize = true
            };
            _locationPicker = new LocationPickerControl { Location = new Point(16, 28), Width = 348, Height = 154 };
            _locationPicker.PickupClicked += (_, _) => { _isPickupSlotActive = true; ShowSuggestions(); };
            _locationPicker.DestinationClicked += (_, _) => { _isPickupSlotActive = false; ShowSuggestions(); };
            pnlLoc.Controls.Add(lblLocTitle);
            pnlLoc.Controls.Add(_locationPicker);

            // Vehicle selector
            var pnlVehicle = new Panel { Dock = DockStyle.Top, Height = 100, BackColor = Color.White };
            var lblVehicle = new Label
            {
                Text = "LOẠI XE",
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(130, 135, 145),
                Location = new Point(16, 8),
                AutoSize = true
            };
            _pnlMotorbike = MakeVehicleCard("🏍️", "Xe máy", "Motorbike", true);
            _pnlCar = MakeVehicleCard("🚗", "Ô tô", "Car", false);
            _pnlMotorbike.Location = new Point(16, 28);
            _pnlCar.Location = new Point(196, 28);
            pnlVehicle.Controls.Add(lblVehicle);
            pnlVehicle.Controls.Add(_pnlMotorbike);
            pnlVehicle.Controls.Add(_pnlCar);

            // Fare card
            _pnlFareCard = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                Visible = false,
                BackColor = Color.FromArgb(240, 248, 255)
            };
            _lblDistance = new Label { Font = new Font("Segoe UI", 9f), ForeColor = AppTheme.TextMuted, Location = new Point(20, 10), AutoSize = true };
            _lblFare = new Label { Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = AppTheme.Primary, Location = new Point(20, 30), AutoSize = true };
            _pnlFareCard.Controls.Add(_lblDistance);
            _pnlFareCard.Controls.Add(_lblFare);

            // Status bar
            _pnlStatusBar = new Panel { Dock = DockStyle.Top, Height = 40, Visible = false, BackColor = AppTheme.Highlight };
            _lblTripStatus = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = AppTheme.Primary,
                Padding = new Padding(20, 0, 0, 0)
            };
            _pnlStatusBar.Controls.Add(_lblTripStatus);

            // Driver info
            _pnlDriverInfo = new Panel { Dock = DockStyle.Top, Height = 56, Visible = false, BackColor = Color.FromArgb(232, 245, 233) };
            _lblDriverInfo = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(27, 94, 32),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(20, 0, 0, 0)
            };
            _pnlDriverInfo.Controls.Add(_lblDriverInfo);

            // Buttons
            var pnlButtons = new Panel { Dock = DockStyle.Top, Height = 64 };
            _btnRequest = new Button
            {
                Text = "ĐẶT XE NGAY",
                Location = new Point(16, 12),
                Width = 216,
                Height = 40,
                BackColor = AppTheme.Success,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            _btnRequest.FlatAppearance.BorderSize = 0;
            _btnRequest.Click += async (_, _) => await OnRequestClicked();

            _btnCancel = new Button
            {
                Text = "HỦY CHUYẾN",
                Location = new Point(240, 12),
                Width = 124,
                Height = 40,
                BackColor = AppTheme.Danger,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Visible = false
            };
            _btnCancel.FlatAppearance.BorderSize = 0;
            _btnCancel.Click += async (_, _) => await OnCancelClicked();
            pnlButtons.Controls.Add(_btnRequest);
            pnlButtons.Controls.Add(_btnCancel);

            // Hint
            var lblHint = new Label
            {
                Text = "💡 Click vào ô địa điểm để tìm kiếm, hoặc nhấp phải lên bản đồ để ghim",
                Dock = DockStyle.Top,
                Height = 40,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = AppTheme.TextMuted,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(20, 0, 20, 0)
            };

            // Assemble sidebar (Dock=Top items stacked bottom-up, so add in reverse)
            sidebar.Controls.Add(lblHint);
            sidebar.Controls.Add(pnlButtons);
            sidebar.Controls.Add(_pnlDriverInfo);
            sidebar.Controls.Add(_pnlStatusBar);
            sidebar.Controls.Add(_pnlFareCard);
            sidebar.Controls.Add(pnlVehicle);
            sidebar.Controls.Add(pnlLoc);
            sidebar.Controls.Add(header);

            return sidebar;
        }

        private Panel MakeVehicleCard(string icon, string name, string tag, bool selected)
        {
            var pnl = new Panel
            {
                Width = 160,
                Height = 60,
                Cursor = Cursors.Hand,
                Tag = tag,
                BackColor = selected ? Color.FromArgb(235, 245, 255) : Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            var lblIcon = new Label { Text = icon, Font = new Font("Segoe UI", 18f), Location = new Point(8, 12), Size = new Size(36, 36), TextAlign = ContentAlignment.MiddleCenter };
            var lblName = new Label { Text = name, Font = new Font("Segoe UI", 10f, FontStyle.Bold), Location = new Point(50, 0), Size = new Size(105, 60), TextAlign = ContentAlignment.MiddleLeft, ForeColor = AppTheme.TextPrimary };
            pnl.Controls.AddRange(new Control[] { lblIcon, lblName });

            void OnClick(object? s, EventArgs e) => SelectVehicle(tag);
            pnl.Click += OnClick;
            foreach (Control c in pnl.Controls) c.Click += OnClick;
            return pnl;
        }

        private void SelectVehicle(string tag)
        {
            _selectedVehicle = tag;
            _pnlMotorbike.BackColor = tag == "Motorbike" ? Color.FromArgb(235, 245, 255) : Color.White;
            _pnlCar.BackColor = tag == "Car" ? Color.FromArgb(235, 245, 255) : Color.White;
            _ = UpdateEstimation();
        }

        // ── Suggestion list ───────────────────────────────────────────────────

        private void ShowSuggestions()
        {
            BuildSuggestionList(null);
        }

        private void BuildSuggestionList(string? query)
        {
            _lstSuggestions.Items.Clear();

            var histItems = string.IsNullOrEmpty(query)
                ? _history.Take(5).ToList()
                : _history.Where(h => h.Name.Contains(query, StringComparison.OrdinalIgnoreCase)).Take(5).ToList();

            if (histItems.Count > 0)
            {
                _lstSuggestions.Items.Add(new SugItem("Gần đây"));
                foreach (var h in histItems) _lstSuggestions.Items.Add(new SugItem(h));
            }

            var fixedItems = string.IsNullOrEmpty(query)
                ? _fixedLocations
                : _fixedLocations.Where(f => f.Name.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();

            if (fixedItems.Count > 0)
            {
                _lstSuggestions.Items.Add(new SugItem("Địa điểm gợi ý"));
                foreach (var f in fixedItems) _lstSuggestions.Items.Add(new SugItem(f));
            }

            PositionSuggestions();
        }

        private void PositionSuggestions()
        {
            if (_lstSuggestions.Items.Count == 0) { _lstSuggestions.Visible = false; return; }
            var screenPt = _locationPicker.Parent!.PointToScreen(_locationPicker.Location);
            var clientPt = PointToClient(screenPt);
            int yOffset = _isPickupSlotActive ? _locationPicker.Height / 2 : _locationPicker.Height;
            _lstSuggestions.Location = new Point(clientPt.X, clientPt.Y + yOffset);
            _lstSuggestions.Width = _locationPicker.Width;
            _lstSuggestions.Height = Math.Min(_lstSuggestions.Items.Count * 48, 260);
            _lstSuggestions.Visible = true;
            _lstSuggestions.BringToFront();
        }

        private void OnDrawSuggestion(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            e.DrawBackground();
            if (_lstSuggestions.Items[e.Index] is not SugItem item) return;

            if (item.Kind == SugKind.Header)
            {
                using var font = new Font("Segoe UI", 7.5f, FontStyle.Bold);
                using var brush = new SolidBrush(Color.FromArgb(120, 120, 130));
                e.Graphics.DrawString(item.Header.ToUpper(), font, brush,
                    new RectangleF(e.Bounds.X + 12, e.Bounds.Y + 16, e.Bounds.Width, 18));
                return;
            }

            var loc = item.Location!;
            using var nameFont = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            using var addrFont = new Font("Segoe UI", 8.5f);
            using var iconFont = new Font("Segoe UI", 11f);
            e.Graphics.DrawString("📍", iconFont, Brushes.CornflowerBlue,
                new RectangleF(e.Bounds.X + 8, e.Bounds.Y + 10, 24, 24));
            e.Graphics.DrawString(loc.Name, nameFont, Brushes.Black,
                new RectangleF(e.Bounds.X + 36, e.Bounds.Y + 6, e.Bounds.Width - 44, 20));
            e.Graphics.DrawString(loc.Address, addrFont, Brushes.Gray,
                new RectangleF(e.Bounds.X + 36, e.Bounds.Y + 26, e.Bounds.Width - 44, 18));
            e.DrawFocusRectangle();
        }

        private async void OnSuggestionClicked(object? sender, MouseEventArgs e)
        {
            if (_lstSuggestions.SelectedItem is not SugItem item) return;
            if (item.Kind == SugKind.Header || item.Location == null) return;
            _lstSuggestions.Visible = false;
            await ApplyLocation(item.Location, _isPickupSlotActive);
        }

        // ── Location apply ────────────────────────────────────────────────────

        private async Task ApplyLocation(DomainLocation loc, bool isPickup)
        {
            var other = isPickup ? _locationPicker.Destination : _locationPicker.Pickup;
            if (other != null &&
                Math.Abs(loc.Lat - other.Lat) < 0.0001 &&
                Math.Abs(loc.Lng - other.Lng) < 0.0001)
            {
                MessageBox.Show("Điểm đón và điểm đến không được trùng nhau!",
                    "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (isPickup) _locationPicker.SetPickup(loc);
            else _locationPicker.SetDestination(loc);

            await _mapControl.SelectLocation(loc, isPickup);

            _history.RemoveAll(h => h.Name == loc.Name && h.Address == loc.Address);
            _history.Insert(0, loc);
            if (_history.Count > 20) _history.RemoveAt(_history.Count - 1);

            await UpdateEstimation();
            UpdateRequestButton();
        }

        // ── Estimation ────────────────────────────────────────────────────────

        private async Task UpdateEstimation()
        {
            var pickup = _locationPicker.Pickup;
            var dest = _locationPicker.Destination;
            if (pickup == null || dest == null) { _pnlFareCard.Visible = false; return; }

            try
            {
                var route = await _routeService.GetFullRouteAsync(pickup, dest);
                if (route == null) return;
                var vehicleType = Enum.Parse<VehicleType>(_selectedVehicle, true);
                var rule = await _fareService.GetFareRule(vehicleType);
                if (rule == null) return;

                decimal fare = rule.CalculateFare(route.Distance);
                _lblDistance.Text = $"Khoảng cách: {route.Distance:F2} km";
                _lblFare.Text = $"Ước tính: {fare:N0} VNĐ";
                _pnlFareCard.Visible = true;
            }
            catch { _pnlFareCard.Visible = false; }
        }

        private void UpdateRequestButton()
        {
            bool ready = _locationPicker.IsReady && !_isRequesting && _shell.CurrentTrip == null;
            _btnRequest.Enabled = ready;
            _btnRequest.BackColor = ready ? AppTheme.Success : AppTheme.Disabled;
        }

        private async Task RefreshNearby()
        {
            try
            {
                var pickup = _locationPicker.Pickup;
                if (pickup == null) return;
                var vehicleType = Enum.Parse<VehicleType>(_selectedVehicle, true);
                var drivers = await _tripService.GetNearbyDrivers(pickup, vehicleType, 3.0);
                _mapControl.UpdateNearbyDrivers(drivers);
            }
            catch { }
        }

        // ── Request / Cancel ──────────────────────────────────────────────────

        private async Task OnRequestClicked()
        {
            var pickup = _locationPicker.Pickup;
            var dest = _locationPicker.Destination;
            if (pickup == null || dest == null) return;
            if (_shell.CurrentTrip != null) return;

            _isRequesting = true;
            _btnRequest.Text = "Đang xử lý...";
            UpdateRequestButton();

            try
            {
                var vehicleType = Enum.Parse<VehicleType>(_selectedVehicle, true);
                var trip = await _tripService.RequestTrip(_shell.Passenger.Id, pickup, dest, vehicleType);

                // Khóa UI trong lúc chờ
                _locationPicker.Enabled = false;
                _pnlMotorbike.Enabled = false;
                _pnlCar.Enabled = false;
                _btnRequest.Visible = false;
                _btnCancel.Visible = true;
                _nearbyTimer.Stop();
                SetStatus("⏳ Đang tìm tài xế...", AppTheme.Warning);

                // KEY POINT: Báo Shell, Shell chuyển sang ActiveTripScreen
                await _shell.OnTripStarted(trip);
            }
            catch (Exception ex)
            {
                _isRequesting = false;
                _btnRequest.Text = "ĐẶT XE NGAY";
                UpdateRequestButton();
                MessageBox.Show($"Không thể đặt xe: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task OnCancelClicked()
        {
            if (_shell.CurrentTrip == null) return;
            if (MessageBox.Show("Bạn có chắc muốn hủy chuyến?", "Xác nhận",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            try
            {
                await _tripService.CancelTrip(_shell.CurrentTrip.Id, "Hành khách hủy");
                _shell.SetCurrentTrip(null);
                ResetToIdle(null);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể hủy: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── UI state helpers ──────────────────────────────────────────────────

        public void ApplyTripUpdate(Trip trip)
        {
            if (InvokeRequired) { BeginInvoke(() => ApplyTripUpdate(trip)); return; }

            _btnRequest.Visible = false;
            _btnCancel.Visible = true;
            _btnCancel.Enabled = trip.Status is
                TripStatus.Requested or TripStatus.Searching or TripStatus.Matched;
            SetStatus(StatusText(trip.Status), StatusColor(trip.Status));

            _ = TryShowDriverInfo(trip);
        }

        private void SetStatus(string text, Color color)
        {
            if (InvokeRequired) { BeginInvoke(() => SetStatus(text, color)); return; }
            _lblTripStatus.Text = text;
            _lblTripStatus.ForeColor = color;
            _pnlStatusBar.Visible = true;
        }

        private async Task TryShowDriverInfo(Trip trip)
        {
            if (trip.Status is TripStatus.Requested or TripStatus.Searching || !trip.DriverId.HasValue)
            {
                _pnlDriverInfo.Visible = false;
                return;
            }

            try
            {
                var driver = await _userService.GetUserProfile(trip.DriverId.Value) as DriverEntity;
                if (driver != null)
                {
                    var vehicleType = driver.Vehicle != null ? (driver.Vehicle.GetVehicleType() == VehicleType.Motorbike ? "Xe máy" : "Ô tô") : "N/A";
                    var plate = driver.Vehicle?.PlateNumber ?? "N/A";
                    ShowDriverInfo($"Tài xế: {driver.Name} | {vehicleType} | {plate}");
                }
                else
                {
                    ShowDriverInfo("Đã có tài xế nhận chuyến.");
                }
            }
            catch
            {
                ShowDriverInfo("Đã có tài xế nhận chuyến.");
            }
        }

        private void ShowDriverInfo(string text)
        {
            if (InvokeRequired) { BeginInvoke(() => ShowDriverInfo(text)); return; }
            _lblDriverInfo.Text = text;
            _pnlDriverInfo.Visible = true;
        }

        private void ResetToIdle(string? statusMsg)
        {
            if (InvokeRequired) { BeginInvoke(() => ResetToIdle(statusMsg)); return; }

            _isRequesting = false;

            _locationPicker.Enabled = true;
            _locationPicker.SetPickup(null);
            _locationPicker.SetDestination(null);
            _pnlMotorbike.Enabled = true;
            _pnlCar.Enabled = true;

            _btnRequest.Text = "ĐẶT XE NGAY";
            _btnRequest.Visible = true;
            _btnCancel.Visible = false;
            _btnCancel.Enabled = true;

            _pnlFareCard.Visible = false;
            _pnlDriverInfo.Visible = false;

            if (statusMsg != null) SetStatus(statusMsg, AppTheme.TextMuted);
            else _pnlStatusBar.Visible = false;

            UpdateRequestButton();
            _mapControl.ClearRoute();
            _nearbyTimer.Start();
        }

        private static string StatusText(TripStatus s) => s switch
        {
            TripStatus.Requested => "⏳ Đang tìm tài xế...",
            TripStatus.Searching => "🔎 Đang tìm tài xế...",
            TripStatus.Matched => "🤝 Đã tìm được tài xế!",
            TripStatus.Arrived => "📍 Tài xế đã đến điểm đón",
            TripStatus.Started => "🚗 Đang di chuyển đến đích",
            TripStatus.Completed => "✅ Hoàn thành",
            TripStatus.Cancelled => "❌ Đã hủy",
            TripStatus.Timeout => "⌛ Hết thời gian",
            _ => s.ToString()
        };

        private static Color StatusColor(TripStatus s) => s switch
        {
            TripStatus.Matched or TripStatus.Arrived => AppTheme.Primary,
            TripStatus.Started or TripStatus.Completed => AppTheme.Success,
            TripStatus.Cancelled or TripStatus.Timeout => AppTheme.Danger,
            _ => AppTheme.Warning
        };

        protected override void Dispose(bool disposing)
        {
            if (disposing) { _nearbyTimer.Stop(); _nearbyTimer.Dispose(); }
            base.Dispose(disposing);
        }
    }
}
