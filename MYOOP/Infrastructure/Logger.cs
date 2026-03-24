using System.Diagnostics;

namespace OOP.Infrastructure
{
    /// <summary>
    /// Singleton Logger - ghi log mọi nơi trong app.
    /// Thread-safe với lock.
    /// </summary>
    public sealed class Logger
    {
        private static readonly object _lock = new object();
        private static Logger? _instance;
        private readonly string _logFilePath;
        private readonly bool _enableConsoleOutput;

        private Logger()
        {
            // Tạo thư mục Logs nếu chưa có
            string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            Directory.CreateDirectory(logDir);
            
            _logFilePath = Path.Combine(logDir, $"app_{DateTime.Now:yyyyMMdd}.log");
            _enableConsoleOutput = true;
        }

        /// <summary>
        /// Singleton Instance - Thread-safe
        /// </summary>
        public static Logger Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new Logger();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Log message với timestamp
        /// </summary>
        public void Log(string message, LogLevel level = LogLevel.Info)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string logEntry = $"[{timestamp}] [{level}] {message}";

            lock (_lock)
            {
                try
                {
                    File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
                    
                    if (_enableConsoleOutput || level == LogLevel.Error)
                    {
                        Debug.WriteLine(logEntry);
                    }
                }
                catch
                {
                    // Ignore logging errors to prevent app crash
                }
            }
        }

        /// <summary>
        /// Log info message
        /// </summary>
        public void Info(string message) => Log(message, LogLevel.Info);

        /// <summary>
        /// Log warning message
        /// </summary>
        public void Warning(string message) => Log(message, LogLevel.Warning);

        /// <summary>
        /// Log error message
        /// </summary>
        public void Error(string message) => Log(message, LogLevel.Error);

        /// <summary>
        /// Log exception
        /// </summary>
        public void Error(Exception ex, string context = "")
        {
            string message = string.IsNullOrEmpty(context) 
                ? $"Exception: {ex.Message}\nStackTrace: {ex.StackTrace}"
                : $"{context} - Exception: {ex.Message}\nStackTrace: {ex.StackTrace}";
            Log(message, LogLevel.Error);
        }

        /// <summary>
        /// Log debug message
        /// </summary>
        public void LogDebug(string message) => Log(message, LogLevel.Debug);

        /// <summary>
        /// Get đường dẫn file log hiện tại
        /// </summary>
        public string LogFilePath => _logFilePath;
    }

    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error
    }
}
