using OOP.Presentation.Base;

namespace OOP.Presentation.Common.Theme
{
    /// <summary>
    /// Engine điều phối chuyển màn hình bên trong một Panel host.
    /// Chỉ một screen visible tại một thời điểm; các screen khác hidden (không dispose).
    /// Nhờ vậy state được giữ nguyên khi quay lại.
    /// </summary>
    public class ScreenNavigator
    {
        // ── Internal state ────────────────────────────────────────────────────
        private readonly Panel _host;
        private readonly Dictionary<string, UserControl> _screens = new();
        private UserControl? _current;
        private string? _currentKey;

        // ── Events ────────────────────────────────────────────────────────────
        /// <summary>Fired sau khi chuyển màn hình thành công. Tham số là key mới.</summary>
        public event Action<string>? ScreenChanged;

        // ─────────────────────────────────────────────────────────────────────
        public ScreenNavigator(Panel host)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Key của screen hiện tại (null nếu chưa navigate lần nào).</summary>
        public string? CurrentKey => _currentKey;

        /// <summary>
        /// Đăng ký một screen với key. Gọi trong constructor của Shell,
        /// trước khi gọi NavigateTo lần đầu.
        /// </summary>
        public void Register(string key, UserControl screen)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key không được rỗng.", nameof(key));
            if (screen == null) throw new ArgumentNullException(nameof(screen));

            screen.Dock = DockStyle.Fill;
            screen.Visible = false;
            _host.Controls.Add(screen);
            _screens[key] = screen;
        }

        /// <summary>
        /// Điều hướng tới screen với key cho trước.
        /// Nếu screen hiện tại block (OnNavigatingFrom trả false) thì không chuyển.
        /// </summary>
        /// <param name="key">Key đã đăng ký qua Register().</param>
        /// <param name="parameter">Dữ liệu truyền sang screen đích.</param>
        /// <returns>true nếu chuyển thành công.</returns>
        public async Task<bool> NavigateTo(string key, object? parameter = null)
        {
            if (!_screens.TryGetValue(key, out var next))
                throw new KeyNotFoundException($"Screen '{key}' chưa được đăng ký.");

            // Đã ở screen này rồi → vẫn gọi OnNavigatedTo để refresh
            bool isSameScreen = (_current == next);

            // Hỏi screen hiện tại có cho phép rời không
            if (!isSameScreen && _current is IScreen outgoing && !outgoing.OnNavigatingFrom())
                return false;

            // Ẩn screen cũ
            if (_current != null && !isSameScreen)
                _current.Visible = false;

            // Hiển thị screen mới
            _current = next;
            _currentKey = key;
            _current.Visible = true;
            _current.BringToFront();

            // Thông báo cho screen mới
            if (next is IScreen incoming)
                await incoming.OnNavigatedTo(parameter);

            ScreenChanged?.Invoke(key);
            return true;
        }

        /// <summary>Lấy screen đã đăng ký theo key, cast sang type T.</summary>
        public T? Get<T>(string key) where T : UserControl
        {
            return _screens.TryGetValue(key, out var s) ? s as T : null;
        }
    }
}