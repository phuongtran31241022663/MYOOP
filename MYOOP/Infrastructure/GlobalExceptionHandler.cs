using OOP.Infrastructure;
using System.Windows.Forms;

namespace OOP.Infrastructure
{
    /// <summary>
    /// Xử lý exception toàn cục cho ứng dụng.
    /// Bắt tất cả các exception không được xử lý và hiển thị thông báo lỗi chi tiết.
    /// </summary>
    public static class GlobalExceptionHandler
    {
        private static bool _isInitialized = false;
        private static string _logFilePath = "";
        
        /// <summary>
        /// Khởi tạo global exception handler
        /// </summary>
        public static void Initialize()
        {
            if (_isInitialized) return;
            
            _logFilePath = Logger.Instance.LogFilePath;
            
            // Xử lý exception trên UI thread
            System.Windows.Forms.Application.ThreadException += OnThreadException;
            
            // Xử lý exception trên non-UI thread
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            
            // Xử lý exception của Task
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
            
            _isInitialized = true;
            Logger.Instance.Info("GlobalExceptionHandler đã được khởi tạo");
        }
        
        /// <summary>
        /// Xử lý exception trên UI thread
        /// </summary>
        private static void OnThreadException(object sender, ThreadExceptionEventArgs e)
        {
            HandleException(e.Exception, "UI Thread Exception");
        }
        
        /// <summary>
        /// Xử lý exception không được xử lý trên non-UI thread
        /// </summary>
        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                HandleException(ex, "Unhandled Exception", isTerminating: e.IsTerminating);
            }
        }
        
        /// <summary>
        /// Xử lý unobserved task exception
        /// </summary>
        private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            HandleException(e.Exception, "Unobserved Task Exception");
            e.SetObserved(); // Ngăn chặn app crash
        }
        
        /// <summary>
        /// Xử lý exception chung
        /// </summary>
        private static void HandleException(Exception ex, string source, bool isTerminating = false)
        {
            // Log exception
            Logger.Instance.Error(ex, source);
            
            // Tạo thông báo lỗi chi tiết
            string errorMessage = BuildErrorMessage(ex, source);
            
            // Hiển thị MessageBox với chi tiết lỗi
            ShowErrorDialog(errorMessage, source, isTerminating);
            
            // Nếu là lỗi nghiêm trọng, ghi log và thoát
            if (isTerminating)
            {
                Logger.Instance.Error($"Ứng dụng sẽ thoát do lỗi nghiêm trọng: {source}");
                Environment.Exit(1);
            }
        }
        
        /// <summary>
        /// Xây dựng thông báo lỗi chi tiết
        /// </summary>
        private static string BuildErrorMessage(Exception ex, string source)
        {
            var sb = new System.Text.StringBuilder();
            
            sb.AppendLine($"❌ LỖI: {source}");
            sb.AppendLine();
            sb.AppendLine($"📋 Loại lỗi: {ex.GetType().FullName}");
            sb.AppendLine();
            sb.AppendLine($"💬 Thông báo: {ex.Message}");
            
            if (ex.InnerException != null)
            {
                sb.AppendLine();
                sb.AppendLine($"🔗 Lỗi gốc: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
            }
            
            if (ex.StackTrace != null)
            {
                sb.AppendLine();
                sb.AppendLine("📍 Stack Trace:");
                sb.AppendLine(ex.StackTrace);
            }
            
            sb.AppendLine();
            sb.AppendLine($"📁 File log: {_logFilePath}");
            sb.AppendLine($"⏰ Thời gian: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Hiển thị dialog lỗi
        /// </summary>
        private static void ShowErrorDialog(string message, string source, bool isTerminating)
        {
            var icon = isTerminating ? MessageBoxIcon.Error : MessageBoxIcon.Warning;
            var buttons = isTerminating ? MessageBoxButtons.OK : MessageBoxButtons.AbortRetryIgnore;
            
            var result = MessageBox.Show(
                message,
                $"LỖI - {source}",
                buttons,
                icon
            );
            
            if (result == DialogResult.Retry)
            {
                // Mở file log để xem chi tiết
                try
                {
                    if (File.Exists(_logFilePath))
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = _logFilePath,
                            UseShellExecute = true
                        });
                    }
                }
                catch { }
            }
            else if (result == DialogResult.Ignore)
            {
                Logger.Instance.Warning($"Người dùng bỏ qua lỗi: {source}");
            }
        }
        
        /// <summary>
        /// Giải phóng tài nguyên
        /// </summary>
        public static void Cleanup()
        {
            if (!_isInitialized) return;
            
            System.Windows.Forms.Application.ThreadException -= OnThreadException;
            AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
            TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
            
            _isInitialized = false;
            Logger.Instance.Info("GlobalExceptionHandler đã được dọn dẹp");
        }
    }
}
