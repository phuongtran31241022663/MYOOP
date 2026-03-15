using OOP.Application.Services.Interfaces;
using OOP.Domain.Entities;
using static OOP.Presentation.AppTheme;
using static OOP.Presentation.FormHelper;

namespace OOP.Presentation
{
    public class PassengerDashboardForm : Form
    {
        private readonly Passenger _passenger;
        private readonly ITripService _tripService;

        private readonly IRatingService _ratingService;

        private readonly Func<Passenger, ITripService, Form> _requestTripFormFactory;
        private readonly Func<Passenger, ITripService, Form> _tripHistoryFormFactory;

        private readonly Func<Passenger, IRatingService, ITripService, Form> _ratingFormFactory;

        private Label _lblWelcome = null!;
        private Label _lblStats = null!;

        public PassengerDashboardForm(
            Passenger passenger,
            ITripService tripService,
            IRatingService ratingService,
            Func<Passenger, ITripService, Form> requestTripFormFactory,
            Func<Passenger, ITripService, Form> tripHistoryFormFactory,
            Func<Passenger, IRatingService, ITripService, Form> ratingFormFactory)
        {
            _passenger = passenger ?? throw new ArgumentNullException(nameof(passenger));
            _tripService = tripService ?? throw new ArgumentNullException(nameof(tripService));
            _ratingService = ratingService ?? throw new ArgumentNullException(nameof(ratingService));
            _requestTripFormFactory = requestTripFormFactory ?? throw new ArgumentNullException(nameof(requestTripFormFactory));
            _tripHistoryFormFactory = tripHistoryFormFactory ?? throw new ArgumentNullException(nameof(tripHistoryFormFactory));
            _ratingFormFactory = ratingFormFactory ?? throw new ArgumentNullException(nameof(ratingFormFactory));

            InitForm();
            BuildUI();
        }

        private void InitForm()
        {
            Text = $"RideGo – {_passenger.Name}";
            Size = new Size(520, 560);
            MinimumSize = new Size(440, 500);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = AppTheme.PageBg;
            Font = new Font("Segoe UI", 10F);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
        }

        private void BuildUI()
        {
            BuildHeader();
            BuildMenuCard();
        }

        private void BuildHeader()
        {
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 110,
                BackColor = AppTheme.DarkBg,
                Padding = new Padding(28, 18, 28, 12)
            };

            _lblWelcome = new Label
            {
                Text = $"Xin chào, {_passenger.Name} 👋",
                Font = new Font("Segoe UI", 15, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Top,
                Height = 36,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _lblStats = new Label
            {
                Text = BuildStatsText(),
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = AppTheme.TextSubtle,
                Dock = DockStyle.Top,
                Height = 24,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft
            };

            header.Controls.Add(_lblStats);
            header.Controls.Add(_lblWelcome);
            Controls.Add(header);
        }

        private void BuildMenuCard()
        {
            var card = new Panel
            {
                BackColor = AppTheme.CardBg,
                Width = 380,
                Height = 330
            };

            card.Paint += FormHelper.RoundedBorderPainter(AppTheme.CardRadius);
            Resize += (_, _) => FormHelper.CenterInParent(card, this, topOffset: 110);
            FormHelper.CenterInParent(card, this, topOffset: 110);

            int y = 28;

            var btnRequestTrip = MakeMenuBtn(
                "🚗  Đặt xe", "Tìm tài xế và đặt chuyến mới",
                AppTheme.Success, AppTheme.SuccessHover, height: 56);
            FormHelper.Place(btnRequestTrip, card, 24, y, card.Width - 48, 56);
            // FIX: event handler gắn trực tiếp vào button — xoá các OnXxxClicked thừa bên dưới.
            btnRequestTrip.Click += (_, _) => OpenChildForm(_requestTripFormFactory(_passenger, _tripService));
            y += 66;

            var btnTripHistory = MakeMenuBtn(
                "🕒  Lịch sử chuyến đi", "Xem các chuyến đã thực hiện",
                AppTheme.Primary, AppTheme.PrimaryHover, height: 48);
            FormHelper.Place(btnTripHistory, card, 24, y, card.Width - 48, 48);
            btnTripHistory.Click += (_, _) => OpenChildForm(_tripHistoryFormFactory(_passenger, _tripService));
            y += 58;

            var btnRating = MakeMenuBtn(
                "⭐  Đánh giá tài xế", "Đánh giá chuyến đi vừa hoàn thành",
                AppTheme.Warning, AppTheme.WarningHover, height: 48);
            FormHelper.Place(btnRating, card, 24, y, card.Width - 48, 48);
            btnRating.Click += (_, _) => OpenChildForm(_ratingFormFactory(_passenger, _ratingService, _tripService));
            y += 58;

            FormHelper.Place(
                new Label { BackColor = AppTheme.BorderLight },
                card, 24, y, card.Width - 48, 1);
            y += 14;

            var btnLogout = FormHelper.MakeOutlineButton("← Đăng xuất", height: AppTheme.SmallButton);
            FormHelper.Place(btnLogout, card, 24, y, card.Width - 48, AppTheme.SmallButton);
            btnLogout.Click += (_, _) =>
            {
                if (MessageBox.Show("Bạn có chắc muốn đăng xuất?", "Xác nhận",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    Close();
            };

            Controls.Add(card);
        }

        // FIX: Xoá OnRequestTripClicked, OnTripHistoryClicked, OnRatingClicked —
        // tất cả đều là dead code (không được gán vào button nào, logic đã inline ở trên).

        private void OpenChildForm(Form childForm)
        {
            using (childForm)
            {
                childForm.StartPosition = FormStartPosition.CenterParent;
                Hide();
                childForm.ShowDialog(this);
            }
            _lblStats.Text = BuildStatsText();
            Show();
            Focus();
        }

        private string BuildStatsText() =>
            $"Tổng chuyến đã đi: {_passenger.TotalTrips}   •   SĐT: {_passenger.Phone}";

        private static Button MakeMenuBtn(
            string text, string subtext,
            Color bg, Color hover, int height,
            Color? textColor = null)
        {
            var fg = textColor ?? Color.White;
            var btn = new Button
            {
                Height = height,
                BackColor = bg,
                ForeColor = fg,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                Text = ""
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Paint += (s, e) =>
            {
                var g = e.Graphics;
                var rect = btn.ClientRectangle;
                using var mainFont = new Font("Segoe UI", 10.5f, FontStyle.Bold);
                using var subFont = new Font("Segoe UI", 8.5f);
                using var mainBrush = new SolidBrush(fg);
                using var subBrush = new SolidBrush(Color.FromArgb(fg == Color.White ? 200 : 130, fg));
                int textX = btn.Padding.Left + 16;
                int totalH = (int)mainFont.GetHeight() + (int)subFont.GetHeight() + 2;
                int startY = (rect.Height - totalH) / 2;
                g.DrawString(text, mainFont, mainBrush, textX, startY);
                g.DrawString(subtext, subFont, subBrush, textX, startY + (int)mainFont.GetHeight() + 2);
            };
            FormHelper.AttachHover(btn, bg, hover);
            return btn;
        }
    }
}