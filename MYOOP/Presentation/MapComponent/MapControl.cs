using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using Newtonsoft.Json.Linq;
using OOP.Application.Services.Interfaces;
using OOP.Domain.Entities;
using DomainLocation = OOP.Domain.Entities.Location;

namespace OOP.Presentation.Map
{
    public class MapControl : Form
    {
        // ── GMap control ──────────────────────────────────────────────────────
        public GMapControl gmap = null!;

        // Overlay thứ tự từ dưới lên (render sau = hiển thị trên cùng)
        private readonly GMapOverlay routeOverlay = new("route");
        private readonly GMapOverlay driverRouteOverlay = new("driverRoute");
        private readonly GMapOverlay poiOverlay = new("poi");
        private readonly GMapOverlay driverOverlay = new("driver");
        private readonly GMapOverlay pickupOverlay = new("pickup");
        private readonly GMapOverlay dropoffOverlay = new("dropoff");

        // ── Markers ───────────────────────────────────────────────────────────
        private GMarkerGoogle? pickupMarker;
        private GMarkerGoogle? dropoffMarker;
        private PointLatLng pickupPoint;
        private PointLatLng? dropoffPoint;

        public PointLatLng PickupPoint => pickupPoint;
        public PointLatLng? DropoffPoint => dropoffPoint;

        // ── Driver marker + animation ─────────────────────────────────────────
        private readonly Dictionary<string, GMarkerGoogle> driverMarkers = new();
        private readonly Dictionary<string, System.Windows.Forms.Timer> animationTimers = new();
        private const int AnimationSteps = 20;
        private const int AnimationInterval = 50;   // ms

        // ── Search UI ─────────────────────────────────────────────────────────
        private TextBox txtSearch = null!;
        private ListBox lstSuggestions = null!;
        private System.Windows.Forms.Timer searchTimer = null!;
        private CancellationTokenSource? cts;
        private readonly List<string> searchHistory = new();
        private const int MaxHistory = 5;

        // ── POI ───────────────────────────────────────────────────────────────
        private bool poiLoading = false;
        private DateTime lastPoiLoad = DateTime.MinValue;

        // ── Drag detection ────────────────────────────────────────────────────
        private bool dragging = false;
        private bool wasDragging = false;
        private Point mouseDownPos;

        // ── Dependencies ──────────────────────────────────────────────────────
        private readonly HttpClient _http;
        private readonly IRouteService _routeService;

        // ── Nominatim rate-limit (TOS: max 1 req/s) ───────────────────────────
        private DateTime _lastNominatimCall = DateTime.MinValue;
        private const int NominatimIntervalMs = 1100;

        // ── Geocode cache (reverse only; forward autocomplete via Photon) ─────
        private readonly Dictionary<string, string> reverseCache = new();

        // ── Events ────────────────────────────────────────────────────────────
        /// <summary>Phát ra khi người dùng chọn điểm đến (search hoặc right-click).</summary>
        public event Action<PointLatLng, string>? LocationSelected;

        // ═════════════════════════════════════════════════════════════════════
        // Static init — gọi từ Program.cs TRƯỚC Application.Run()
        // ═════════════════════════════════════════════════════════════════════
        public static void InitializeMapProvider()
        {
            GMapProvider.UserAgent =
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
                "AppleWebKit/537.36 (KHTML, like Gecko) " +
                "Chrome/124.0.0.0 Safari/537.36";
            GMaps.Instance.Mode = AccessMode.ServerAndCache;
        }

        // ═════════════════════════════════════════════════════════════════════
        // Constructor
        // ═════════════════════════════════════════════════════════════════════
        public MapControl(HttpClient http, IRouteService routeService)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _routeService = routeService ?? throw new ArgumentNullException(nameof(routeService));

            if (!_http.DefaultRequestHeaders.Contains("User-Agent"))
                _http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 RideGoApp/1.0");

            TopLevel = false;
            FormBorderStyle = FormBorderStyle.None;

            InitMap();
            InitSearch();
        }

