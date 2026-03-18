﻿﻿﻿using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using OOP.Application.Services.Interfaces;
using OOP.Domain.Entities;
using OOP.Domain.Enums;

// FIX #1: was "MYOOP.Presentation.TripForms" — typo caused the class to live
//         in a different namespace from the rest of the project.
namespace OOP.Presentation.TripForms
{
    public class DriverTripForm : Form
    {
        private readonly Guid _tripId;
        private readonly Guid _driverId;
        private readonly ITripService _tripService;
        private readonly IRouteService _routeService;
        private readonly IUserService _userService;

        private Trip? _trip;
        private Driver? _driver;
        private bool _canMarkArrived = false;
        private readonly System.Windows.Forms.Timer _refreshTimer = new() { Interval = 2000 };

        private GMapControl Map = null!;
        private GMapOverlay markerOverlay = new("markers");
        private GMapOverlay routeOverlay = new("route");
        private GMapOverlay driverRouteOverlay = new("driverRoute");

        private Panel PanelTripInfo = null!;
        private Label LabelTripId = null!;
        private Label LabelPickup = null!;
        private Label LabelDestination = null!;
        private Label LabelDistance = null!;
        private Label LabelFare = null!;
        private Label LabelStatus = null!;

        private Button ButtonMarkArrived = null!;
        private Button ButtonStartTrip = null!;
        private Button ButtonCompleteTrip = null!;
        private Button ButtonBack = null!;

        public DriverTripForm(
            Guid tripId,
            Guid driverId,
            ITripService tripService,
            IRouteService routeService,
            IUserService userService)
        {
            _tripId = tripId;
            _driverId = driverId;
            _tripService = tripService;
            _routeService = routeService;
            _userService = userService;

            InitializeUI();
            Load += async (_, _) =>
            {
                try { await OnFormLoad(); }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi tải thông tin chuyến: {ex.Message}", "Lỗi",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Close();
                }
            };
            FormClosed += (_, _) =>
            {
                _refreshTimer.Stop();
                _refreshTimer.Dispose();
            };
        }

        private void InitializeUI()
        {
            Text = "Driver – Chuyến đi";
            Size = new Size(1000, 650);
            StartPosition = FormStartPosition.CenterScreen;

            Map = new GMapControl { Dock = DockStyle.Fill };
            GMaps.Instance.Mode = AccessMode.ServerAndCache;
            Map.MapProvider = GMapProviders.GoogleMap;
            Map.MinZoom = 2; Map.MaxZoom = 18; Map.Zoom = 13;
            Map.Position = new PointLatLng(10.7626, 106.6601);
            Map.Overlays.Add(markerOverlay);
            Map.Overlays.Add(routeOverlay);
            Map.Overlays.Add(driverRouteOverlay);
            Map.MouseClick += async (s, e) =>
            {
                if (e.Button == MouseButtons.Right)
                    await OnMapRightClick(e.X, e.Y);
            };

            BuildTripPanel();
            Controls.Add(Map);
            Controls.Add(PanelTripInfo);
        }

        private void BuildTripPanel()
        {
            PanelTripInfo = new Panel
            {
                Dock = DockStyle.Right,
                Width = 320,
                BackColor = Color.White,
                Padding = new Padding(0, 0, 0, 8)
            };

            int y = 20;
            LabelTripId = CreateLabel(ref y);
            LabelPickup = CreateLabel(ref y);
            LabelDestination = CreateLabel(ref y);
            LabelDistance = CreateLabel(ref y);
            LabelFare = CreateLabel(ref y);
            LabelStatus = CreateLabel(ref y);

            y += 10;

            ButtonMarkArrived = MakeBtn("📍  Đã đến điểm đón", ref y);
            ButtonMarkArrived.BackColor = Color.FromArgb(13, 110, 253);
            ButtonMarkArrived.ForeColor = Color.White;
            ButtonMarkArrived.Click += async (_, _) => await OnMarkArrivedClicked();

            ButtonStartTrip = MakeBtn("▶  Bắt đầu chuyến", ref y);
            ButtonStartTrip.BackColor = Color.FromArgb(25, 135, 84);
            ButtonStartTrip.ForeColor = Color.White;
            ButtonStartTrip.Click += async (_, _) => await OnStartTripClicked();

            ButtonCompleteTrip = MakeBtn("✓  Hoàn thành chuyến", ref y);
            ButtonCompleteTrip.BackColor = Color.FromArgb(102, 16, 242);
            ButtonCompleteTrip.ForeColor = Color.White;
            ButtonCompleteTrip.Click += async (_, _) => await OnCompleteTripClicked();

            ButtonBack = MakeBtn("← Quay lại", ref y);
            ButtonBack.Click += (_, _) => Close();

            PanelTripInfo.Controls.AddRange(new Control[]
            {
                LabelTripId, LabelPickup, LabelDestination,
                LabelDistance, LabelFare, LabelStatus,
                ButtonMarkArrived, ButtonStartTrip, ButtonCompleteTrip, ButtonBack
            });
        }

