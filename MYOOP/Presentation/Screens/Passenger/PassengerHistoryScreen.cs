// ─────────────────────────────────────────────────────────────────────────────
// PassengerHistoryScreen.cs
// Wrap lại logic TripHistoryForm thành một Screen.
// ─────────────────────────────────────────────────────────────────────────────

using OOP.Presentation.Base;
using OOP.Presentation.Common.Theme;
using OOP.Application.Services.Interfaces;
using OOP.Domain.Enums;
using OOP.Domain.Interfaces;
using OOP.Presentation.Common.Theme;

namespace OOP.Presentation.Screens.Passenger
{
    public class PassengerHistoryScreen : UserControl, IScreen
    {
        private readonly Guid _userId;
        private readonly ITripService _tripService;
        private readonly IUserRepository _userRepo;

        private DataGridView _dgv = null!;
        private Label _lblEmpty = null!;
        private Button _btnRefresh = null!;

        public string ScreenTitle => "Lịch sử chuyến đi";

        public async Task OnNavigatedTo(object? parameter = null)
        {
            // Mỗi lần mở tab này → tự reload
            await LoadTrips();
        }

        public bool OnNavigatingFrom() => true;

        public PassengerHistoryScreen(Guid userId, ITripService tripService, IUserRepository userRepo)
        {
            _userId = userId;
            _tripService = tripService;
            _userRepo = userRepo;
            BuildUI();
        }

        private void BuildUI()
        {
            BackColor = AppTheme.PageBg;

            // Header bar
            var headerBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = AppTheme.CardBg,
                Padding = new Padding(16, 8, 16, 8)
            };
            var lblTitle = new Label
            {
                Text = "Lịch sử chuyến đi",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            _btnRefresh = FormHelper.MakeButton("🔄 Làm mới", AppTheme.Primary, AppTheme.PrimaryHover, height: 34);
            _btnRefresh.Width = 100;
            _btnRefresh.Dock = DockStyle.Right;
            _btnRefresh.Click += async (_, _) => await LoadTrips();
            headerBar.Controls.Add(lblTitle);
            headerBar.Controls.Add(_btnRefresh);

            // Grid — copy từ TripHistoryForm
            _dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                BackgroundColor = AppTheme.CardBg,
                BorderStyle = BorderStyle.None,
                ColumnHeadersHeight = 36,
                Font = new Font("Segoe UI", 9.5f)
            };
            _dgv.Columns.AddRange(
                MakeCol("Pickup", "Điểm đón", 180),
                MakeCol("Destination", "Điểm đến", 180),
                MakeCol("Distance", "K.Cách", 80),
                MakeCol("Fare", "Cước phí", 100),
                MakeCol("Status", "Trạng thái", 120),
                MakeCol("Date", "Ngày", 120)
            );

            _lblEmpty = new Label
            {
                Text = "Chưa có chuyến đi nào.",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 11),
                ForeColor = AppTheme.TextMuted,
                Visible = false
            };

            Controls.Add(_lblEmpty);
            Controls.Add(_dgv);
            Controls.Add(headerBar);
        }

        private async Task LoadTrips()
        {
            _btnRefresh.Enabled = false;
            try
            {
                var trips = await _tripService.GetTripHistory(_userId);
                if (trips.Count == 0) { _dgv.Visible = false; _lblEmpty.Visible = true; return; }

                _dgv.Visible = true; _lblEmpty.Visible = false;
                _dgv.Rows.Clear();
                foreach (var t in trips)
                {
                    _dgv.Rows.Add(
                        t.Pickup?.Address ?? "–",
                        t.Destination?.Address ?? "–",
                        $"{t.Distance:F1} km",
                        t.Fare > 0 ? $"{t.Fare:N0} VNĐ" : "–",
                        StatusLabel(t.Status),
                        t.RequestedAt.ToLocalTime().ToString("dd/MM HH:mm")
                    );

                    var row = _dgv.Rows[^1];
                    row.DefaultCellStyle.ForeColor = t.Status switch
                    {
                        TripStatus.Completed => Color.FromArgb(20, 120, 60),
                        TripStatus.Cancelled => Color.FromArgb(160, 50, 50),
                        _ => AppTheme.TextPrimary
                    };
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải lịch sử: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { _btnRefresh.Enabled = true; }
        }

        private static DataGridViewTextBoxColumn MakeCol(string name, string header, int width) =>
            new()
            {
                Name = name,
                HeaderText = header,
                Width = width,
                SortMode = DataGridViewColumnSortMode.Automatic
            };

        private static string StatusLabel(TripStatus s) => s switch
        {
            TripStatus.Completed => "✅ Hoàn thành",
            TripStatus.Cancelled => "❌ Đã hủy",
            TripStatus.Timeout => "⌛ Hết TG",
            TripStatus.Started => "🚗 Đang chạy",
            _ => s.ToString()
        };
    }
}


