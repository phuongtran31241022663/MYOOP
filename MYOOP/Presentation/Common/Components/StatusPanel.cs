using OOP.Infrastructure;
using OOP.Presentation.Common.Theme;
using System.Collections.Concurrent;

namespace OOP.Presentation.Common.Components
{
    /// <summary>
    /// Panel hiển thị trạng thái hệ thống và log lỗi theo thời gian thực.
    /// Giúp người dùng dễ dàng nhận biết mọi lỗi xảy ra trong ứng dụng.
    /// </summary>
    public class StatusPanel : Panel
    {
        private readonly RichTextBox _logTextBox;
        private readonly Label _statusLabel;
        private readonly Label _errorCountLabel;
        private readonly Button _clearButton;
        private readonly Button _viewLogButton;
        private readonly Panel _headerPanel;
        
        private static readonly ConcurrentQueue<LogEntry> _logEntries = new();
        private static int _errorCount = 0;
        private static int _warningCount = 0;
        
        public StatusPanel()
        {
            // Cấu hình panel chính
            BackColor = AppTheme.SidebarDark;
            BorderStyle = BorderStyle.FixedSingle;
            Dock = DockStyle.Bottom;
            Height = 150;
            Padding = new Padding(5);
            
            // Header panel với status và nút
            _headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 32,
                BackColor = AppTheme.SidebarBg,
                Padding = new Padding(8, 0, 8, 0)
            };
            
            // Status label
            _statusLabel = new Label
            {
                Text = "● Sẵn sàng",
                ForeColor = AppTheme.Success,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(8, 6)
            };
            
            // Error count label
            _errorCountLabel = new Label
            {
                Text = "Lỗi: 0 | Cảnh báo: 0",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9),
                AutoSize = true,
                Location = new Point(180, 6)
            };
            
            // Clear button
            _clearButton = new Button
            {
                Text = "Xóa log",
                Width = 70,
                Height = 24,
                FlatStyle = FlatStyle.Flat,
                BackColor = AppTheme.SidebarCard,
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Location = new Point(380, 4)
            };
            _clearButton.FlatAppearance.BorderSize = 0;
            _clearButton.Click += (_, _) => ClearLogs();
            
            // View log file button
            _viewLogButton = new Button
            {
                Text = "Mở file log",
                Width = 90,
                Height = 24,
                FlatStyle = FlatStyle.Flat,
                BackColor = AppTheme.Primary,
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Location = new Point(455, 4)
            };
            _viewLogButton.FlatAppearance.BorderSize = 0;
            _viewLogButton.Click += (_, _) => OpenLogFile();
            
            _headerPanel.Controls.Add(_viewLogButton);
            _headerPanel.Controls.Add(_clearButton);
            _headerPanel.Controls.Add(_errorCountLabel);
            _headerPanel.Controls.Add(_statusLabel);
            
