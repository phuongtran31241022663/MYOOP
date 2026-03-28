using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using Newtonsoft.Json.Linq;
using OOP.Application.Services.Interfaces;
using OOP.Domain.Entities;
using OOP.Domain.Enums;
using DomainLocation = OOP.Domain.Entities.GeoLocation;

namespace OOP.Presentation.Common.MapComponent
{
    public class MapControl : UserControl
    {
        // ── GMap ──────────────────────────────────────────────────────────────
        private GMapControl gmap = null!;

        private readonly GMapOverlay routeOverlay = new("route");
        private readonly GMapOverlay driverRouteOverlay = new("driverRoute");
        private readonly GMapOverlay poiOverlay = new("poi");
        private readonly GMapOverlay driverOverlay = new("driver");
        private readonly GMapOverlay pickupOverlay = new("pickup");
        private readonly GMapOverlay dropoffOverlay = new("dropoff");

        // ── Markers ───────────────────────────────────────────────────────────
        private GMarkerGoogle? _pickupMarker;
        private GMarkerGoogle? _dropoffMarker;
        private PointLatLng _pickupPoint;
        private PointLatLng? _dropoffPoint;

        public PointLatLng PickupPoint => _pickupPoint;
        public PointLatLng? DropoffPoint => _dropoffPoint;

        // ── Driver markers + animation ────────────────────────────────────────
        private readonly Dictionary<string, GMarkerGoogle> _driverMarkers = new();
        private readonly Dictionary<string, System.Windows.Forms.Timer> _animTimers = new();
        private const int AnimSteps = 20;
        private const int AnimInterval = 50;

        // ── POI throttle ─────────────────────────────────────────────────────
        private bool _poiLoading = false;
        private DateTime _lastPoiLoad = DateTime.MinValue;

        // ── Drag detection ────────────────────────────────────────────────────
        private bool _isDragging;
        private Point _mouseDownPos;
        private bool _isRightDragging;
        private Point _rightMouseDownPos;

        // ── Dependencies ──────────────────────────────────────────────────────
        private readonly HttpClient _http;
        private readonly IRouteService _routeService;

        private static DateTime _lastNominatimCall = DateTime.MinValue;
        private static readonly SemaphoreSlim _nominatimSem = new(1, 1);
        private const int NominatimIntervalMs = 1100;

        // ── Geocode cache ─────────────────────────────────────────────────────
        private readonly Dictionary<string, string> _reverseCache = new();
        private const int MaxCacheSize = 500;

        // ── HCMC bounds ───────────────────────────────────────────────────────
        private const double HcmMinLat = 10.35, HcmMaxLat = 11.20;
        private const double HcmMinLng = 106.40, HcmMaxLng = 107.10;

        // ── Static GDI (app-lifetime, intentionally not disposed) ─────────────
        private static readonly SolidBrush _tooltipFill = new(Color.FromArgb(230, 40, 40, 40));
        private static readonly Font _tooltipFont = new("Segoe UI", 9, FontStyle.Bold);
        private static readonly Pen _tooltipStroke = new(Color.White, 1);

        // ── Events ────────────────────────────────────────────────────────────
        public event Action<PointLatLng, string, bool>? LocationSelected;
        private Func<bool>? _isPickupSelector;

        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(
            System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool AllowLeftClickSelect { get; set; } = false;

        public void SetPickupSelector(Func<bool> selector) => _isPickupSelector = selector;

        // ── Static init ───────────────────────────────────────────────────────
        public static void InitializeMapProvider()
        {
            GMapProvider.UserAgent =
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
                "AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36";
            GMaps.Instance.Mode = AccessMode.ServerAndCache;
        }

        // ── Constructor ───────────────────────────────────────────────────────
        public MapControl(HttpClient http, IRouteService routeService)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _routeService = routeService ?? throw new ArgumentNullException(nameof(routeService));

            if (!_http.DefaultRequestHeaders.Contains("User-Agent"))
                _http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 OPPApp/1.0");

            InitMap();
        }

        // ── Map init ──────────────────────────────────────────────────────────
        private void InitMap()
        {
            gmap = new GMapControl { Dock = DockStyle.Fill };
            GMaps.Instance.Mode = AccessMode.ServerAndCache;
            gmap.MapProvider = GMapProviders.GoogleMap;
            gmap.CacheLocation = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "OOPMapCache");

