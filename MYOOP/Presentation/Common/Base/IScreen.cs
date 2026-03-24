namespace OOP.Presentation.Base
{
    /// <summary>
    /// Contract tất cả "màn hình" (UserControl) phải thực hiện.
    /// Shell gọi các method này trong lifecycle navigation.
    /// </summary>
    public interface IScreen
    {
        /// <summary>Tiêu đề hiển thị trên header của shell.</summary>
        string ScreenTitle { get; }

        /// <summary>
        /// Được gọi MỖI LẦN screen này trở thành active.
        /// Dùng để refresh data, reset state, nhận parameter từ navigate.
        /// </summary>
        /// <param name="parameter">Optional data từ caller (vd: tripId, userId…)</param>
        Task OnNavigatedTo(object? parameter = null);

        /// <summary>
        /// Được gọi TRƯỚC KHI rời khỏi screen này.
        /// Trả về false để block navigation (vd: đang xử lý chuyến đi).
        /// </summary>
        bool OnNavigatingFrom();
    }
}