            // Log textbox
            _logTextBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = AppTheme.DarkBg,
                ForeColor = Color.FromArgb(200, 200, 200),
                Font = new Font("Consolas", 8.5f),
                BorderStyle = BorderStyle.None,
                ScrollBars = RichTextBoxScrollBars.Vertical
            };
            
            Controls.Add(_logTextBox);
            Controls.Add(_headerPanel);
            
            // Subscribe to logger events
            Logger.Instance.Info("StatusPanel đã khởi tạo - Theo dõi lỗi...");
        }
        
        /// <summary>
        /// Thêm một log entry và hiển thị
        /// </summary>
        public void AddLog(string message, LogLevel level)
        {
            var entry = new LogEntry
            {
                Timestamp = DateTime.Now,
                Message = message,
                Level = level
            };
            
            _logEntries.Enqueue(entry);
            
            // Cập nhật màu sắc theo level
            Color textColor = level switch
            {
                LogLevel.Error => AppTheme.Danger,
                LogLevel.Warning => AppTheme.Warning,
                LogLevel.Debug => AppTheme.TextSubtle,
                _ => AppTheme.Success
            };
            
            string prefix = level switch
            {
                LogLevel.Error => "[LỖI]",
                LogLevel.Warning => "[CẢNH BÁO]",
                LogLevel.Debug => "[DEBUG]",
                _ => "[INFO]"
            };
            
            string logLine = $"[{entry.Timestamp:HH:mm:ss}] {prefix} {message}\n";
            
            // Cập nhật RichTextBox an toàn từ thread khác
            if (_logTextBox.InvokeRequired)
            {
                _logTextBox.Invoke(() => AppendLogLine(logLine, textColor));
            }
            else
            {
                AppendLogLine(logLine, textColor);
            }
            
            // Cập nhật đếm lỗi
            if (level == LogLevel.Error)
            {
                _errorCount++;
                UpdateErrorCount();
            }
            else if (level == LogLevel.Warning)
            {
                _warningCount++;
                UpdateErrorCount();
            }
            
            // Cập nhật status label nếu là lỗi
            if (level == LogLevel.Error)
            {
                UpdateStatus("● Có lỗi xảy ra!", AppTheme.Danger);
            }
        }
        
        private void AppendLogLine(string line, Color color)
        {
            _logTextBox.SelectionStart = _logTextBox.TextLength;
            _logTextBox.SelectionColor = color;
            _logTextBox.AppendText(line);
            _logTextBox.SelectionStart = _logTextBox.TextLength;
            _logTextBox.ScrollToCaret();
        }
        
        private void UpdateErrorCount()
        {
            _errorCountLabel.Text = $"Lỗi: {_errorCount} | Cảnh báo: {_warningCount}";
            
            // Đổi màu nếu có lỗi
            if (_errorCount > 0)
            {
                _errorCountLabel.ForeColor = AppTheme.Danger;
            }
            else if (_warningCount > 0)
            {
                _errorCountLabel.ForeColor = AppTheme.Warning;
            }
            else
            {
                _errorCountLabel.ForeColor = Color.White;
            }
        }
        
        private void UpdateStatus(string status, Color color)
        {
            _statusLabel.Text = status;
            _statusLabel.ForeColor = color;
        }
        
        private void ClearLogs()
        {
            _logEntries.Clear();
            _errorCount = 0;
            _warningCount = 0;
            
            if (_logTextBox.InvokeRequired)
            {
                _logTextBox.Invoke(() => _logTextBox.Clear());
            }
            else
            {
                _logTextBox.Clear();
            }
            
            UpdateErrorCount();
            UpdateStatus("● Sẵn sàng", AppTheme.Success);
        }
        
        private void OpenLogFile()
        {
            try
            {
                string logPath = Logger.Instance.LogFilePath;
                if (File.Exists(logPath))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = logPath,
                        UseShellExecute = true
                    });
                }
                else
                {
                    FormHelper.ShowError($"File log không tồn tại:\nlogPath", "Không tìm thấy");
                }
            }
            catch (Exception ex)
            {
                FormHelper.ShowError($"Không thể mở file log:\n{ex.Message}");
            }
        }
        
        /// <summary>
        /// Hiển thị thông báo lỗi với chi tiết đầy đủ
        /// </summary>
        public void ShowError(string title, string message, Exception? ex = null)
        {
            string fullMessage = message;
            if (ex != null)
            {
                fullMessage += $"\n\nChi tiết lỗi:\n{ex.GetType().Name}: {ex.Message}";
                if (ex.StackTrace != null)
                {
                    fullMessage += $"\n\nStack Trace:\n{ex.StackTrace}";
                }
            }
            
            AddLog($"{title}: {message}", LogLevel.Error);
            
            // Hiển thị MessageBox
            FormHelper.ShowError(fullMessage, title);
        }
        
        /// <summary>
        /// Hiển thị cảnh báo
        /// </summary>
        public void ShowWarning(string title, string message)
        {
            AddLog($"{title}: {message}", LogLevel.Warning);
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        
        /// <summary>
        /// Hiển thị thông tin
        /// </summary>
        public void ShowInfo(string title, string message)
        {
            AddLog($"{title}: {message}", LogLevel.Info);
        }
        
        private class LogEntry
        {
            public DateTime Timestamp { get; set; }
            public string Message { get; set; } = "";
            public LogLevel Level { get; set; }
        }
    }
}
