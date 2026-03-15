using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using OOP.Application.Services.Interfaces;
using OOP.Domain.Entities;

namespace MYOOP.Presentation.TripForms
{
    public class DriverTripForm : Form
    {
        private readonly Guid _tripId;
        private readonly Guid _driverId;
        private readonly ITripService _tripService;
        private readonly IRouteService _routeService;

        private Trip? trip;

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

        // FIX: thêm ButtonMarkArrived — state machine yêu cầu Matched → Arrived → Ongoing.
        // Trước đây form chỉ có StartTrip và CompleteTrip → tài xế không thể đi qua bước Arrived từ UI.
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
            // FIX: async void Load không có try/catch → exception crash app.
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
            Text = "Driver - Chuyến đi";
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
            PanelTripInfo = new Panel { Dock = DockStyle.Right, Width = 320 };

            int y = 20;
            LabelTripId = CreateLabel(ref y);
            LabelPickup = CreateLabel(ref y);
            LabelDestination = CreateLabel(ref y);
            LabelDistance = CreateLabel(ref y);
            LabelFare = CreateLabel(ref y);
            LabelStatus = CreateLabel(ref y);

            y += 10;

            // FIX: nút "Đã đến điểm đón" — Matched → Arrived
            ButtonMarkArrived = MakeBtn("📍  Đã đến điểm đón", ref y);
            ButtonMarkArrived.Click += async (_, _) => await OnMarkArrivedClicked();

            // Arrived → Ongoing
            ButtonStartTrip = MakeBtn("▶  Bắt đầu chuyến", ref y);
            ButtonStartTrip.Click += async (_, _) => await OnStartTripClicked();

            // Ongoing → Completed
            ButtonCompleteTrip = MakeBtn("✓  Hoàn thành chuyến", ref y);
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
            var lbl = new Label { Location = new Point(20, y), Width = 280, Height = 24 };
            y += 28;
            return lbl;
        }

        private Button MakeBtn(string text, ref int y)
        {
            var btn = new Button { Text = text, Location = new Point(20, y), Width = 270, Height = 36 };
            y += 44;
            return btn;
        }

        private async Task OnFormLoad()
        {
            trip = await _tripService.GetTrip(_tripId);
            if (trip == null) { MessageBox.Show("Không tìm thấy chuyến đi."); Close(); return; }

            RefreshLabels();
            SetMarkers();
            await DrawRoute();
            UpdateButtonStates();
        }

        private void RefreshLabels()
        {
            if (trip == null) return;
            LabelTripId.Text = $"Trip: {trip.Id.ToString()[..8]}";
            LabelPickup.Text = $"Đón: {trip.PickupLocation.Address}";
            LabelDestination.Text = $"Đến: {trip.DestinationLocation.Address}";
            LabelDistance.Text = $"Khoảng cách: {trip.Distance:F2} km";
            LabelFare.Text = $"Cước phí: {trip.Fare:N0} VNĐ";
            LabelStatus.Text = $"Trạng thái: {trip.Status}";
        }

        private void UpdateButtonStates()
        {
            if (trip == null) return;
            ButtonMarkArrived.Enabled = trip.Status == OOP.Domain.Enums.TripStatus.Matched;
            ButtonStartTrip.Enabled = trip.Status == OOP.Domain.Enums.TripStatus.Arrived;
            ButtonCompleteTrip.Enabled = trip.Status == OOP.Domain.Enums.TripStatus.Ongoing;
        }

        private void SetMarkers()
        {
            markerOverlay.Markers.Clear();
            markerOverlay.Markers.Add(new GMarkerGoogle(
                new PointLatLng(trip!.PickupLocation.Lat, trip.PickupLocation.Lng),
                GMarkerGoogleType.green_dot));
            markerOverlay.Markers.Add(new GMarkerGoogle(
                new PointLatLng(trip.DestinationLocation.Lat, trip.DestinationLocation.Lng),
                GMarkerGoogleType.red_dot));
        }

        private async Task DrawRoute()
        {
            var route = await _routeService.GetFullRouteAsync(
                trip!.PickupLocation, trip.DestinationLocation);
            if (route == null) return;

            routeOverlay.Routes.Clear();
            var mapRoute = new GMapRoute(
                route.Points.Select(p => new PointLatLng(p.Lat, p.Lng)), "tripRoute");
            routeOverlay.Routes.Add(mapRoute);
            Map.ZoomAndCenterRoute(mapRoute);
        }

        private async Task OnMarkArrivedClicked()
        {
            try
            {
                await _tripService.MarkArrived(_tripId);
                trip = await _tripService.GetTrip(_tripId);
                RefreshLabels();
                UpdateButtonStates();
            }
            catch (Exception ex) { MessageBox.Show($"Lỗi: {ex.Message}"); }
        }

        private async Task OnStartTripClicked()
        {
            try
            {
                await _tripService.StartTrip(_tripId);
                trip = await _tripService.GetTrip(_tripId);
                RefreshLabels();
                UpdateButtonStates();
            }
            catch (Exception ex) { MessageBox.Show($"Lỗi: {ex.Message}"); }
        }

        private async Task OnCompleteTripClicked()
        {
            try
            {
                await _tripService.CompleteTrip(_tripId);
                trip = await _tripService.GetTrip(_tripId);
                RefreshLabels();
                UpdateButtonStates();
                MessageBox.Show("Chuyến đi hoàn thành. Xác nhận đã nhận tiền mặt.");
            }
            catch (Exception ex) { MessageBox.Show($"Lỗi: {ex.Message}"); }
        }
    }
}