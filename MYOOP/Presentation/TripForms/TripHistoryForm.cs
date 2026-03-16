using OOP.Application.Services.Interfaces;
using OOP.Presentation;

namespace OOP.Presentation.TripForms
{
    public class TripHistoryForm : Form
    {
        private readonly Guid _userId;
        private readonly ITripService _tripService;

        private DataGridView _dgvTrips = null!;
        private Label LabelEmpty = null!;
        private Button ButtonRefresh = null!;
        private Button ButtonBack = null!;

        public TripHistoryForm(Guid userId, ITripService tripService)
        {
            _userId = userId;
            _tripService = tripService;
            InitForm();
            BuildUI();
            Load += async (_, _) => await LoadTrips();
        }

        private void InitForm()
        {
            Text = "Lịch sử chuyến đi";
            Size = new Size(1000, 600);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = AppTheme.PageBg;
            Font = new Font("Segoe UI", 10F);
        }

        private void BuildUI()
        {
            _dgvTrips = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                BackgroundColor = AppTheme.CardBg,
                BorderStyle = BorderStyle.None,
                ColumnHeadersHeight = 36,
                RowTemplate = { Height = 28 },
                Font = new Font("Segoe UI", 9.5f)
            };

            _dgvTrips.Columns.AddRange(
                MakeCol("TripId", "ID", 80),
                MakeCol("Pickup", "Điểm đón", 220),
                MakeCol("Destination", "Điểm đến", 220),
                MakeCol("Distance", "Khoảng cách", 100),
                MakeCol("Fare", "Cước phí", 110),
                MakeCol("Status", "Trạng thái", 130),
                MakeCol("Date", "Ngày đặt", 130)
            );

            LabelEmpty = new Label
            {
                Text = "Bạn chưa có chuyến đi nào.",
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 12),
                ForeColor = Color.Gray,
                Visible = false
            };

            ButtonRefresh = FormHelper.MakeButton("🔄  Làm mới", AppTheme.Primary, AppTheme.PrimaryHover, height: 40);
            ButtonBack = FormHelper.MakeOutlineButton("← Quay lại", height: 40);
            ButtonRefresh.Width = 140;
            ButtonBack.Width = 140;

            ButtonRefresh.Click += async (_, _) => await LoadTrips();
            ButtonBack.Click += (_, _) => Close();

            var panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                Padding = new Padding(8, 10, 8, 0)
            };
            panel.Controls.Add(ButtonRefresh);
            panel.Controls.Add(ButtonBack);

            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = AppTheme.CardBg,
                Padding = new Padding(16, 12, 16, 0)
            };
            var lblHeader = new Label
            {
                Text = "Lịch sử chuyến đi",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 12.5f, FontStyle.Bold),
                ForeColor = AppTheme.TextPrimary,
                TextAlign = ContentAlignment.MiddleLeft
            };
            header.Controls.Add(lblHeader);

            Controls.Add(LabelEmpty);
            Controls.Add(_dgvTrips);
            Controls.Add(panel);
            Controls.Add(header);
        }

        private async Task LoadTrips()
        {
            ButtonRefresh.Enabled = false;
            try
            {
                var trips = await _tripService.GetTripHistory(_userId);

                if (trips.Count == 0)
                {
                    _dgvTrips.Visible = false;
                    LabelEmpty.Visible = true;
                    return;
                }

                LabelEmpty.Visible = false;
                _dgvTrips.Visible = true;

                _dgvTrips.Rows.Clear();
                foreach (var t in trips)
                {
                    _dgvTrips.Rows.Add(
                        t.Id.ToString()[..8],
                        t.PickupLocation?.Address ?? "–",
                        t.DestinationLocation?.Address ?? "–",
                        $"{t.Distance:F2} km",
                        t.Fare > 0 ? $"{t.Fare:N0} VNĐ" : "–",
                        StatusLabel(t.Status),
                        t.RequestedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
                    );

                    var row = _dgvTrips.Rows[_dgvTrips.Rows.Count - 1];
                    row.DefaultCellStyle.ForeColor = t.Status switch
                    {
                        OOP.Domain.Enums.TripStatus.Completed => Color.FromArgb(20, 120, 60),
                        OOP.Domain.Enums.TripStatus.Cancelled => Color.FromArgb(160, 50, 50),
                        _ => Color.FromArgb(40, 40, 40)
                    };
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải lịch sử: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                ButtonRefresh.Enabled = true;
            }
        }

        private static DataGridViewTextBoxColumn MakeCol(string name, string header, int width) =>
            new DataGridViewTextBoxColumn
            {
                Name = name,
                HeaderText = header,
                Width = width,
                SortMode = DataGridViewColumnSortMode.Automatic,
                DefaultCellStyle = new DataGridViewCellStyle { Padding = new Padding(4, 0, 4, 0) }
            };

        private static string StatusLabel(OOP.Domain.Enums.TripStatus status) => status switch
        {
            OOP.Domain.Enums.TripStatus.Requested => "⏳ Đang tìm",
            OOP.Domain.Enums.TripStatus.Matched => "🤝 Đã ghép",
            OOP.Domain.Enums.TripStatus.Arrived => "📍 Đã đến",
            OOP.Domain.Enums.TripStatus.Ongoing => "🚗 Đang chạy",
            OOP.Domain.Enums.TripStatus.Completed => "✅ Hoàn thành",
            OOP.Domain.Enums.TripStatus.Cancelled => "❌ Đã hủy",
            _ => status.ToString()
        };
    }
}