        // ═════════════════════════════════════════════════════════════════════
        // Map init
        // ═════════════════════════════════════════════════════════════════════
        private void InitMap()
        {
            gmap = new GMapControl { Dock = DockStyle.Fill };

            GMaps.Instance.Mode = AccessMode.ServerAndCache;
            gmap.MapProvider = GMapProviders.GoogleMap;
            gmap.CacheLocation = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RideGoMapCache");

            gmap.MinZoom = 2;
            gmap.MaxZoom = 18;
            gmap.Zoom = 14;
            gmap.Position = new PointLatLng(10.7626, 106.6601);
            gmap.ShowCenter = false;
            gmap.CanDragMap = true;
            gmap.DragButton = MouseButtons.Left;
            gmap.MarkersEnabled = true;
            gmap.RoutesEnabled = true;

            // Thứ tự add = thứ tự render từ dưới lên
            gmap.Overlays.Add(routeOverlay);
            gmap.Overlays.Add(driverRouteOverlay);
            gmap.Overlays.Add(poiOverlay);
            gmap.Overlays.Add(driverOverlay);
            gmap.Overlays.Add(pickupOverlay);
            gmap.Overlays.Add(dropoffOverlay);

            gmap.OnMapDrag += async () => await LoadPOI(gmap.Position);
            gmap.OnMapZoomChanged += async () => await LoadPOI(gmap.Position);

            gmap.MouseDown += MapMouseDown;
            gmap.MouseMove += MapMouseMove;
            gmap.MouseUp += MapMouseUp;
            gmap.MouseClick += MapMouseClick;

            Controls.Add(gmap);
        }

        // ═════════════════════════════════════════════════════════════════════
        // Search UI
        // ═════════════════════════════════════════════════════════════════════
        private void InitSearch()
        {
            txtSearch = new TextBox
            {
                Width = 350,
                Location = new Point(15, 15),
                PlaceholderText = "Nhập điểm đến của bạn...",
                Font = new Font("Segoe UI", 11)
            };

            lstSuggestions = new ListBox
            {
                Width = 350,
                Height = 200,
                Location = new Point(15, 46),
                Visible = false,
                Font = new Font("Segoe UI", 10),
                BorderStyle = BorderStyle.FixedSingle
            };

            searchTimer = new System.Windows.Forms.Timer { Interval = 500 };
            searchTimer.Tick += async (_, _) =>
            {
                searchTimer.Stop();
                if (IsDisposed || txtSearch.IsDisposed) return;
                await RunSearch(cts?.Token ?? CancellationToken.None);
            };

            txtSearch.TextChanged += OnSearchTextChanged;
            txtSearch.Click += OnSearchBoxClick;
            lstSuggestions.MouseClick += OnSuggestionClick;

            Controls.Add(lstSuggestions);
            Controls.Add(txtSearch);
            txtSearch.BringToFront();
        }

        private void OnSearchTextChanged(object? sender, EventArgs e)
        {
            cts?.Cancel();
            cts = new CancellationTokenSource();
            searchTimer.Stop();
            if (string.IsNullOrWhiteSpace(txtSearch.Text))
            { lstSuggestions.Visible = false; return; }
            if (txtSearch.Text.Trim().Length >= 3) searchTimer.Start();
        }

        private void OnSearchBoxClick(object? sender, EventArgs e)
        {
            if (searchHistory.Count == 0) return;
            lstSuggestions.BeginUpdate();
            lstSuggestions.Items.Clear();
            foreach (var s in searchHistory) lstSuggestions.Items.Add(s);
            lstSuggestions.EndUpdate();
            lstSuggestions.Visible = true;
            lstSuggestions.BringToFront();
        }

        private async void OnSuggestionClick(object? sender, MouseEventArgs e)
        {
            int idx = lstSuggestions.IndexFromPoint(e.Location);
            if (idx < 0) return;

            string address = lstSuggestions.Items[idx].ToString()!;
            txtSearch.Text = address;
            lstSuggestions.Visible = false;
            searchTimer.Stop();

            var point = await NominatimGeocode(address);
            if (point == null) return;

            await SetDropoffMarker(point.Value);
            gmap.Position = point.Value;
            gmap.Zoom = 16;
            LocationSelected?.Invoke(point.Value, address);
            AddHistory(address);
        }

