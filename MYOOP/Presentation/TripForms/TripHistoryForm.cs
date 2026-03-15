using OOP.Application.Services.Interfaces;

namespace OOP.Presentation.TripForms
{
    public class TripHistoryForm : Form
    {
        private readonly Guid _userId;
        private readonly ITripService _tripService;

        private DataGridView DataGridViewTrips = null!;
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
        }

        private void BuildUI()
        {
            DataGridViewTrips = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };

            // FIX: thêm empty state label — trước đây để lưới trống không có thông báo.
            LabelEmpty = new Label
            {
                Text = "Bạn chưa có chuyến đi nào.",
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 12),
                ForeColor = Color.Gray,
                Visible = false
            };

            ButtonRefresh = new Button { Text = "Làm mới", Width = 120, Height = 40 };
            ButtonBack = new Button { Text = "Quay lại", Width = 120, Height = 40 };

            ButtonRefresh.Click += async (_, _) => await LoadTrips();
            ButtonBack.Click += (_, _) => Close();

            var panel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 60 };
            panel.Controls.Add(ButtonRefresh);
            panel.Controls.Add(ButtonBack);

            Controls.Add(LabelEmpty);
            Controls.Add(DataGridViewTrips);
            Controls.Add(panel);
        }

        private async Task LoadTrips()
        {
            ButtonRefresh.Enabled = false;
            try
            {
                var trips = await _tripService.GetTripHistory(_userId);

                if (trips.Count == 0)
                {
                    DataGridViewTrips.Visible = false;
                    LabelEmpty.Visible = true;
                    return;
                }

                LabelEmpty.Visible = false;
                DataGridViewTrips.Visible = true;
                DataGridViewTrips.DataSource = trips.Select(t => new
                {
                    TripId = t.Id.ToString()[..8],
                    Pickup = t.PickupLocation?.Address,
                    Destination = t.DestinationLocation?.Address,
                    Distance = $"{t.Distance:F2} km",
                    Fare = $"{t.Fare:N0} VNĐ",
                    Status = t.Status,
                    Date = t.RequestedAt.ToString("dd/MM/yyyy HH:mm")
                }).ToList();
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
    }
}