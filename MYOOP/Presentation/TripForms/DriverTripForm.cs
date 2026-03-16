﻿using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using OOP.Application.Services.Interfaces;
using OOP.Domain.Entities;

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

        private Trip? _trip;

        private GMapControl Map = null!;
        private GMapOverlay markerOverlay = new("markers");
        private GMapOverlay routeOverlay = new("route");

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
            IRouteService routeService)
        {
            _tripId = tripId;
            _driverId = driverId;
            _tripService = tripService;
            _routeService = routeService;

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
            _trip = await _tripService.GetTrip(_tripId);
            if (_trip == null) { MessageBox.Show("Không tìm thấy chuyến đi."); Close(); return; }

            RefreshLabels();
            SetMarkers();
            await DrawRoute();
            UpdateButtonStates();
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

            ButtonMarkArrived.Enabled = _trip.Status == OOP.Domain.Enums.TripStatus.Matched;
            ButtonStartTrip.Enabled = _trip.Status == OOP.Domain.Enums.TripStatus.Arrived;
            ButtonCompleteTrip.Enabled = _trip.Status == OOP.Domain.Enums.TripStatus.Ongoing;
        }

        private void SetMarkers()
        {
            markerOverlay.Markers.Clear();
            markerOverlay.Markers.Add(new GMarkerGoogle(
                new PointLatLng(_trip!.PickupLocation.Lat, _trip.PickupLocation.Lng),
                GMarkerGoogleType.green_dot)
            { ToolTipText = "Điểm đón" });
            markerOverlay.Markers.Add(new GMarkerGoogle(
                new PointLatLng(_trip.DestinationLocation.Lat, _trip.DestinationLocation.Lng),
                GMarkerGoogleType.red_dot)
            { ToolTipText = "Điểm đến" });
        }

        private async Task DrawRoute()
        {
            var route = await _routeService.GetFullRouteAsync(
                _trip!.PickupLocation, _trip.DestinationLocation);
            if (route == null || route.Points.Count < 2) return;

            // FIX #3: dispose the old Pen before clearing, and keep ownership clear
            foreach (var r in routeOverlay.Routes) r.Stroke?.Dispose();
            routeOverlay.Routes.Clear();

            var pen = new Pen(Color.FromArgb(180, 0, 120, 255), 4);
            var mapRoute = new GMapRoute(
                route.Points.Select(p => new PointLatLng(p.Lat, p.Lng)), "tripRoute")
            { Stroke = pen };

            routeOverlay.Routes.Add(mapRoute);
            Map.ZoomAndCenterRoute(mapRoute);
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
            OOP.Domain.Enums.TripStatus.Matched => "🤝 Đã nhận",
            OOP.Domain.Enums.TripStatus.Arrived => "📍 Đã đến nơi đón",
            OOP.Domain.Enums.TripStatus.Ongoing => "🚗 Đang chạy",
            OOP.Domain.Enums.TripStatus.Completed => "✅ Hoàn thành",
            OOP.Domain.Enums.TripStatus.Cancelled => "❌ Đã hủy",
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
            }
            base.Dispose(disposing);
        }
    }
}