using System.Text.Json;
using OOP.Domain.Entities;

namespace OOP.Infrastructure.Map
{
    /// <summary>
    /// Reverse geocoding dùng Nominatim.
    /// Chiến lược: tìm POI gần nhất trong bán kính <see cref="PoiRadiusMeters"/> trước.
    /// Nếu không có POI, fallback về địa chỉ đường của tọa độ đó.
    /// </summary>
    public class ReverseGeocoder
    {
        private readonly HttpClient _http;

        /// <summary>Bán kính tìm POI (mét). Mặc định 80m — đủ gần để có nghĩa.</summary>
        public int PoiRadiusMeters { get; set; } = 80;

        // Nominatim OSM class/type coi là "POI có địa danh"
        // Tham khảo: https://wiki.openstreetmap.org/wiki/Map_features
        private static readonly HashSet<string> PoiClasses = new(StringComparer.OrdinalIgnoreCase)
        {
            "amenity",      // trường học, bệnh viện, quán ăn, quán cà phê, ngân hàng, ...
            "shop",         // cửa hàng, siêu thị, chợ, ...
            "tourism",      // khách sạn, điểm tham quan, ...
            "leisure",      // công viên, sân thể thao, ...
            "office",       // văn phòng, tòa nhà công ty
            "building",     // tòa nhà có tên
            "place",        // khu dân cư có tên
        };

        public ReverseGeocoder(HttpClient http)
        {
            _http = http;
        }

        /// <summary>
        /// Từ tọa độ → GeoLocation với tên địa danh ưu tiên (nếu có), địa chỉ chi tiết.
        /// </summary>
        public async Task<GeoLocation> ReverseAsync(double lat, double lng)
        {
            // --- Bước 1: Tìm POI gần nhất trong bán kính PoiRadiusMeters ---
            var poi = await FindNearbyPoiAsync(lat, lng);
            if (poi != null)
                return poi;

            // --- Bước 2: Fallback — lấy địa chỉ đường tại tọa độ ---
            return await ReverseAddressAsync(lat, lng);
        }

        // ────────────────────────────────────────────────────────────────────
        // Step 1 — POI search via Nominatim /search with viewbox
        // ────────────────────────────────────────────────────────────────────
        private async Task<GeoLocation?> FindNearbyPoiAsync(double lat, double lng)
        {
            // Tính viewbox nhỏ quanh điểm click (~PoiRadiusMeters mét mỗi chiều)
            double delta = MetersToDecimalDegrees(PoiRadiusMeters, lat);
            double minLat = lat - delta;
            double maxLat = lat + delta;
            double minLng = lng - delta;
            double maxLng = lng + delta;

            string url = $"https://nominatim.openstreetmap.org/search" +
                         $"?format=jsonv2" +
                         $"&addressdetails=1" +
                         $"&namedetails=1" +
                         $"&limit=10" +
                         $"&viewbox={minLng:F6},{maxLat:F6},{maxLng:F6},{minLat:F6}" +
                         $"&bounded=1";

            try
            {
                var json = await _http.GetStringAsync(url);
                using var doc = JsonDocument.Parse(json);
                var results = doc.RootElement.EnumerateArray().ToList();

                if (results.Count == 0) return null;

                // Lọc những kết quả thuộc class POI và có tên
                var candidates = results
                    .Select(r => new
                    {
                        Element  = r,
                        Class    = r.TryGetProperty("class", out var c) ? c.GetString() ?? "" : "",
                        Name     = r.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
                        Lat      = double.TryParse(r.GetProperty("lat").GetString(), out var la) ? la : lat,
                        Lng      = double.TryParse(r.GetProperty("lon").GetString(), out var lo) ? lo : lng,
                        Distance = 0.0
                    })
                    .Where(x => PoiClasses.Contains(x.Class) && !string.IsNullOrWhiteSpace(x.Name))
                    .Select(x => x with { Distance = HaversineMeters(lat, lng, x.Lat, x.Lng) })
                    .OrderBy(x => x.Distance)
                    .FirstOrDefault();

                if (candidates == null) return null;

                string address = BuildAddress(candidates.Element);
                return new GeoLocation(candidates.Name, address, candidates.Lat, candidates.Lng);
            }
            catch
            {
                return null;    // network lỗi → fallback bình thường
            }
        }