        private async Task RunSearch(CancellationToken token)
        {
            string query = txtSearch.Text.Trim();
            if (query.Length < 3) { lstSuggestions.Visible = false; return; }
            try
            {
                var list = await PhotonAutocomplete(query);
                if (token.IsCancellationRequested) return;

                lstSuggestions.BeginUpdate();
                lstSuggestions.Items.Clear();
                if (list?.Count > 0)
                {
                    lstSuggestions.Items.AddRange(list.ToArray());
                    lstSuggestions.Visible = true;
                    lstSuggestions.BringToFront();
                }
                else lstSuggestions.Visible = false;
                lstSuggestions.EndUpdate();
            }
            catch { }
        }

        private async Task<List<string>> PhotonAutocomplete(string query)
        {
            var result = new List<string>();
            try
            {
                string url = $"https://photon.komoot.io/api/?q={Uri.EscapeDataString(query)}&limit=5&lang=vi";
                var json = JObject.Parse(await _http.GetStringAsync(url));
                foreach (var f in json["features"]!)
                {
                    var props = f["properties"];
                    string? name = props?["name"]?.ToString();
                    string? street = props?["street"]?.ToString();
                    string? city = props?["city"]?.ToString() ?? props?["state"]?.ToString();
                    if (string.IsNullOrEmpty(name)) continue;
                    string full = name;
                    if (!string.IsNullOrEmpty(street) && street != name) full += $", {street}";
                    if (!string.IsNullOrEmpty(city)) full += $", {city}";
                    result.Add(full);
                }
            }
            catch { }
            return result;
        }

        // ═════════════════════════════════════════════════════════════════════
        // Mouse drag detection
        // ═════════════════════════════════════════════════════════════════════
        private void MapMouseDown(object? sender, MouseEventArgs e)
        {
            dragging = false;
            mouseDownPos = new Point(e.X, e.Y);
        }

        private void MapMouseMove(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            if (Math.Abs(e.X - mouseDownPos.X) > 5 || Math.Abs(e.Y - mouseDownPos.Y) > 5)
                dragging = true;
        }

        private void MapMouseUp(object? sender, MouseEventArgs e)
        {
            wasDragging = dragging;
            dragging = false;
        }

        private async void MapMouseClick(object? sender, MouseEventArgs e)
        {
            if (wasDragging) return;
            if (e.Button != MouseButtons.Right) return;

            var point = gmap.FromLocalToLatLng(e.X, e.Y);
            lstSuggestions.Visible = false;

            await SetDropoffMarker(point);
            string address = await GetAddressFromPoint(point);
            txtSearch.Text = address;
            LocationSelected?.Invoke(point, address);
            AddHistory(address);
        }

        // ═════════════════════════════════════════════════════════════════════
        // Marker helpers
        // ═════════════════════════════════════════════════════════════════════
        public void SetPickupMarker(PointLatLng point)
        {
            pickupOverlay.Markers.Clear();
            pickupMarker = new GMarkerGoogle(point, GMarkerGoogleType.green_dot)
            { ToolTipText = "Điểm đón" };
            pickupOverlay.Markers.Add(pickupMarker);
            pickupPoint = point;
            gmap.Refresh();
        }

        /// <summary>
        /// Đặt marker điểm đến rồi tự động vẽ tuyến pickup → dropoff
        /// thông qua IRouteService (lộ trình thật).
        /// </summary>
        public async Task SetDropoffMarker(PointLatLng point)
        {
            dropoffOverlay.Markers.Clear();
            dropoffMarker = new GMarkerGoogle(point, GMarkerGoogleType.red_dot);
            ApplyCustomTooltip(dropoffMarker, "Đang xác định địa chỉ...");
            dropoffOverlay.Markers.Add(dropoffMarker);
            dropoffPoint = point;

            string address = await GetAddressFromPoint(point);
            if (dropoffMarker != null) dropoffMarker.ToolTipText = address;

            if (pickupPoint != default)
                await DrawTripRouteAsync(pickupPoint, point);
        }