// ─────────────────────────────────────────────────────────────────────────────
// PassengerProfileScreen.cs
// Wrap lại ProfileForm thành Screen.
// ─────────────────────────────────────────────────────────────────────────────

namespace OOP.Presentation.Screens.Passenger
{
    public class PassengerProfileScreen : UserControl, IScreen
    {
        private readonly OOP.Domain.Entities.User _user;
        private readonly IUserService _userService;

        private TextBox _txtName = null!;
        private TextBox _txtPhone = null!;
        private Label _lblSaved = null!;

        public string ScreenTitle => "Tài khoản";

        public Task OnNavigatedTo(object? parameter = null)
        {
            // Reset lại form mỗi khi mở
            _txtName.Text = _user.Name;
            _txtPhone.Text = _user.Phone;
            _lblSaved.Visible = false;
            return Task.CompletedTask;
        }

        public bool OnNavigatingFrom() => true;

        public PassengerProfileScreen(OOP.Domain.Entities.User user, IUserService userService)
        {
            _user = user;
            _userService = userService;
            BuildUI();
        }

        private void BuildUI()
        {
            BackColor = AppTheme.PageBg;
            Padding = new Padding(24);

            var card = FormHelper.MakeCard(360, 260);
            card.Location = new Point(20, 20);

            int y = 20;
            var lblName = FormHelper.MakeLabel("Họ và tên", 9.5f, foreColor: AppTheme.TextMuted);
            FormHelper.Place(lblName, card, 20, y, 300, 18); y += 22;
            _txtName = FormHelper.MakeInputSized("Nhập họ tên", 300);
            FormHelper.Place(_txtName, card, 20, y, 300, AppTheme.InputHeight); y += 46;

            var lblPhone = FormHelper.MakeLabel("Số điện thoại", 9.5f, foreColor: AppTheme.TextMuted);
            FormHelper.Place(lblPhone, card, 20, y, 300, 18); y += 22;
            _txtPhone = FormHelper.MakeInputSized("Nhập số điện thoại", 300);
            FormHelper.Place(_txtPhone, card, 20, y, 300, AppTheme.InputHeight); y += 46;

            var btnSave = FormHelper.MakeButton("Lưu thay đổi", AppTheme.Success, AppTheme.SuccessHover);
            btnSave.Width = 160;
            FormHelper.Place(btnSave, card, 20, y, 160, AppTheme.ButtonHeight);
            btnSave.Click += async (_, _) => await OnSave();

            _lblSaved = new Label
            {
                Text = "✅ Đã lưu thành công",
                ForeColor = AppTheme.Success,
                Font = AppTheme.SmallFont,
                AutoSize = true,
                Location = new Point(190, y + 12),
                Visible = false
            };
            card.Controls.Add(_lblSaved);

            Controls.Add(card);
        }

        private async Task OnSave()
        {
            try
            {
                await _userService.UpdateUserProfile(_user.Id, _txtName.Text.Trim(), _txtPhone.Text.Trim());
                _lblSaved.Visible = true;
                await Task.Delay(3000);
                _lblSaved.Visible = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}