            gmap.MinZoom = 2; gmap.MaxZoom = 18; gmap.Zoom = 14;
            gmap.Position = new PointLatLng(10.7626, 106.6601);
            gmap.ShowCenter = false;
            gmap.CanDragMap = true;
            gmap.DragButton = MouseButtons.Left;
            gmap.MarkersEnabled = true;
            gmap.RoutesEnabled = true;

            foreach (var o in new[] { routeOverlay, driverRouteOverlay, poiOverlay,
                                      driverOverlay, pickupOverlay, dropoffOverlay })
                gmap.Overlays.Add(o);

            gmap.OnMapDrag += async () => { ClampMapToHcm(); await LoadPOI(gmap.Position); };
            gmap.OnMapZoomChanged += async () => { ClampMapToHcm(); await LoadPOI(gmap.Position); };

            gmap.MouseDown += OnMapMouseDown;
            gmap.MouseMove += OnMapMouseMove;
            gmap.MouseUp += OnMapMouseUp;

            Controls.Add(gmap);
        }

        // ── Mouse ─────────────────────────────────────────────────────────────
        private void OnMapMouseDown(object? s, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (e.Button != MouseButtons.Left) return;
                _isDragging = false;
                _mouseDownPos = new Point(e.X, e.Y);
            }
            else if (e.Button == MouseButtons.Right)
            {
                _isRightDragging = false;
                _rightMouseDownPos = new Point(e.X, e.Y);
            }
        }

        private void OnMapMouseMove(object? s, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (Math.Abs(e.X - _mouseDownPos.X) > 5 ||
                    Math.Abs(e.Y - _mouseDownPos.Y) > 5)
                    _isDragging = true;
            }
            else if (e.Button == MouseButtons.Right)
            {
                if (Math.Abs(e.X - _rightMouseDownPos.X) > 5 ||
                    Math.Abs(e.Y - _rightMouseDownPos.Y) > 5)
                    _isRightDragging = true;
            }
        }

        private void OnMapMouseUp(object? s, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                bool wasDrag = _isDragging;
                _isDragging = false;   // reset before async
                if (!wasDrag && AllowLeftClickSelect)
                    _ = HandleClickAsync(e.X, e.Y);
                return;
            }

            if (e.Button == MouseButtons.Right)
            {
                bool wasRightDrag = _isRightDragging;
                _isRightDragging = false;
                if (!wasRightDrag)
                    _ = HandleClickAsync(e.X, e.Y);
            }
        }

        private async Task HandleClickAsync(int x, int y)
        {
            try
            {
                var point = ClampPoint(gmap.FromLocalToLatLng(x, y));
                string addr = await GetAddressFromPoint(point);
                bool isPickup = _isPickupSelector?.Invoke()
                    ?? (_pickupPoint == default(PointLatLng));

                if (isPickup) SetPickupMarker(point);
                else await SetDropoffMarker(point);

                LocationSelected?.Invoke(point, addr, isPickup);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MapClick] {ex}");
            }
        }

        // ── Autocomplete ──────────────────────────────────────────────────────
        public async Task<List<DomainLocation>> GetSuggestions(string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 3)
                return new List<DomainLocation>();
            return await PhotonAutocomplete(query);
        }

        private async Task<List<DomainLocation>> PhotonAutocomplete(string query)
        {
            var result = new List<DomainLocation>();
            try
            {
                string url = $"https://photon.komoot.io/api/?q={Uri.EscapeDataString(query)}&limit=10&lang=vi";
                var response = await _http.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    if (!IsHtml(content))
                    {
                        ParsePhotonResults(content, result);
                        return result;
                    }
                }

                // Fallback: Nominatim
                string nomUrl = $"https://nominatim.openstreetmap.org/search" +
                    $"?q={Uri.EscapeDataString(query)}&format=json&limit=10&addressdetails=1&accept-language=vi";
                var nomContent = await RateLimitedNominatimGet(nomUrl);
                if (nomContent != null && !IsHtml(nomContent))
                    ParseNominatimResults(nomContent, result);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Photon] {ex.Message}");
            }
            return result;
        }

        private static void ParsePhotonResults(string content, List<DomainLocation> result)
        {
            var obj = JObject.Parse(content);
            foreach (var f in obj["features"] ?? new JArray())
            {
                var props = f["properties"];
                var coords = f["geometry"]?["coordinates"]; // [lon, lat]
                string name = props?["name"]?.ToString() ?? "Không xác định";
                if (string.IsNullOrWhiteSpace(name)) name = "Không xác định";

                var parts = new List<string>();
                foreach (var k in new[] { "housenumber", "street", "district", "city" })
                    if (props?[k] != null) parts.Add(props[k]!.ToString()!);
                string addr = parts.Count > 0 ? string.Join(", ", parts) : name;

                double lat = coords?[1] != null ? (double)coords[1]! : 0;
                double lng = coords?[0] != null ? (double)coords[0]! : 0;
                if (lat != 0 && lng != 0)
                    result.Add(new DomainLocation(name, addr, lat, lng));
            }
        }

        private static void ParseNominatimResults(string content, List<DomainLocation> result)
        {
            var arr = JArray.Parse(content);
            foreach (var item in arr)
            {
                string name = item["display_name"]?.ToString() ?? "Unknown";
                if (name.Length > 60) name = name[..60];
                string addr = item["address"]?["road"]?.ToString()
                           ?? item["address"]?["city"]?.ToString()
                           ?? name;
                if (!double.TryParse(item["lat"]?.ToString(), out double lat)) continue;
                if (!double.TryParse(item["lon"]?.ToString(), out double lng)) continue;
                result.Add(new DomainLocation(name, addr, lat, lng));
            }
        }

        // ── SelectLocation ────────────────────────────────────────────────────
        public async Task<PointLatLng> SelectLocation(DomainLocation location, bool isPickup)
        {
            var point = new PointLatLng(location.Lat, location.Lng);
            if (isPickup) SetPickupMarker(point);
            else await SetDropoffMarker(point);
            gmap.Position = point;
            gmap.Zoom = 16;
            return point;
        }

        public async Task<PointLatLng?> SelectAddress(string address, bool isPickup)
        {
            var point = await NominatimGeocode(address);
            if (point != null)
            {
                if (isPickup) SetPickupMarker(point.Value);
                else await SetDropoffMarker(point.Value);
                gmap.Position = point.Value;
                gmap.Zoom = 16;
            }
            return point;
        }

        // ── Markers ───────────────────────────────────────────────────────────
        public void SetPickupMarker(PointLatLng point)
        {
            pickupOverlay.Markers.Clear();
            _pickupMarker = new GMarkerGoogle(point, GMarkerGoogleType.green_dot)
            { ToolTipText = "Điểm đón" };
            pickupOverlay.Markers.Add(_pickupMarker);
            _pickupPoint = point;
            poiOverlay.IsVisibile = false;
            gmap.Refresh();
        }

        public async Task SetDropoffMarker(PointLatLng point)
        {
            dropoffOverlay.Markers.Clear();
            _dropoffMarker = new GMarkerGoogle(point, GMarkerGoogleType.red_dot);
            ApplyCustomTooltip(_dropoffMarker, "Đang xác định địa chỉ...");
            dropoffOverlay.Markers.Add(_dropoffMarker);
            _dropoffPoint = point;

            string addr = await GetAddressFromPoint(point);
            if (_dropoffMarker != null) _dropoffMarker.ToolTipText = addr;

            if (_pickupPoint != default)
                await DrawTripRouteAsync(_pickupPoint, point);
        }

        // ── Routes ────────────────────────────────────────────────────────────
        public async Task DrawTripRouteAsync(PointLatLng pickup, PointLatLng dropoff)
        {
            var pts = await _routeService.GetRoutePointsAsync(ToLoc(pickup), ToLoc(dropoff));
            if (pts.Count < 2) { DrawStraightLine(routeOverlay, pickup, dropoff); return; }
            DrawRoute(routeOverlay, pts.ToList(), Color.FromArgb(180, 0, 120, 255), 5f, "TripRoute");
        }

        public async Task DrawDriverToPickupRouteAsync(PointLatLng driverPos, PointLatLng pickup)
        {
            var pts = await _routeService.GetRoutePointsAsync(ToLoc(driverPos), ToLoc(pickup));
            if (pts.Count < 2) { DrawStraightLine(driverRouteOverlay, driverPos, pickup); return; }
            DrawRoute(driverRouteOverlay, pts.ToList(), Color.FromArgb(200, 0, 160, 60), 3f, "DriverRoute");
        }

        public void ClearDriverRoute()
        {
            if (InvokeRequired) { BeginInvoke(ClearDriverRoute); return; }
            ClearOverlayRoutes(driverRouteOverlay);
            gmap.Refresh();
        }

        private void DrawRoute(GMapOverlay overlay, List<DomainLocation> locs,
            Color color, float width, string name)
        {
            if (InvokeRequired)
            {
                BeginInvoke(() => DrawRoute(overlay, locs, color, width, name));
                return;
            }
            var pts = locs.Select(l => new PointLatLng(l.Lat, l.Lng)).ToList();
            ClearOverlayRoutes(overlay);
            overlay.Routes.Add(new GMapRoute(pts, name) { Stroke = new Pen(color, width) });
            gmap.Refresh();
        }

        private void DrawStraightLine(GMapOverlay overlay, PointLatLng a, PointLatLng b)
        {
            if (InvokeRequired) { BeginInvoke(() => DrawStraightLine(overlay, a, b)); return; }
            ClearOverlayRoutes(overlay);
            overlay.Routes.Add(new GMapRoute(new List<PointLatLng> { a, b }, "Fallback")
            {
                Stroke = new Pen(Color.FromArgb(100, Color.Gray), 2)
                { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash }
            });
            gmap.Refresh();
        }

        private static void ClearOverlayRoutes(GMapOverlay overlay)
        {
            foreach (var r in overlay.Routes) r.Stroke?.Dispose();
            overlay.Routes.Clear();
        }

        public void ZoomToRoute()
        {
            if (InvokeRequired) { BeginInvoke(ZoomToRoute); return; }
            if (routeOverlay.Routes.Count > 0)
                gmap.ZoomAndCenterRoute(routeOverlay.Routes[0]);
        }

        // ── Geofence ──────────────────────────────────────────────────────────
        public Task<bool> IsDriverNearPickup(PointLatLng driverPos, PointLatLng pickup,
            double radiusKm = 0.05) =>
            _routeService.IsNearAsync(ToLoc(driverPos), ToLoc(pickup), radiusKm);

        // ── Driver markers + animation ────────────────────────────────────────
        public void UpdateDriverLocation(Guid driverId, PointLatLng pos)
        {
            if (InvokeRequired) { BeginInvoke(() => UpdateDriverLocation(driverId, pos)); return; }
            AnimateDriver(driverId, pos);
        }

        public void UpdateNearbyDrivers(IEnumerable<Driver> drivers)
        {
            if (InvokeRequired) { BeginInvoke(() => UpdateNearbyDrivers(drivers)); return; }

            var idSet = new HashSet<string>(drivers.Select(d => d.Id.ToString()));

            foreach (var key in _driverMarkers.Keys.ToList())
            {
                if (idSet.Contains(key)) continue;
                if (_animTimers.TryGetValue(key, out var t)) { t.Stop(); t.Dispose(); _animTimers.Remove(key); }
                if (_driverMarkers.TryGetValue(key, out var m)) { driverOverlay.Markers.Remove(m); _driverMarkers.Remove(key); }
            }

            foreach (var d in drivers)
            {
                if (d.Position == null) continue;
                string key = d.Id.ToString();
                string vehicleType = d.Vehicle != null ? (d.Vehicle.GetVehicleType() == VehicleType.Motorbike ? "Xe máy" : "Ô tô") : "N/A";
                string tip = $"Tài xế: {d.Name}\nXe: {vehicleType}\n⭐ {d.AverageRating:F1}";

                if (!_driverMarkers.ContainsKey(key))
                {
                    var m = new GMarkerGoogle(
                        new PointLatLng(d.Position.Lat, d.Position.Lng),
                        GMarkerGoogleType.blue_dot)
                    { ToolTipText = tip, ToolTipMode = MarkerTooltipMode.OnMouseOver };
                    _driverMarkers[key] = m;
                    driverOverlay.Markers.Add(m);
                }
                else _driverMarkers[key].ToolTipText = tip;

                UpdateDriverLocation(d.Id, new PointLatLng(d.Position.Lat, d.Position.Lng));
            }
        }

        private void AnimateDriver(Guid id, PointLatLng end)
        {
            string key = id.ToString();
            if (!_driverMarkers.ContainsKey(key))
            {
                var m = new GMarkerGoogle(end, GMarkerGoogleType.blue_dot)
                { ToolTipText = "Tài xế đang đến", ToolTipMode = MarkerTooltipMode.OnMouseOver };
                _driverMarkers[key] = m;
                driverOverlay.Markers.Add(m);
                return;
            }

            var marker = _driverMarkers[key];
            var start = marker.Position;

            if (_animTimers.TryGetValue(key, out var old)) { old.Stop(); old.Dispose(); _animTimers.Remove(key); }

            int step = 0;
            var timer = new System.Windows.Forms.Timer { Interval = AnimInterval };
            timer.Tick += (_, _) =>
            {
                step++;
                if (step <= AnimSteps)
                {
                    double t = (double)step / AnimSteps;
                    marker.Position = new PointLatLng(
                        start.Lat + (end.Lat - start.Lat) * t,
                        start.Lng + (end.Lng - start.Lng) * t);
                }
                else
                {
                    marker.Position = end;
                    timer.Stop(); timer.Dispose();
                    _animTimers.Remove(key);
                }
            };
            _animTimers[key] = timer;
            timer.Start();
        }

        // ── POI ───────────────────────────────────────────────────────────────
        private async Task LoadPOI(PointLatLng pos)
        {
            if (_poiLoading || gmap.Zoom < 14) return;
            if ((DateTime.UtcNow - _lastPoiLoad).TotalSeconds < 5) return;

            _poiLoading = true;
            _lastPoiLoad = DateTime.UtcNow;

            if (InvokeRequired) BeginInvoke(() => poiOverlay.Markers.Clear());
            else poiOverlay.Markers.Clear();

            var rect = gmap.ViewArea;
            string q = $"[out:json];\nnode[\"amenity\"]({rect.Bottom},{rect.Left},{rect.Top},{rect.Right});\nout;";
            try
            {
                var res = await _http.PostAsync(
                    "https://overpass.kumi.systems/api/interpreter",
                    new FormUrlEncodedContent(new Dictionary<string, string> { { "data", q } }));
                if (!res.IsSuccessStatusCode) return;

                var content = await res.Content.ReadAsStringAsync();
                if (IsHtml(content)) return;

                var elements = JObject.Parse(content)["elements"]!;
                var markers = new List<GMarkerGoogle>();
                foreach (var el in elements)
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
                    markers.Add(new GMarkerGoogle(
                        new PointLatLng((double)el["lat"]!, (double)el["lon"]!), icon)
                    { ToolTipText = el["tags"]?["name"]?.ToString() ?? "POI" });
                }
                if (InvokeRequired)
                    BeginInvoke(() => { foreach (var m in markers) poiOverlay.Markers.Add(m); });
                else
                    foreach (var m in markers) poiOverlay.Markers.Add(m);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LoadPOI] {ex.Message}");
            }
            finally { _poiLoading = false; }
        }

        // ── Geocoding ─────────────────────────────────────────────────────────
        public async Task<PointLatLng?> NominatimGeocode(string address)
        {
            address = address.Trim();
            try
            {
                string url = $"https://nominatim.openstreetmap.org/search" +
                                 $"?q={Uri.EscapeDataString(address)}&format=json&limit=1";
                var content = await RateLimitedNominatimGet(url) ?? "[]";
                if (IsHtml(content)) return null;
                var arr = JArray.Parse(content);
                if (arr.Count == 0) return null;
                return new PointLatLng(
                    double.Parse(arr[0]["lat"]!.ToString(), System.Globalization.CultureInfo.InvariantCulture),
                    double.Parse(arr[0]["lon"]!.ToString(), System.Globalization.CultureInfo.InvariantCulture));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Geocode] {ex.Message}");
                return null;
            }
        }

        public async Task<string> GetAddressFromPoint(PointLatLng point)
        {
            string key = $"{point.Lat:F5},{point.Lng:F5}";
            if (_reverseCache.TryGetValue(key, out var cached)) return cached;

            try
            {
                string url = $"https://nominatim.openstreetmap.org/reverse" +
                                 $"?lat={point.Lat}&lon={point.Lng}&format=json&addressdetails=1";
                var content = await RateLimitedNominatimGet(url) ?? "{}";
                if (IsHtml(content)) return $"{point.Lat:F4}, {point.Lng:F4}";
                var json = JObject.Parse(content);
                string addr = json["display_name"]?.ToString() ?? $"{point.Lat:F4}, {point.Lng:F4}";

                if (_reverseCache.Count >= MaxCacheSize)
                    _reverseCache.Remove(_reverseCache.Keys.First());
                _reverseCache[key] = addr;
                return addr;
            }
            catch { return $"{point.Lat:F4}, {point.Lng:F4}"; }
        }

        public async Task<string> ZoomToMyLocation()
        {
            try
            {
                var res = await _http.GetAsync("https://ipapi.co/json/");
                if (res.IsSuccessStatusCode)
                {
                    var obj = JObject.Parse(await res.Content.ReadAsStringAsync());
                    if (obj["latitude"] != null)
                    {
                        var pt = ClampPoint(new PointLatLng((double)obj["latitude"]!, (double)obj["longitude"]!));
                        gmap.Position = pt; gmap.Zoom = 15;
                        SetPickupMarker(pt);
                        return await GetAddressFromPoint(pt);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ZoomToMyLocation] {ex.Message}");
            }

            var def = new PointLatLng(10.7769, 106.7009);
            gmap.Position = def; gmap.Zoom = 15;
            SetPickupMarker(def);
            return "TP. Hồ Chí Minh (Mặc định)";
        }

        private static async Task<string?> RateLimitedNominatimGet(string url)
        {
            await _nominatimSem.WaitAsync();
            try
            {
                var elapsed = (DateTime.UtcNow - _lastNominatimCall).TotalMilliseconds;
                if (elapsed < NominatimIntervalMs)
                    await Task.Delay((int)(NominatimIntervalMs - elapsed));
                _lastNominatimCall = DateTime.UtcNow;

                // Create a temporary client with the required User-Agent for Nominatim
                using var tempClient = new HttpClient();
                tempClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 OPPApp/1.0");
                return await tempClient.GetStringAsync(url);
            }
            finally { _nominatimSem.Release(); }
        }

        // ── Clear ─────────────────────────────────────────────────────────────
        public void ClearRoute()
        {
            foreach (var t in _animTimers.Values.ToArray()) { t.Stop(); t.Dispose(); }
            _animTimers.Clear();

            ClearOverlayRoutes(routeOverlay);
            ClearOverlayRoutes(driverRouteOverlay);
            dropoffOverlay.Markers.Clear();
            pickupOverlay.Markers.Clear();
            driverOverlay.Markers.Clear();
            poiOverlay.Markers.Clear();

            _dropoffPoint = null;
            _pickupPoint = default;
            _dropoffMarker = null;
            _pickupMarker = null;
            _driverMarkers.Clear();
            gmap?.Refresh();
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private static DomainLocation ToLoc(PointLatLng p) =>
            new("Point", "Point", p.Lat, p.Lng);

        private static PointLatLng ClampPoint(PointLatLng p) => new(
            Math.Clamp(p.Lat, HcmMinLat, HcmMaxLat),
            Math.Clamp(p.Lng, HcmMinLng, HcmMaxLng));

        private void ClampMapToHcm()
        {
            var c = ClampPoint(gmap.Position);
            if (Math.Abs(c.Lat - gmap.Position.Lat) > 0.0001 ||
                Math.Abs(c.Lng - gmap.Position.Lng) > 0.0001)
                gmap.Position = c;
        }

        private static bool IsHtml(string s) =>
            s.TrimStart().StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase) ||
            s.TrimStart().StartsWith("<html", StringComparison.OrdinalIgnoreCase);

        private static void ApplyCustomTooltip(GMapMarker marker, string text)
        {
            marker.ToolTipText = text;
            marker.ToolTipMode = MarkerTooltipMode.OnMouseOver;
            marker.ToolTip = new GMapToolTip(marker)
            {
                Fill = _tooltipFill,
                Foreground = Brushes.White,
                Font = _tooltipFont,
                Stroke = _tooltipStroke,
                Offset = new Point(10, -25)
            };
        }

        // ── Dispose ───────────────────────────────────────────────────────────
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var t in _animTimers.Values.ToArray()) { t.Stop(); t.Dispose(); }
                _animTimers.Clear();
                ClearOverlayRoutes(routeOverlay);
                ClearOverlayRoutes(driverRouteOverlay);
            }
            base.Dispose(disposing);
        }
    }
}