        // ═════════════════════════════════════════════════════════════════════
        // Route drawing — dùng IRouteService
        // ═════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Vẽ tuyến chuyến đi (pickup → dropoff) lên routeOverlay.
        /// Dùng IRouteService.GetRoutePointsAsync để lấy lộ trình thật.
        /// </summary>
        public async Task DrawTripRouteAsync(PointLatLng pickup, PointLatLng dropoff)
        {
            var points = await _routeService.GetRoutePointsAsync(
                ToLocation(pickup), ToLocation(dropoff));

            if (points.Count < 2)
            {
                DrawStraightLine(routeOverlay, pickup, dropoff);
                return;
            }

            DrawRouteOnOverlay(
                routeOverlay,
                points,
                new Pen(Color.FromArgb(180, 0, 120, 255), 5),
                "TripRoute");
        }

        /// <summary>
        /// Vẽ tuyến dẫn đường cho tài xế (vị trí hiện tại → điểm đón)
        /// lên driverRouteOverlay (màu xanh lá, nét mảnh hơn).
        /// Gọi lại mỗi khi vị trí tài xế cập nhật khi status = Matched.
        /// </summary>
        public async Task DrawDriverToPickupRouteAsync(PointLatLng driverPos, PointLatLng pickup)
        {
            var points = await _routeService.GetRoutePointsAsync(
                ToLocation(driverPos), ToLocation(pickup));

            if (points.Count < 2)
            {
                DrawStraightLine(driverRouteOverlay, driverPos, pickup);
                return;
            }

            DrawRouteOnOverlay(
                driverRouteOverlay,
                points,
                new Pen(Color.FromArgb(200, 0, 160, 60), 3),
                "DriverRoute");
        }

        /// <summary>Xoá tuyến dẫn đường của tài xế (sau khi đón hành khách).</summary>
        public void ClearDriverRoute()
        {
            if (InvokeRequired) { BeginInvoke(ClearDriverRoute); return; }
            driverRouteOverlay.Routes.Clear();
            gmap.Refresh();
        }

        private void DrawRouteOnOverlay(
            GMapOverlay overlay,
            List<DomainLocation> locations,
            Pen pen,
            string name)
        {
            if (InvokeRequired)
            {
                BeginInvoke(() => DrawRouteOnOverlay(overlay, locations, pen, name));
                return;
            }

            var pts = locations.Select(ToPointLatLng).ToList();
            overlay.Routes.Clear();
            overlay.Routes.Add(new GMapRoute(pts, name) { Stroke = pen });
            gmap.Refresh();
        }

        private void DrawStraightLine(GMapOverlay overlay, PointLatLng start, PointLatLng end)
        {
            if (InvokeRequired) { BeginInvoke(() => DrawStraightLine(overlay, start, end)); return; }
            overlay.Routes.Clear();
            overlay.Routes.Add(new GMapRoute(new List<PointLatLng> { start, end }, "Fallback")
            {
                Stroke = new Pen(Color.FromArgb(100, Color.Gray), 2)
                { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash }
            });
            gmap.Refresh();
        }

        /// <summary>Zoom + center để toàn bộ trip route vừa khung nhìn.</summary>
        public void ZoomToRoute()
        {
            if (InvokeRequired) { BeginInvoke(ZoomToRoute); return; }
            if (routeOverlay.Routes.Count > 0)
                gmap.ZoomAndCenterRoute(routeOverlay.Routes[0]);
        }

        // ═════════════════════════════════════════════════════════════════════
        // Geofence — dùng IRouteService.IsNearAsync
        // ═════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Trả về true nếu tài xế đang trong bán kính radiusKm so với điểm đón
        /// theo lộ trình thật (không phải đường chim bay).
        /// Dùng trong SimulationService trước khi gọi MarkArrived().
        /// </summary>
        public async Task<bool> IsDriverNearPickup(
            PointLatLng driverPos,
            PointLatLng pickup,
            double radiusKm = 0.05)
        {
            return await _routeService.IsNearAsync(
                ToLocation(driverPos),
                ToLocation(pickup),
                radiusKm);
        }

        // ═════════════════════════════════════════════════════════════════════
        // Driver marker + smooth animation
        // ═════════════════════════════════════════════════════════════════════
        public void UpdateDriverLocation(Guid driverId, PointLatLng pos)
        {
            if (InvokeRequired) { BeginInvoke(() => UpdateDriverLocation(driverId, pos)); return; }
            AnimateDriverMarker(driverId, pos);
        }