        private Label CreateLabel(ref int y)
        {
            var lbl = new Label
            {
                Location = new Point(20, y),
                Width = 280,
                Height = 24,
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(40, 40, 40)
            };
            y += 28;
            return lbl;
        }

        private Button MakeBtn(string text, ref int y)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(20, y),
                Width = 270,
                Height = 36,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            btn.FlatAppearance.BorderSize = 0;
            y += 44;
            return btn;
        }

        private async Task OnFormLoad()
        {
            await RefreshTripAsync();
            _refreshTimer.Tick += async (_, _) => await RefreshTripAsync();
            _refreshTimer.Start();
        }

        // FIX #2: all UI mutations now run on the UI thread
        private void RefreshLabels()
        {
            if (_trip == null) return;
            if (InvokeRequired) { BeginInvoke(RefreshLabels); return; }

            LabelTripId.Text = $"Trip: {_trip.Id.ToString()[..8]}";
            LabelPickup.Text = $"Đón: {_trip.PickupLocation.Address}";
            LabelDestination.Text = $"Đến: {_trip.DestinationLocation.Address}";
            LabelDistance.Text = $"Khoảng cách: {_trip.Distance:F2} km";
            LabelFare.Text = $"Cước phí: {(_trip.Fare > 0 ? $"{_trip.Fare:N0} VNĐ" : "Chưa tính")}";
            LabelStatus.Text = $"Trạng thái: {StatusLabel(_trip.Status)}";
        }

        private void UpdateButtonStates()
        {
            if (_trip == null) return;
            if (InvokeRequired) { BeginInvoke(UpdateButtonStates); return; }

            ButtonMarkArrived.Enabled = _trip.Status == OOP.Domain.Enums.TripStatus.Matched && _canMarkArrived;
            ButtonStartTrip.Enabled = _trip.Status == OOP.Domain.Enums.TripStatus.Arrived;
            ButtonCompleteTrip.Enabled = _trip.Status == OOP.Domain.Enums.TripStatus.Started;
        }

        private void SetMarkers()
        {
            markerOverlay.Markers.Clear();
            if (_driver != null)
            {
                markerOverlay.Markers.Add(new GMarkerGoogle(
                    new PointLatLng(_driver.CurrentLocation.Lat, _driver.CurrentLocation.Lng),
                    GMarkerGoogleType.blue_dot)
                { ToolTipText = "Tài xế" });
            }
            markerOverlay.Markers.Add(new GMarkerGoogle(
                new PointLatLng(_trip!.PickupLocation.Lat, _trip.PickupLocation.Lng),
                GMarkerGoogleType.green_dot)
            { ToolTipText = "Điểm đón" });
            markerOverlay.Markers.Add(new GMarkerGoogle(
                new PointLatLng(_trip.DestinationLocation.Lat, _trip.DestinationLocation.Lng),
                GMarkerGoogleType.red_dot)
            { ToolTipText = "Điểm đến" });
        }

        private async Task DrawRoutes()
        {
            routeOverlay.Routes.Clear();
            driverRouteOverlay.Routes.Clear();

            if (_trip == null) return;

            if ((_trip.Status == TripStatus.Matched || _trip.Status == TripStatus.Arrived) && _driver != null)
            {
                var points = await _routeService.GetRoutePointsAsync(
                    _driver.CurrentLocation, _trip.PickupLocation);
                if (points.Count >= 2)
                {
                    var pts = points.Select(p => new PointLatLng(p.Lat, p.Lng)).ToList();
                    driverRouteOverlay.Routes.Add(new GMapRoute(pts, "driverToPickup")
                    { Stroke = new Pen(Color.FromArgb(200, 0, 160, 60), 3) });
                    Map.ZoomAndCenterRoute(driverRouteOverlay.Routes[0]);
                    return;
                }
            }

            if (_trip.Status == TripStatus.Started || _trip.Status == TripStatus.Completed)
            {
                var route = await _routeService.GetFullRouteAsync(
                    _trip.PickupLocation, _trip.DestinationLocation);
                if (route != null && route.Points.Count >= 2)
                {
                    var pen = new Pen(Color.FromArgb(180, 0, 120, 255), 4);
                    var mapRoute = new GMapRoute(
                        route.Points.Select(p => new PointLatLng(p.Lat, p.Lng)), "tripRoute")
                    { Stroke = pen };

                    routeOverlay.Routes.Add(mapRoute);
                    Map.ZoomAndCenterRoute(mapRoute);
                }
            }

            Map.Refresh();
        }

        private async Task OnMarkArrivedClicked()
        {
            try
            {
                await _tripService.MarkArrived(_tripId);
                _trip = await _tripService.GetTrip(_tripId);
                RefreshLabels();
                UpdateButtonStates();
            }
            catch (Exception ex) { ShowError(ex.Message); }
        }

        private async Task OnStartTripClicked()
        {
            try
            {
                await _tripService.StartTrip(_tripId);
                _trip = await _tripService.GetTrip(_tripId);
                RefreshLabels();
                UpdateButtonStates();
            }
            catch (Exception ex) { ShowError(ex.Message); }
        }

        private async Task OnCompleteTripClicked()
        {
            var confirm = MessageBox.Show(
                "Xác nhận hoàn thành chuyến đi?",
                "Hoàn thành", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            try
            {
                await _tripService.CompleteTrip(_tripId);
                _trip = await _tripService.GetTrip(_tripId);
                RefreshLabels();
                UpdateButtonStates();
                MessageBox.Show("Chuyến đi hoàn thành. Xác nhận đã nhận tiền mặt.",
                    "Hoàn thành", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { ShowError(ex.Message); }
        }

        private static string StatusLabel(OOP.Domain.Enums.TripStatus status) => status switch
        {
            OOP.Domain.Enums.TripStatus.Requested => "⏳ Đang chờ tài xế",
            OOP.Domain.Enums.TripStatus.Searching => "🔎 Đang tìm tài xế",
            OOP.Domain.Enums.TripStatus.Matched => "🤝 Đã nhận",
            OOP.Domain.Enums.TripStatus.Arrived => "📍 Đã đến nơi đón",
            OOP.Domain.Enums.TripStatus.Started => "🚗 Đang chạy",
            OOP.Domain.Enums.TripStatus.Completed => "✅ Hoàn thành",
            OOP.Domain.Enums.TripStatus.Cancelled => "❌ Đã hủy",
            OOP.Domain.Enums.TripStatus.Timeout => "⌛ Hết thời gian",
            _ => status.ToString()
        };

        private static void ShowError(string msg) =>
            MessageBox.Show(msg, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Dispose all route Pens on form close
                foreach (var r in routeOverlay.Routes) r.Stroke?.Dispose();
                foreach (var r in driverRouteOverlay.Routes) r.Stroke?.Dispose();
            }
            base.Dispose(disposing);
        }

        private async Task RefreshTripAsync()
        {
            _trip = await _tripService.GetTrip(_tripId);
            if (_trip == null) return;
            var user = await _userService.GetUserProfile(_driverId);
            _driver = user as Driver;

            if (_trip.Status == TripStatus.Matched && _driver != null)
                _canMarkArrived = await _routeService.IsNearAsync(
                    _driver.CurrentLocation, _trip.PickupLocation, 0.08);
            else
                _canMarkArrived = false;

            RefreshLabels();
            SetMarkers();
            await DrawRoutes();
            UpdateButtonStates();
        }

        private async Task OnMapRightClick(int x, int y)
        {
            var confirm = MessageBox.Show(
                "Cập nhật vị trí hiện tại của tài xế tại điểm vừa chọn?",
                "Cập nhật vị trí", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            var point = Map.FromLocalToLatLng(x, y);
            var loc = new OOP.Domain.Entities.Location("Vị trí hiện tại", "Tài xế cập nhật", point.Lat, point.Lng);

            try
            {
                await _userService.UpdateDriverLocation(_driverId, loc);
                await RefreshTripAsync();
            }
            catch (Exception ex) { ShowError(ex.Message); }
        }
    }
}