        // ────────────────────────────────────────────────────────────────────
        // Step 2 — Reverse geocode address via Nominatim /reverse
        // ────────────────────────────────────────────────────────────────────
        private async Task<GeoLocation> ReverseAddressAsync(double lat, double lng)
        {
            string url = $"https://nominatim.openstreetmap.org/reverse" +
                         $"?format=jsonv2" +
                         $"&lat={lat:F6}" +
                         $"&lon={lng:F6}" +
                         $"&addressdetails=1" +
                         $"&zoom=18";

            try
            {
                var json = await _http.GetStringAsync(url);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                string displayName = root.TryGetProperty("display_name", out var dn)
                    ? dn.GetString() ?? $"{lat:F5}, {lng:F5}"
                    : $"{lat:F5}, {lng:F5}";

                // Tên ngắn = tên POI nếu có, không thì tên đường
                string name = ExtractShortName(root, displayName);
                string address = BuildAddress(root);

                return new GeoLocation(name, address, lat, lng);
            }
            catch
            {
                // Hoàn toàn offline — trả về tọa độ thô
                string fallback = $"{lat:F5}, {lng:F5}";
                return new GeoLocation(fallback, fallback, lat, lng);
            }
        }

        // ────────────────────────────────────────────────────────────────────
        // Helpers
        // ────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Ghép địa chỉ ngắn gọn theo thứ tự: số nhà + đường + phường/quận.
        /// </summary>
        private static string BuildAddress(JsonElement root)
        {
            if (!root.TryGetProperty("address", out var addr))
            {
                // jsonv2 search results have display_name
                return root.TryGetProperty("display_name", out var dn)
                    ? ShortenDisplayName(dn.GetString() ?? "")
                    : "";
            }

            var parts = new List<string>();

            foreach (var key in new[] { "house_number", "road", "suburb", "quarter",
                                         "city_district", "district", "city", "town", "village" })
            {
                if (addr.TryGetProperty(key, out var v))
                {
                    string val = v.GetString() ?? "";
                    if (!string.IsNullOrWhiteSpace(val))
                        parts.Add(val);

                    // Dừng lại ở cấp quận/huyện — đủ rõ cho ride-hailing
                    if (key is "district" or "city_district") break;
                }
            }

            return parts.Count > 0 ? string.Join(", ", parts) : "";
        }

        /// <summary>
        /// Lấy tên ngắn từ response: ưu tiên name của POI, fallback về tên đường.
        /// </summary>
        private static string ExtractShortName(JsonElement root, string fallback)
        {
            if (root.TryGetProperty("name", out var n) && !string.IsNullOrWhiteSpace(n.GetString()))
                return n.GetString()!;

            if (root.TryGetProperty("address", out var addr))
            {
                if (addr.TryGetProperty("road", out var road) && !string.IsNullOrWhiteSpace(road.GetString()))
                    return road.GetString()!;
            }

            return ShortenDisplayName(fallback);
        }

        /// <summary>Cắt display_name dài → lấy phần tử đầu tiên.</summary>
        private static string ShortenDisplayName(string displayName)
        {
            if (string.IsNullOrEmpty(displayName)) return displayName;
            var idx = displayName.IndexOf(',');
            return idx > 0 ? displayName[..idx].Trim() : displayName;
        }

        /// <summary>Chuyển mét → độ thập phân (xấp xỉ, đủ dùng cho bán kính nhỏ).</summary>
        private static double MetersToDecimalDegrees(double meters, double lat)
        {
            const double metersPerDegree = 111_320.0;
            return meters / (metersPerDegree * Math.Cos(lat * Math.PI / 180.0));
        }

        /// <summary>Khoảng cách Haversine giữa hai tọa độ (mét).</summary>
        private static double HaversineMeters(double lat1, double lng1, double lat2, double lng2)
        {
            const double R = 6_371_000.0;
            double dLat = (lat2 - lat1) * Math.PI / 180.0;
            double dLng = (lng2 - lng1) * Math.PI / 180.0;
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                     + Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0)
                     * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
            return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }
    }
}