        private void AnimateDriverMarker(Guid id, PointLatLng endPos)
        {
            string key = id.ToString();

            if (!driverMarkers.ContainsKey(key))
            {
                var m = new GMarkerGoogle(endPos, GMarkerGoogleType.blue_dot)
                {
                    ToolTipText = "Tài xế đang đến",
                    ToolTipMode = MarkerTooltipMode.OnMouseOver
                };
                driverMarkers[key] = m;
                driverOverlay.Markers.Add(m);
                return;
            }

            var marker = driverMarkers[key];
            var startPos = marker.Position;

            if (animationTimers.TryGetValue(key, out var old))
            { old.Stop(); old.Dispose(); animationTimers.Remove(key); }

            int step = 0;
            var timer = new System.Windows.Forms.Timer { Interval = AnimationInterval };
            timer.Tick += (_, _) =>
            {
                step++;
                if (step <= AnimationSteps)
                {
                    double t = (double)step / AnimationSteps;
                    marker.Position = new PointLatLng(
                        startPos.Lat + (endPos.Lat - startPos.Lat) * t,
                        startPos.Lng + (endPos.Lng - startPos.Lng) * t);
                }
                else
                {
                    marker.Position = endPos;
                    timer.Stop(); timer.Dispose();
                    animationTimers.Remove(key);
                }
            };
            animationTimers[key] = timer;
            timer.Start();
        }

        // ═════════════════════════════════════════════════════════════════════
        // POI overlay (Overpass API)
        // ═════════════════════════════════════════════════════════════════════
        private async Task LoadPOI(PointLatLng pos)
        {
            if (poiLoading || gmap.Zoom < 14) return;
            if ((DateTime.Now - lastPoiLoad).TotalSeconds < 5) return;

            poiLoading = true;
            lastPoiLoad = DateTime.Now;
            poiOverlay.Markers.Clear();

            var rect = gmap.ViewArea;
            string q = $"[out:json];\nnode[\"amenity\"]" +
                       $"({rect.Bottom},{rect.Left},{rect.Top},{rect.Right});\nout;";
            try
            {
                var res = await _http.PostAsync(
                    "https://overpass.kumi.systems/api/interpreter",
                    new FormUrlEncodedContent(new Dictionary<string, string> { { "data", q } }));
                if (!res.IsSuccessStatusCode) return;

                foreach (var el in JObject.Parse(await res.Content.ReadAsStringAsync())["elements"]!)
                {
                    string amenity = el["tags"]?["amenity"]?.ToString() ?? "";
                    GMarkerGoogleType icon = amenity switch
                    {
                        "restaurant" => GMarkerGoogleType.red_small,
                        "cafe" => GMarkerGoogleType.orange_small,
                        "hospital" => GMarkerGoogleType.blue_dot,
                        "atm" => GMarkerGoogleType.green_small,
                        _ => GMarkerGoogleType.orange_dot
                    };
                    poiOverlay.Markers.Add(new GMarkerGoogle(
                        new PointLatLng((double)el["lat"]!, (double)el["lon"]!), icon)
                    { ToolTipText = el["tags"]?["name"]?.ToString() ?? "POI" });
                }
            }
            catch { }
            finally { poiLoading = false; }
        }

        // ═════════════════════════════════════════════════════════════════════
        // Geocoding (Nominatim — chỉ dùng cho địa chỉ; route đi qua RouteService)
        // ═════════════════════════════════════════════════════════════════════
        public async Task<PointLatLng?> NominatimGeocode(string address)
        {
            address = address.Trim();
            try
            {
                string url = $"https://nominatim.openstreetmap.org/search" +
                             $"?q={Uri.EscapeDataString(address)}&format=json&limit=1";
                var arr = JArray.Parse(await RateLimitedNominatimGet(url) ?? "[]");
                if (arr.Count == 0) return null;
                return new PointLatLng(
                    double.Parse(arr[0]["lat"]!.ToString(), System.Globalization.CultureInfo.InvariantCulture),
                    double.Parse(arr[0]["lon"]!.ToString(), System.Globalization.CultureInfo.InvariantCulture));
            }
            catch { return null; }
        }

