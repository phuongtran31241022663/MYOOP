﻿using GMap.NET;
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
        private const int AnimationInterval = 50; // ms

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

        // ── Geocode cache ─────────────────────────────────────────────────────
        private readonly Dictionary<string, string> reverseCache = new();
        private const int MaxCacheSize = 500;

        // ── Static GDI resources for tooltip (app-lifetime, never leaked) ─────
        private static readonly SolidBrush TooltipFillBrush = new(Color.FromArgb(230, 40, 40, 40));
        private static readonly Font TooltipFont = new("Segoe UI", 9, FontStyle.Bold);
        private static readonly Pen TooltipStrokePen = new(Color.White, 1);

        // ── Events ────────────────────────────────────────────────────────────
        public event Action<PointLatLng, string, bool>? LocationSelected;
        private Func<bool>? _isPickupSelector;

        public void SetPickupSelector(Func<bool> selector)
        {
            _isPickupSelector = selector;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Static init
        // ─────────────────────────────────────────────────────────────────────
        public static void InitializeMapProvider()
        {
            GMapProvider.UserAgent =
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
                "AppleWebKit/537.36 (KHTML, like Gecko) " +
                "Chrome/124.0.0.0 Safari/537.36";
            GMaps.Instance.Mode = AccessMode.ServerAndCache;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Constructor
        // ─────────────────────────────────────────────────────────────────────
        public MapControl(HttpClient http, IRouteService routeService)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _routeService = routeService ?? throw new ArgumentNullException(nameof(routeService));

            if (!_http.DefaultRequestHeaders.Contains("User-Agent"))
                _http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 RideGoApp/1.0");

            TopLevel = false;
            FormBorderStyle = FormBorderStyle.None;
            InitMap();
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
            // Right-click handled in MapMouseUp → HandleRightClickAsync.
            // MapMouseClick is NOT registered — GMap fires MouseClick before
            // MouseUp in some internal WndProc paths, causing double-fire.

            Controls.Add(gmap);
        }

        // ═════════════════════════════════════════════════════════════════════
        // Autocomplete / suggestions
        // ═════════════════════════════════════════════════════════════════════
        private async Task<List<DomainLocation>> PhotonAutocomplete(string query)
        {
            var result = new List<DomainLocation>();
            try
            {
                string url = $"https://photon.komoot.io/api/?q={Uri.EscapeDataString(query)}" +
                             $"&limit=10&lang=vi&lat=10.7626&lon=106.6601";
                var json = JObject.Parse(await _http.GetStringAsync(url));

                foreach (var f in json["features"]!)
                {
                    var props = f["properties"];
                    var coords = f["geometry"]?["coordinates"]; // [lon, lat]

                    string name = props?["name"]?.ToString() ?? "Không xác định";
                    if (string.IsNullOrWhiteSpace(name)) name = "Không xác định";

                    var addrParts = new List<string>();
                    if (props?["housenumber"] != null) addrParts.Add(props["housenumber"]!.ToString());
                    if (props?["street"] != null) addrParts.Add(props["street"]!.ToString());
                    if (props?["district"] != null) addrParts.Add(props["district"]!.ToString());
                    if (props?["city"] != null) addrParts.Add(props["city"]!.ToString());

                    // Location constructor validates non-empty name/address
                    string address = addrParts.Count > 0
                        ? string.Join(", ", addrParts)
                        : name; // fallback: use name so address is never empty

                    double lat = coords != null ? (double)coords[1]! : 0;
                    double lng = coords != null ? (double)coords[0]! : 0;

                    result.Add(new DomainLocation(name, address, lat, lng));
                }
            }
            catch { /* Log error if needed */ }
            return result;
        }

        public async Task<List<DomainLocation>> GetSuggestions(string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 3)
                return new List<DomainLocation>();

            return await PhotonAutocomplete(query);
        }

        /// <summary>
        /// Đặt marker từ DomainLocation đã có toạ độ (từ Photon) — không cần geocode lại.
        /// </summary>
        public async Task<PointLatLng> SelectLocation(DomainLocation location, bool isPickup)
        {
            var point = new PointLatLng(location.Lat, location.Lng);
            if (isPickup) SetPickupMarker(point);
            else await SetDropoffMarker(point);

            gmap.Position = point;
            gmap.Zoom = 16;
            return point;
        }

        /// <summary>Fallback: geocode từ chuỗi địa chỉ khi không có toạ độ sẵn.</summary>
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

        // ═════════════════════════════════════════════════════════════════════
        // Mouse drag detection
        // ═════════════════════════════════════════════════════════════════════
        private void MapMouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

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
            if (e.Button == MouseButtons.Left)
            {
                wasDragging = dragging;
                dragging = false;
                if (!wasDragging)
                    _ = HandleLeftClickAsync(e.X, e.Y);
                return;
            }

            if (e.Button == MouseButtons.Right)
            {
                if (!wasDragging)
                    _ = HandleRightClickAsync(e.X, e.Y);
            }
        }
        private async Task HandleLeftClickAsync(int x, int y)
        {
            try
            {
                PointLatLng point = gmap.FromLocalToLatLng(x, y);
                string address = await GetAddressFromPoint(point);
                bool isPickup = _isPickupSelector?.Invoke() ?? (pickupPoint == default(PointLatLng));

                if (isPickup) SetPickupMarker(point);
                else await SetDropoffMarker(point);

                LocationSelected?.Invoke(point, address, isPickup);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HandleLeftClickAsync] {ex}");
            }
        }
        private async Task HandleRightClickAsync(int x, int y)
        {
            try
            {
                PointLatLng point = gmap.FromLocalToLatLng(x, y);
                string address = await GetAddressFromPoint(point);
                bool isPickup = _isPickupSelector?.Invoke() ?? (pickupPoint == default(PointLatLng));

                if (isPickup) SetPickupMarker(point);
                else await SetDropoffMarker(point);

                LocationSelected?.Invoke(point, address, isPickup);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HandleRightClickAsync] {ex}");
            }
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
        // Route drawing
        // ═════════════════════════════════════════════════════════════════════
        public async Task DrawTripRouteAsync(PointLatLng pickup, PointLatLng dropoff)
        {
            var points = await _routeService.GetRoutePointsAsync(
                ToLocation(pickup), ToLocation(dropoff));

            if (points.Count < 2) { DrawStraightLine(routeOverlay, pickup, dropoff); return; }

            DrawRouteOnOverlay(routeOverlay, points, Color.FromArgb(180, 0, 120, 255), 5f, "TripRoute");
        }

        public async Task DrawDriverToPickupRouteAsync(PointLatLng driverPos, PointLatLng pickup)
        {
            var points = await _routeService.GetRoutePointsAsync(
                ToLocation(driverPos), ToLocation(pickup));

            if (points.Count < 2) { DrawStraightLine(driverRouteOverlay, driverPos, pickup); return; }

            DrawRouteOnOverlay(driverRouteOverlay, points, Color.FromArgb(200, 0, 160, 60), 3f, "DriverRoute");
        }

        public void ClearDriverRoute()
        {
            if (InvokeRequired) { BeginInvoke(ClearDriverRoute); return; }
            ClearOverlayRoutes(driverRouteOverlay);
            gmap.Refresh();
        }

        private void DrawRouteOnOverlay(
            GMapOverlay overlay,
            List<DomainLocation> locations,
            Color color,
            float width,
            string name)
        {
            if (InvokeRequired)
            {
                BeginInvoke(() => DrawRouteOnOverlay(overlay, locations, color, width, name));
                return;
            }

            var pts = locations.Select(ToPointLatLng).ToList();
            ClearOverlayRoutes(overlay);
            overlay.Routes.Add(new GMapRoute(pts, name) { Stroke = new Pen(color, width) });
            gmap.Refresh();
        }

        private void DrawStraightLine(GMapOverlay overlay, PointLatLng start, PointLatLng end)
        {
            if (InvokeRequired) { BeginInvoke(() => DrawStraightLine(overlay, start, end)); return; }

            ClearOverlayRoutes(overlay);
            overlay.Routes.Add(new GMapRoute(new List<PointLatLng> { start, end }, "Fallback")
            {
                Stroke = new Pen(Color.FromArgb(100, Color.Gray), 2)
                { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash }
            });
            gmap.Refresh();
        }

        private static void ClearOverlayRoutes(GMapOverlay overlay)
        {
            foreach (var route in overlay.Routes)
                route.Stroke?.Dispose();
            overlay.Routes.Clear();
        }

        public void ZoomToRoute()
        {
            if (InvokeRequired) { BeginInvoke(ZoomToRoute); return; }
            if (routeOverlay.Routes.Count > 0)
                gmap.ZoomAndCenterRoute(routeOverlay.Routes[0]);
        }

        // ═════════════════════════════════════════════════════════════════════
        // Geofence
        // ═════════════════════════════════════════════════════════════════════
        public async Task<bool> IsDriverNearPickup(
            PointLatLng driverPos,
            PointLatLng pickup,
            double radiusKm = 0.05)
        {
            return await _routeService.IsNearAsync(
                ToLocation(driverPos), ToLocation(pickup), radiusKm);
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
            if ((DateTime.UtcNow - lastPoiLoad).TotalSeconds < 5) return;

            poiLoading = true;
            lastPoiLoad = DateTime.UtcNow;

            if (InvokeRequired) BeginInvoke(() => poiOverlay.Markers.Clear());
            else poiOverlay.Markers.Clear();

            var rect = gmap.ViewArea;
            string q = $"[out:json];\nnode[\"amenity\"]" +
                          $"({rect.Bottom},{rect.Left},{rect.Top},{rect.Right});\nout;";
            try
            {
                var res = await _http.PostAsync(
                    "https://overpass.kumi.systems/api/interpreter",
                    new FormUrlEncodedContent(new Dictionary<string, string> { { "data", q } }));
                if (!res.IsSuccessStatusCode) return;

                var elements = JObject.Parse(await res.Content.ReadAsStringAsync())["elements"]!;
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
            catch { }
            finally { poiLoading = false; }
        }

        // ═════════════════════════════════════════════════════════════════════
        // Geocoding
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

                if (reverseCache.Count >= MaxCacheSize)
                {
                    var first = reverseCache.Keys.First();
                    reverseCache.Remove(first);
                }
                reverseCache[key] = addr;
                return addr;
            }
            catch { return $"{point.Lat:F4}, {point.Lng:F4}"; }
        }

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
            foreach (var t in timers) { t.Stop(); t.Dispose(); }
            animationTimers.Clear();

            ClearOverlayRoutes(routeOverlay);
            ClearOverlayRoutes(driverRouteOverlay);
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

        private static DomainLocation ToLocation(PointLatLng p)
        {
            var loc = new DomainLocation();
            loc.Name = "Point";
            loc.Address = "Point";
            loc.Lat = p.Lat;
            loc.Lng = p.Lng;
            return loc;
        }

        private static PointLatLng ToPointLatLng(DomainLocation l) =>
            new(l.Lat, l.Lng);

        // ═════════════════════════════════════════════════════════════════════
        // Misc helpers
        // ═════════════════════════════════════════════════════════════════════
        private static void ApplyCustomTooltip(GMapMarker marker, string text)
        {
            marker.ToolTipText = text;
            marker.ToolTipMode = MarkerTooltipMode.OnMouseOver;
            marker.ToolTip = new GMapToolTip(marker)
            {
                Fill = TooltipFillBrush,
                Foreground = Brushes.White,
                Font = TooltipFont,
                Stroke = TooltipStrokePen,
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
                var timers = animationTimers.Values.ToArray();
                foreach (var t in timers) { t.Stop(); t.Dispose(); }
                animationTimers.Clear();

                ClearOverlayRoutes(routeOverlay);
                ClearOverlayRoutes(driverRouteOverlay);
            }
            base.Dispose(disposing);
        }
    }
}


