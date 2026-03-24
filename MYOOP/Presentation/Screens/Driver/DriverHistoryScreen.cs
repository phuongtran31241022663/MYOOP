using OOP.Presentation.Base;
using OOP.Application.Services.Interfaces;
using OOP.Domain.Entities;
using OOP.Domain.Enums;
using OOP.Domain.Interfaces;
using OOP.Presentation.Common.Theme;

namespace OOP.Presentation.Screens.Driver
{
    /// <summary>
    /// L?ch s? chuy?n di c?a t�i x?.
    /// Tuong t? PassengerHistoryScreen nhung th�m c?t hoa h?ng v� thu nh?p th?c.
    /// </summary>
    public class DriverHistoryScreen : UserControl, IScreen
    {
        // ── Constructor ───────────────────────────────────────────────────────
        public DriverHistoryScreen()
        {
            DoubleBuffered = true; // Reduces flicker when repainting
        }

        private readonly Guid _driverId;
        private readonly ITripService _tripService;
        private readonly IUserRepository _userRepo;
        private readonly IFareService _fareService;

        private DataGridView _dgv = null!;
        private Label _lblEmpty = null!;
        private Button _btnRefresh = null!;

        // Summary labels
        private Label _lblTotalTrips = null!;
        private Label _lblTotalIncome = null!;
        private Label _lblAvgRating = null!;

        public string ScreenTitle => "L?ch s?";

        public async Task OnNavigatedTo(object? parameter = null) => await LoadTrips();

        public bool OnNavigatingFrom() => true;

        public DriverHistoryScreen(
            Guid driverId,
            ITripService tripService,
            IUserRepository userRepo,
            IFareService fareService)
        {
            _driverId = driverId;
            _tripService = tripService;
            _userRepo = userRepo;
            _fareService = fareService;
            BuildUI();
        }

        private void BuildUI()
        {
            BackColor = AppTheme.PageBg;

            // Header bar
            var headerBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 54,
                BackColor = AppTheme.CardBg,
                Padding = new Padding(16, 8, 16, 8)
            };
            var lblTitle = new Label
            {
                Text = "L?ch s? chuy?n di",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            _btnRefresh = FormHelper.MakeButton("?? L�m m?i", AppTheme.Primary, AppTheme.PrimaryHover, height: 36);
            _btnRefresh.Width = 100;
            _btnRefresh.Dock = DockStyle.Right;
            _btnRefresh.Click += async (_, _) => await LoadTrips();
            headerBar.Controls.Add(lblTitle);
            headerBar.Controls.Add(_btnRefresh);

            // Summary strip
            var summaryStrip = new Panel
            {
                Dock = DockStyle.Top,
                Height = 48,
                BackColor = AppTheme.SidebarBg,
                Padding = new Padding(16, 4, 16, 4)
            };
            var summaryLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1 };
            for (int i = 0; i < 3; i++) summaryLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));

            _lblTotalTrips = MakeSumLabel("T?ng chuy?n: --");
            _lblTotalIncome = MakeSumLabel("Thu nh?p: --");
            _lblAvgRating = MakeSumLabel("��nh gi� TB: --");

            summaryLayout.Controls.Add(_lblTotalTrips, 0, 0);
            summaryLayout.Controls.Add(_lblTotalIncome, 1, 0);
            summaryLayout.Controls.Add(_lblAvgRating, 2, 0);
            summaryStrip.Controls.Add(summaryLayout);

            // Grid
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
                MakeCol("Pickup", "�i?m d�n", 200),
                MakeCol("Destination", "�i?m d?n", 200),
                MakeCol("Distance", "K.C�ch", 80),
                MakeCol("Fare", "Cu?c ph�", 100),
                MakeCol("Net", "Thu nh?p", 100),
                MakeCol("Status", "Tr?ng th�i", 120),
                MakeCol("Date", "Ng�y", 110)
            );

            _lblEmpty = new Label
            {
                Text = "Chua c� chuy?n di n�o.",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 11),
                ForeColor = AppTheme.TextMuted,
                Visible = false
            };

            Controls.Add(_lblEmpty);
            Controls.Add(_dgv);
            Controls.Add(summaryStrip);
            Controls.Add(headerBar);
        }

        private async Task LoadTrips()
        {
            _btnRefresh.Enabled = false;
            try
            {
                var trips = await _tripService.GetTripHistory(_driverId);
                var completed = trips.Where(t => t.Status == TripStatus.Completed).ToList();
                var commissionRates = await LoadCommissionRates(trips);

                // Summary
                _lblTotalTrips.Text = $"T?ng chuy?n: {trips.Count}";
                _lblTotalIncome.Text = $"Thu nh?p: {completed.Sum(t => t.Fare * (1m - GetRate(t.VehicleType, commissionRates))):N0} d";

                if (trips.Count == 0) { _dgv.Visible = false; _lblEmpty.Visible = true; return; }

                _dgv.Visible = true; _lblEmpty.Visible = false;
                _dgv.Rows.Clear();

                foreach (var t in trips)
                {
                    decimal net = t.Status == TripStatus.Completed
                        ? Math.Round(t.Fare * (1m - GetRate(t.VehicleType, commissionRates)), 0) : 0;

                    _dgv.Rows.Add(
                        t.Pickup?.Address ?? "�",
                        t.Destination?.Address ?? "�",
                        $"{t.Distance:F1} km",
                        t.Fare > 0 ? $"{t.Fare:N0} d" : "�",
                        net > 0 ? $"{net:N0} d" : "�",
                        StatusLabel(t.Status),
                        t.RequestedAt.ToLocalTime().ToString("dd/MM HH:mm")
                    );

                    _dgv.Rows[^1].DefaultCellStyle.ForeColor = t.Status switch
                    {
                        TripStatus.Completed => Color.FromArgb(20, 120, 60),
                        TripStatus.Cancelled => Color.FromArgb(160, 50, 50),
                        _ => AppTheme.TextPrimary
                    };
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"L?i t?i l?ch s?: {ex.Message}", "L?i",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { _btnRefresh.Enabled = true; }
        }

        private async Task<Dictionary<string, decimal>> LoadCommissionRates(IEnumerable<Trip> trips)
        {
            var rates = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            var vehicleTypes = trips
                .Select(t => t.VehicleType)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var vehicleType in vehicleTypes)
            {
                try
                {
                    var rule = await _fareService.GetFareRule(vehicleType);
                    rates[vehicleType] = rule?.CommissionRate ?? 0.2m;
                }
                catch
                {
                    rates[vehicleType] = 0.2m;
                }
            }

            return rates;
        }

        private static decimal GetRate(string vehicleType, Dictionary<string, decimal> rates)
        {
            if (rates.TryGetValue(vehicleType, out var rate))
                return rate;
            return 0.2m;
        }

        private static Label MakeSumLabel(string text) => new()
        {
            Text = text,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 9f),
            ForeColor = Color.FromArgb(180, 200, 220)
        };

        private static DataGridViewTextBoxColumn MakeCol(string name, string header, int width) =>
            new() { Name = name, HeaderText = header, Width = width, SortMode = DataGridViewColumnSortMode.Automatic };

        private static string StatusLabel(TripStatus s) => s switch
        {
            TripStatus.Completed => "? Ho�n th�nh",
            TripStatus.Cancelled => "? �� h?y",
            TripStatus.Timeout => "? H?t TG",
            TripStatus.Started => "?? �ang ch?y",
            _ => s.ToString()
        };
    }
}