        public async Task<string> GetAddressFromPoint(PointLatLng point)
        {
            string key = $"{point.Lat:F5},{point.Lng:F5}";
            if (reverseCache.TryGetValue(key, out var cached)) return cached;
            try
            {
                string url = $"https://nominatim.openstreetmap.org/reverse" +
                              $"?lat={point.Lat}&lon={point.Lng}&format=json&addressdetails=1";
                var json = JObject.Parse(await RateLimitedNominatimGet(url) ?? "{}");
                string addr = json["display_name"]?.ToString() ?? $"{point.Lat:F4}, {point.Lng:F4}";
                reverseCache[key] = addr;
                return addr;
            }
            catch { return $"{point.Lat:F4}, {point.Lng:F4}"; }
        }

        /// <summary>Lấy vị trí hiện tại qua IP, đặt pickup marker.</summary>
        public async Task<string> ZoomToMyLocation()
        {
            try
            {
                var obj = JObject.Parse(await _http.GetStringAsync("https://ipapi.co/json/"));
                if (obj["latitude"] == null) return "Vị trí hiện tại";
                var point = new PointLatLng((double)obj["latitude"]!, (double)obj["longitude"]!);
                gmap.Position = point;
                gmap.Zoom = 15;
                SetPickupMarker(point);
                return await GetAddressFromPoint(point);
            }
            catch { return "Vị trí hiện tại"; }
        }

        private async Task<string?> RateLimitedNominatimGet(string url)
        {
            var elapsed = (DateTime.UtcNow - _lastNominatimCall).TotalMilliseconds;
            if (elapsed < NominatimIntervalMs)
                await Task.Delay((int)(NominatimIntervalMs - elapsed));
            _lastNominatimCall = DateTime.UtcNow;
            return await _http.GetStringAsync(url);
        }

        // ═════════════════════════════════════════════════════════════════════
        // Clear / reset
        // ═════════════════════════════════════════════════════════════════════
        public void ClearRoute()
        {
            var timers = animationTimers.Values.ToArray();
            animationTimers.Clear();
            foreach (var t in timers) { t.Stop(); t.Dispose(); }

            routeOverlay.Routes.Clear();
            driverRouteOverlay.Routes.Clear();
            dropoffOverlay.Markers.Clear();
            pickupOverlay.Markers.Clear();
            driverOverlay.Markers.Clear();
            poiOverlay.Markers.Clear();

            dropoffPoint = null;
            pickupPoint = default;
            dropoffMarker = null;
            pickupMarker = null;
            driverMarkers.Clear();
            gmap?.Refresh();
        }

        // ═════════════════════════════════════════════════════════════════════
        // Conversion helpers  (PointLatLng ↔ Location)
        // ═════════════════════════════════════════════════════════════════════

        /// <summary>Chuyển GMap.NET PointLatLng → Domain Location (không có label/address).</summary>
        private static DomainLocation ToLocation(PointLatLng p) =>
            new(string.Empty, string.Empty, p.Lat, p.Lng);

        /// <summary>Chuyển Domain Location → GMap.NET PointLatLng.</summary>
        private static PointLatLng ToPointLatLng(DomainLocation l) =>
            new(l.Lat, l.Lng);

        // ═════════════════════════════════════════════════════════════════════
        // Misc helpers
        // ═════════════════════════════════════════════════════════════════════
        private void AddHistory(string address)
        {
            if (string.IsNullOrWhiteSpace(address)) return;
            searchHistory.Remove(address);
            searchHistory.Insert(0, address);
            if (searchHistory.Count > MaxHistory) searchHistory.RemoveAt(MaxHistory);
        }

        private static void ApplyCustomTooltip(GMapMarker marker, string text)
        {
            marker.ToolTipText = text;
            marker.ToolTipMode = MarkerTooltipMode.OnMouseOver;
            marker.ToolTip = new GMapToolTip(marker)
            {
                Fill = new SolidBrush(Color.FromArgb(230, 40, 40, 40)),
                Foreground = Brushes.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Stroke = new Pen(Color.White, 1),
                Offset = new Point(10, -25)
            };
        }

        // ═════════════════════════════════════════════════════════════════════
        // Dispose
        // ═════════════════════════════════════════════════════════════════════
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                searchTimer?.Dispose();
                cts?.Dispose();
                foreach (var t in animationTimers.Values) { t.Stop(); t.Dispose(); }
                animationTimers.Clear();
            }
            base.Dispose(disposing);
        }
    }
}