using OOP.Domain.Entities;

namespace OOP.Infrastructure.Map
{
    public class MapRouteProvider : IMapRouteProvider
    {
        public async Task<MapRouteResult?> GetRouteAsync(Location start, Location end)
        {
            await Task.Delay(10);

            // 1. Khởi tạo điểm rẽ bằng constructor có tham số để tránh lỗi protection level
            var turnPoint = new Location("Rẽ", "Điểm rẽ mô phỏng", start.Lat, end.Lng);

            // 2. Tính khoảng cách lộ trình (tổng 2 cạnh góc vuông)
            // Khoảng cách này luôn >= khoảng cách đường thẳng
            double distLat = Math.Abs(start.Lat - turnPoint.Lat) * 111;
            double distLng = Math.Abs(turnPoint.Lng - end.Lng) * 111;

            // Cộng thêm 10% hệ số uốn lượn để thực tế hơn
            double routeDistance = (distLat + distLng) * 1.1;

            return new MapRouteResult
            {
                Distance = routeDistance,
                Duration = (int)(routeDistance / 30 * 3600), // Giả lập 30km/h
                Points = new List<Location> { start, turnPoint, end }
            };
        }
    }
}