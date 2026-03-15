using OOP.Application.Services.Interfaces;
using OOP.Application.Validators;
using OOP.Domain.Entities;

namespace OOP
{
    public class RatingForm : Form
    {
        private ComboBox ComboBoxTrip = null!;
        private NumericUpDown NumericScore = null!;
        private TextBox TextBoxComment = null!;
        private Button ButtonSubmit = null!;
        private Button ButtonCancel = null!;

        private readonly ITripService _tripService;
        private readonly IRatingService _ratingService;
        private readonly Guid _passengerId;

        private List<Trip> _completedTrips = new();

        public RatingForm(
            Passenger passenger,
            IRatingService ratingService,
            ITripService tripService)
        {
            _passengerId = passenger?.Id ?? throw new ArgumentNullException(nameof(passenger));
            _ratingService = ratingService ?? throw new ArgumentNullException(nameof(ratingService));
            _tripService = tripService ?? throw new ArgumentNullException(nameof(tripService));

            InitializeControls();
            Load += async (_, _) => await OnFormLoad();
        }

        private void InitializeControls()
        {
            Width = 440;
            Height = 320;
            Text = "Đánh giá tài xế";
            StartPosition = FormStartPosition.CenterParent;

            var lblTrip = new Label { Text = "Chuyến đi:", Left = 20, Top = 24, AutoSize = true };

            ComboBoxTrip = new ComboBox
            {
                Left = 20,
                Top = 44,
                Width = 390,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            ComboBoxTrip.Format += (s, e) =>
            {
                if (e.ListItem is Trip t)
                    e.Value = $"{t.Id.ToString()[..8]}  {t.PickupLocation?.Label} → {t.DestinationLocation?.Label}  ({t.RequestedAt:dd/MM HH:mm})";
            };
            ComboBoxTrip.FormattingEnabled = true;

            var lblScore = new Label { Text = "Điểm (1–5 ⭐):", Left = 20, Top = 88, AutoSize = true };

            NumericScore = new NumericUpDown
            {
                Left = 20,
                Top = 108,
                Width = 100,
                Minimum = 1,
                Maximum = 5,
                Value = 5
            };

            var lblComment = new Label { Text = "Góp ý (Tùy chọn):", Left = 20, Top = 138, AutoSize = true };
            NumericScore.ValueChanged += (s, e) =>
            {
                bool needsComment = NumericScore.Value < 3;
                lblComment.Text = needsComment
                    ? "Góp ý (BẮT BUỘC):"
                    : "Góp ý (Tùy chọn):";
                lblComment.ForeColor = needsComment ? Color.Red : Color.Black;
            };
            TextBoxComment = new TextBox
            {
                Left = 20,
                Top = 158,
                Width = 390,
                Height = 72,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };

            ButtonSubmit = new Button { Text = "Gửi đánh giá", Left = 220, Top = 244, Width = 100, Height = 34 };
            ButtonCancel = new Button { Text = "Hủy", Left = 328, Top = 244, Width = 80, Height = 34 };

            ButtonSubmit.Click += async (_, _) => await OnSubmitClicked();
            ButtonCancel.Click += (_, _) => Close();

            Controls.AddRange(new Control[]
            {
                lblTrip, ComboBoxTrip, lblScore, NumericScore,
                lblComment, TextBoxComment, ButtonSubmit, ButtonCancel
            });
        }

        private async Task OnFormLoad()
        {
            try
            {
                var trips = await _tripService.GetTripHistory(_passengerId);
                _completedTrips = trips
                    .Where(t => t.Status == OOP.Domain.Enums.TripStatus.Completed && !t.IsRated)
                    .ToList();

                ComboBoxTrip.DataSource = _completedTrips;

                if (_completedTrips.Count == 0)
                {
                    ButtonSubmit.Enabled = false;
                    MessageBox.Show("Bạn không có chuyến đi nào cần đánh giá.",
                        "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải dữ liệu: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task OnSubmitClicked()
        {
            if (ComboBoxTrip.SelectedItem is not Trip trip)
            {
                MessageBox.Show("Vui lòng chọn chuyến đi.");
                return;
            }

            int score = (int)NumericScore.Value;
            string comment = TextBoxComment.Text.Trim();

            try
            {
                RatingValidator.ValidateRating(score, comment);
                await _ratingService.CreateRating(trip.Id, _passengerId, score, comment);
                MessageBox.Show("Đánh giá đã được gửi. Cảm ơn bạn!",
                    "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Close();
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show(ex.Message, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Gửi đánh giá thất bại",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}