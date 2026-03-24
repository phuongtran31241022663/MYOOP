using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using OOP.Application.Services.Interfaces;
using OOP.Domain.Entities;
using DriverEntity = OOP.Domain.Entities.Driver;
using OOP.Domain.Enums;
using OOP.Presentation.Common.Theme;
using OOP.Presentation.Base;

namespace OOP.Presentation.Screens.Driver
{
    /// <summary>
    /// Màn hình bản đồ + hành trình cho tài xế.
    /// Thay thế DriverTripForm (trước đây mở riêng bằng ShowDialog).
    ///
    /// Khác biệt chính so với DriverTripForm:
    /// - Không Close() sau khi xác nhận thanh toán → báo Shell.OnTripEnded()
    /// - Nhận trip từ parameter trong OnNavigatedTo hoặc từ Shell.CurrentTrip
    /// - Timer sim và refresh chạy chỉ khi screen này visible
    /// </summary>
    public class DriverActiveTripScreen : UserControl, IScreen
    {
        // ── Dependencies ──────────────────────────────────────────────────────
        private readonly DriverShell _shell;
        private readonly ITripService _tripService;
        private readonly IRouteService _routeService;
        private readonly IUserService _userService;
        private readonly ISimulationService _simulationService;
        private readonly IFareService _fareService;

        // ── State ─────────────────────────────────────────────────────────────
        private Trip? _trip;
        private DriverEntity? _driver;
        private bool _isRefreshing = false;
        private readonly System.Windows.Forms.Timer _timer = new() { Interval = 2000 };
        private decimal _currentCommissionRate = 0.2m;

        // ── Map ───────────────────────────────────────────────────────────────
        private GMapControl _map = null!;
        private readonly GMapOverlay _markerOverlay = new("markers");
        private readonly GMapOverlay _routeOverlay = new("route");
        private readonly GMapOverlay _driverOverlay = new("driver");

        // ── Right panel controls ──────────────────────────────────────────────
        private Panel _pnlStatusHeader = null!;
        private Label _lblStatusText = null!;

        // Step dots
        private Panel _pnlS1 = null!, _pnlS2 = null!, _pnlS3 = null!, _pnlS4 = null!;
        private Panel _pnlC1 = null!, _pnlC2 = null!, _pnlC3 = null!;

        // Info
        private Label _lblPickup = null!;
        private Label _lblDestination = null!;
        private Label _lblDistance = null!;
        private Label _lblFare = null!;

        // Action buttons
        private Panel _pnlAction = null!;
        private Button _btnArrived = null!;
        private Button _btnStart = null!;
        private Button _btnComplete = null!;

        // Payment panel
        private Panel _pnlPayment = null!;
        private Label _lblPayAmount = null!;
        private Label _lblPayBreakdown = null!;
        private Label _lblPayCommission = null!;
        private Label _lblPayNet = null!;
        private Button _btnConfirmPayment = null!;

        // Empty state
        private Panel _pnlEmpty = null!;

        // ── IScreen ───────────────────────────────────────────────────────────
        public string ScreenTitle => "Bản đồ";

        public async Task OnNavigatedTo(object? parameter = null)
        {
            // parameter có thể là Trip (vừa accept) hoặc null (user tự mở tab)
            if (parameter is Trip t)
                _trip = t;
            else
                _trip = _shell.CurrentTrip;

            if (_trip == null)
            {
                ShowEmptyState();
                _timer.Stop();
                return;
            }

            _timer.Start();
            await RefreshAsync();

            // Start simulation nếu vừa accept (Matched)
            if (_trip.Status == TripStatus.Matched)
            {
                try { await _simulationService.SimulateTripProgress(_trip.Id); }
                catch { /* simulation đã chạy rồi */ }
            }
        }

        public bool OnNavigatingFrom()
        {
            // Không dừng timer khi rời — timer chạy nền để update Shell.CurrentTrip
            return true;
        }

        // ─────────────────────────────────────────────────────────────────────
        public DriverActiveTripScreen(
            DriverShell shell,
            ITripService tripService,
            IRouteService routeService,
            IUserService userService,
            ISimulationService simulationService,
            IFareService fareService)
        {
            _shell = shell;
            _tripService = tripService;
            _routeService = routeService;
            _userService = userService;
            _simulationService = simulationService;
            _fareService = fareService;

            BuildUI();

            _timer.Tick += async (_, _) =>
            {
                try { await _simulationService.Tick(); } catch { }
                await RefreshAsync();
            };
        }

        // ── Build UI ──────────────────────────────────────────────────────────

        private void BuildUI()
        {
            BackColor = AppTheme.PageBg;

            // Map (fill)
            _map = new GMapControl { Dock = DockStyle.Fill };
            GMap.NET.GMaps.Instance.Mode = GMap.NET.AccessMode.ServerAndCache;
            _map.MapProvider = GMapProviders.GoogleMap;
            _map.MinZoom = 2; _map.MaxZoom = 18; _map.Zoom = 14;
            _map.Position = new PointLatLng(10.7626, 106.6601);
            _map.Overlays.Add(_markerOverlay);
            _map.Overlays.Add(_routeOverlay);
            _map.Overlays.Add(_driverOverlay);
            _map.MouseClick += async (_, e) =>
            {
                if (e.Button == MouseButtons.Right) await OnMapRightClick(e.X, e.Y);
            };

            // Right panel (fixed width)
            var pnlRight = new Panel { Dock = DockStyle.Right, Width = 340, BackColor = Color.White };
            BuildRightPanel(pnlRight);

            // Empty state (full width, shown when no trip)
            _pnlEmpty = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.PageBg, Visible = false };
            var lblEmpty = new Label
            {
                Text = "Chưa có chuyến đi.\nNhận chuyến từ tab Điều phối.",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 11),
                ForeColor = AppTheme.TextMuted
            };
            _pnlEmpty.Controls.Add(lblEmpty);

            Controls.Add(_pnlEmpty);
            Controls.Add(_map);
            Controls.Add(pnlRight);
        }

        private void BuildRightPanel(Panel host)
        {
            // Status header
            _pnlStatusHeader = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = AppTheme.Primary };
            _lblStatusText = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.White,
                Text = "Đang chờ..."
            };
            _pnlStatusHeader.Controls.Add(_lblStatusText);

            // Step bar
            var pnlStepBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.FromArgb(248, 250, 253),
                Padding = new Padding(16, 10, 16, 10)
            };
            var stepLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 7,
                RowCount = 1,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            for (int i = 0; i < 7; i++)
                stepLayout.ColumnStyles.Add(new ColumnStyle(i % 2 == 0 ? SizeType.AutoSize : SizeType.Percent, 100f));

            (_pnlS1, _) = MakeStepDot("1", "Nhận");
            (_pnlS2, _) = MakeStepDot("2", "Đến đón");
            (_pnlS3, _) = MakeStepDot("3", "Bắt đầu");
            (_pnlS4, _) = MakeStepDot("4", "Xong");
            _pnlC1 = MakeConn(); _pnlC2 = MakeConn(); _pnlC3 = MakeConn();

            stepLayout.Controls.Add(_pnlS1.Parent ?? _pnlS1, 0, 0);
            stepLayout.Controls.Add(_pnlC1, 1, 0);
            stepLayout.Controls.Add(_pnlS2.Parent ?? _pnlS2, 2, 0);
            stepLayout.Controls.Add(_pnlC2, 3, 0);
            stepLayout.Controls.Add(_pnlS3.Parent ?? _pnlS3, 4, 0);
            stepLayout.Controls.Add(_pnlC3, 5, 0);
            stepLayout.Controls.Add(_pnlS4.Parent ?? _pnlS4, 6, 0);
            pnlStepBar.Controls.Add(stepLayout);

            // Trip info
            var pnlInfo = new Panel
            {
                Dock = DockStyle.Top,
                Height = 136,
                BackColor = Color.White,
                Padding = new Padding(20, 14, 20, 8)
            };
            int iy = 14;
            void AddInfo(Label lbl) { lbl.Location = new Point(20, iy); iy += 28; pnlInfo.Controls.Add(lbl); }

            _lblPickup = MakeInfoLabel("📍  --", primary: true);
            _lblDestination = MakeInfoLabel("🏁  --", primary: true);
            _lblDistance = MakeInfoLabel("📏  --", primary: false);
            _lblFare = MakeInfoLabel("💰  --", primary: false);
            AddInfo(_lblPickup); AddInfo(_lblDestination); AddInfo(_lblDistance); AddInfo(_lblFare);

            var divider = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = AppTheme.BorderLight };

            // Action panel
            _pnlAction = new Panel { Dock = DockStyle.Top, Height = 180, Padding = new Padding(16, 14, 16, 0) };
            _btnArrived = MakeBtn("📍  Đã đến điểm đón", AppTheme.Primary);
            _btnStart = MakeBtn("▶  Bắt đầu chuyến", AppTheme.Success);
            _btnComplete = MakeBtn("🏁  Kết thúc chuyến", Color.FromArgb(100, 60, 200));
            var btnBack = MakeBtn("← Dashboard", AppTheme.TextMuted);
            _btnArrived.Click += async (_, _) => await OnArrivedClicked();
            _btnStart.Click += async (_, _) => await OnStartClicked();
            _btnComplete.Click += async (_, _) => await OnCompleteClicked();
            btnBack.Click += async (_, _) => await _shell.Nav.NavigateTo(DriverShell.KEY_DASHBOARD);
            int by = 14;
            void PlaceBtn(Button b) { b.Location = new Point(16, by); by += 46; _pnlAction.Controls.Add(b); }
            PlaceBtn(_btnArrived); PlaceBtn(_btnStart); PlaceBtn(_btnComplete); PlaceBtn(btnBack);

            // Payment panel
            _pnlPayment = BuildPaymentPanel();

            // Map hint
            var hint = new Panel { Dock = DockStyle.Bottom, Height = 30, BackColor = Color.FromArgb(248, 249, 250) };
            hint.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "Nhấp phải lên bản đồ để cập nhật vị trí tài xế",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 8f),
                ForeColor = AppTheme.TextMuted
            });

            host.Controls.Add(hint);
            host.Controls.Add(_pnlPayment);
            host.Controls.Add(_pnlAction);
            host.Controls.Add(divider);
            host.Controls.Add(pnlInfo);
            host.Controls.Add(pnlStepBar);
            host.Controls.Add(_pnlStatusHeader);
        }

        private Panel BuildPaymentPanel()
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Top,
                Height = 224,
                BackColor = Color.White,
                Padding = new Padding(16, 14, 16, 0),
                Visible = false
            };

            var lblTitle = new Label
            {
                Text = "Hóa đơn chuyến đi",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = AppTheme.TextPrimary,
                Location = new Point(16, 10),
                AutoSize = true
            };
            _lblPayAmount = new Label
            {
                Font = new Font("Segoe UI", 26f, FontStyle.Bold),
                ForeColor = AppTheme.TextPrimary,
                Location = new Point(16, 38),
                AutoSize = true
            };
            _lblPayBreakdown = new Label
            {
                Font = AppTheme.SmallFont,
                ForeColor = AppTheme.TextMuted,
                Location = new Point(16, 88),
                Width = 290,
                Height = 30
            };
            var divider = new Panel { BackColor = AppTheme.BorderLight, Location = new Point(16, 122), Width = 290, Height = 1 };
            _lblPayCommission = new Label
            {
                Font = AppTheme.SmallFont,
                ForeColor = AppTheme.TextMuted,
                Location = new Point(16, 128),
                Width = 290,
                Height = 18
            };
            _lblPayNet = new Label
            {
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = AppTheme.Success,
                Location = new Point(16, 148),
                Width = 290,
                Height = 20
            };
            _btnConfirmPayment = new Button
            {
                Text = "✅  Xác nhận đã nhận tiền mặt",
                Location = new Point(16, 174),
                Width = 294,
                Height = 40,
                BackColor = AppTheme.Success,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnConfirmPayment.FlatAppearance.BorderSize = 0;
            _btnConfirmPayment.Click += async (_, _) => await OnConfirmPaymentClicked();

            pnl.Controls.AddRange(new Control[]
            {
                lblTitle, _lblPayAmount, _lblPayBreakdown,
                divider, _lblPayCommission, _lblPayNet, _btnConfirmPayment
            });
            return pnl;
        }

        // ── Refresh ───────────────────────────────────────────────────────────

        private async Task RefreshAsync()
        {
            if (_isRefreshing) return;
            _isRefreshing = true;
            try
            {
                if (_trip == null) { ShowEmptyState(); return; }

                var updated = await _tripService.GetTrip(_trip.Id);
                if (updated == null) return;
                _trip = updated;
                _currentCommissionRate = await GetCommissionRate(updated.VehicleType);

                // Sync Shell state
                _shell.SetCurrentTrip(_trip);

                var userProfile = await _userService.GetUserProfile(_shell.Driver.Id);
                _driver = userProfile as DriverEntity;

                if (InvokeRequired) { BeginInvoke(() => ApplyTripToUI(_trip)); }
                else ApplyTripToUI(_trip);

                RefreshMap();
                await DrawRoutes();
            }
            finally { _isRefreshing = false; }
        }

        private void ApplyTripToUI(Trip trip)
        {
            // Status header
            (_lblStatusText.Text, _pnlStatusHeader.BackColor) = trip.Status switch
            {
                TripStatus.Matched => ("🤝 Đã nhận — Đang đến đón", Color.FromArgb(13, 110, 253)),
                TripStatus.Arrived => ("📍 Đã đến điểm đón — Chờ khách", Color.FromArgb(25, 135, 84)),
                TripStatus.Started => ("🚗 Đang di chuyển", Color.FromArgb(25, 135, 84)),
                TripStatus.Completed => ("✅ Chuyến hoàn thành", Color.FromArgb(20, 100, 50)),
                TripStatus.Cancelled => ("❌ Đã hủy", Color.FromArgb(160, 50, 50)),
                _ => (trip.Status.ToString(), AppTheme.Primary)
            };

            // Step bar
            SetStep(_pnlS1, "done");
            SetStep(_pnlS2, trip.Status is TripStatus.Arrived or TripStatus.Started or TripStatus.Completed ? "done"
                           : trip.Status == TripStatus.Matched ? "active" : "todo");
            SetStep(_pnlS3, trip.Status is TripStatus.Started or TripStatus.Completed ? "done"
                           : trip.Status == TripStatus.Arrived ? "active" : "todo");
            SetStep(_pnlS4, trip.Status is TripStatus.Completed ? "done"
                           : trip.Status == TripStatus.Started ? "active" : "todo");
            _pnlC1.BackColor = trip.Status is TripStatus.Arrived or TripStatus.Started or TripStatus.Completed ? AppTheme.Success : AppTheme.BorderLight;
            _pnlC2.BackColor = trip.Status is TripStatus.Started or TripStatus.Completed ? AppTheme.Success : AppTheme.BorderLight;
            _pnlC3.BackColor = trip.Status is TripStatus.Completed ? AppTheme.Success : AppTheme.BorderLight;

            // Info
            _lblPickup.Text = $"📍  {trip.Pickup.Address}";
            _lblDestination.Text = $"🏁  {trip.Destination.Address}";
            _lblDistance.Text = $"📏  {trip.Distance:F2} km";
            _lblFare.Text = $"💰  {trip.Fare:N0} VNĐ";

            // Payment vs action
            bool isDone = trip.Status is TripStatus.Completed or TripStatus.Cancelled or TripStatus.Timeout;
            _pnlPayment.Visible = isDone;
            _pnlAction.Visible = !isDone;

            if (isDone && trip.Status == TripStatus.Completed) RefreshPaymentPanel(trip);

            // Action buttons
            bool simDone = !_simulationService.IsSimulationActive(trip.Id) && trip.Status == TripStatus.Matched;
            _btnArrived.Visible = trip.Status is TripStatus.Matched or TripStatus.Arrived;
            _btnArrived.Enabled = trip.Status == TripStatus.Arrived || simDone;
            _btnStart.Visible = trip.Status == TripStatus.Arrived;
            _btnComplete.Visible = trip.Status == TripStatus.Started;

            // Empty state
            _pnlEmpty.Visible = false;
        }

        private void RefreshPaymentPanel(Trip trip)
        {
            decimal fare = trip.Fare;
            decimal commission = Math.Round(fare * _currentCommissionRate, 0);
            decimal net = fare - commission;

            _lblPayAmount.Text = $"{fare:N0} VNĐ";
            _lblPayBreakdown.Text = $"Khoảng cách: {trip.Distance:F2} km";
            _lblPayCommission.Text = $"Hoa hồng ({_currentCommissionRate * 100m:F0}%): -{commission:N0} đ";
            _lblPayNet.Text = $"Thu nhập thực: {net:N0} đ";
            _btnConfirmPayment.Enabled = (trip.Status == TripStatus.Completed);
        }

        private async Task<decimal> GetCommissionRate(string vehicleType)
        {
            try
            {
                var rule = await _fareService.GetFareRule(vehicleType);
                return rule?.CommissionRate ?? 0.2m;
            }
            catch
            {
                return 0.2m;
            }
        }

        private void ShowEmptyState()
        {
            if (InvokeRequired) { BeginInvoke(ShowEmptyState); return; }
            _pnlEmpty.Visible = true;
            _pnlEmpty.BringToFront();
        }

        // ── Map ───────────────────────────────────────────────────────────────

        private void RefreshMap()
        {
            if (_trip == null) return;
            if (InvokeRequired) { BeginInvoke(RefreshMap); return; }

            _markerOverlay.Markers.Clear();

            if (_driver?.Position != null)
                _markerOverlay.Markers.Add(new GMarkerGoogle(
                    new PointLatLng(_driver.Position.Lat, _driver.Position.Lng),
                    GMarkerGoogleType.blue_dot)
                { ToolTipText = "Tài xế" });

            _markerOverlay.Markers.Add(new GMarkerGoogle(
                new PointLatLng(_trip.Pickup.Lat, _trip.Pickup.Lng),
                GMarkerGoogleType.green_dot)
            { ToolTipText = "Điểm đón" });

            _markerOverlay.Markers.Add(new GMarkerGoogle(
                new PointLatLng(_trip.Destination.Lat, _trip.Destination.Lng),
                GMarkerGoogleType.red_dot)
            { ToolTipText = "Điểm đến" });
        }

        private async Task DrawRoutes()
        {
            if (_trip == null) return;
            _routeOverlay.Routes.Clear();
            _driverOverlay.Routes.Clear();

            if (_trip.Status is TripStatus.Matched or TripStatus.Arrived && _driver?.Position != null)
            {
                var pts = await _routeService.GetRoutePointsAsync(_driver.Position, _trip.Pickup);
                if (pts.Count >= 2)
                {
                    var route = new GMapRoute(pts.Select(p => new PointLatLng(p.Lat, p.Lng)), "toPickup")
                    { Stroke = new Pen(Color.FromArgb(200, 0, 160, 60), 3) };
                    _driverOverlay.Routes.Add(route);
                    SafeInvoke(() => _map.ZoomAndCenterRoute(route));
                    return;
                }
            }

            if (_trip.Status is TripStatus.Started or TripStatus.Completed)
            {
                var full = await _routeService.GetFullRouteAsync(_trip.Pickup, _trip.Destination);
                if (full?.Points.Count >= 2)
                {
                    var route = new GMapRoute(full.Points.Select(p => new PointLatLng(p.Lat, p.Lng)), "tripRoute")
                    { Stroke = new Pen(Color.FromArgb(180, 0, 120, 255), 4) };
                    _routeOverlay.Routes.Add(route);
                    SafeInvoke(() => { _map.ZoomAndCenterRoute(route); _map.Refresh(); });
                }
            }
        }

        private void SafeInvoke(Action a)
        {
            if (_map.InvokeRequired) _map.Invoke(a); else a();
        }

        // ── Action handlers ───────────────────────────────────────────────────

        private async Task OnArrivedClicked()
        {
            if (_trip == null) return;
            try { await _tripService.MarkArrived(_trip.Id); await RefreshAsync(); }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private async Task OnStartClicked()
        {
            if (_trip == null) return;
            try { await _tripService.StartTrip(_trip.Id); await _simulationService.SimulateTripProgress(_trip.Id); await RefreshAsync(); }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private async Task OnCompleteClicked()
        {
            if (_trip == null) return;
            if (MessageBox.Show("Xác nhận đã đến điểm đến?\nChuyến đi sẽ kết thúc.",
                "Kết thúc chuyến", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            try
            {
                await _tripService.CompleteTrip(_trip.Id);
                _timer.Stop();
                await RefreshAsync();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private async Task OnConfirmPaymentClicked()
        {
            if (_trip == null) return;
            if (MessageBox.Show(
                $"Xác nhận đã nhận {_trip.Fare:N0} VNĐ tiền mặt?",
                "Xác nhận thanh toán", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            try
            {
                await _tripService.ConfirmPayment(_trip.Id, _trip.Fare);

                MessageBox.Show(
                    "✅ Hoàn tất!\nThu nhập đã được cộng vào ví.",
                    "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // KEY POINT: không Close() — báo Shell kết thúc chuyến
                _shell.OnTripEnded();
                _trip = null;
                ShowEmptyState();

                // Quay về Dashboard
                await _shell.Nav.NavigateTo(DriverShell.KEY_DASHBOARD);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private async Task OnMapRightClick(int x, int y)
        {
            if (MessageBox.Show("Cập nhật vị trí tài xế tại điểm này?", "Cập nhật vị trí",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            var pt = _map.FromLocalToLatLng(x, y);
            var loc = new GeoLocation("Vị trí hiện tại", "Cập nhật thủ công", pt.Lat, pt.Lng);
            try { await _userService.UpdateDriverLocation(_shell.Driver.Id, loc); await RefreshAsync(); }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        // ── UI factories ──────────────────────────────────────────────────────

        private static void SetStep(Panel dot, string state) =>
            dot.BackColor = state switch { "done" => AppTheme.Success, "active" => AppTheme.Primary, _ => AppTheme.BorderLight };

        private static (Panel dot, Label lbl) MakeStepDot(string num, string text)
        {
            var wrap = new Panel { Width = 44, Height = 46, BackColor = Color.Transparent };
            var dot = new Panel { Width = 24, Height = 24, BackColor = AppTheme.BorderLight, Location = new Point(10, 0) };
            dot.Region = System.Drawing.Region.FromHrgn(
                CreateRoundRectRgn(0, 0, 25, 25, 24, 24));
            dot.Controls.Add(new Label
            {
                Text = num,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.White
            });
            wrap.Controls.AddRange(new Control[]
            {
                dot,
                new Label
                {
                    Text = text, Location = new Point(0, 28), Width = 44, Height = 16,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 7.5f), ForeColor = AppTheme.TextMuted
                }
            });
            return (dot, (Label)wrap.Controls[1]);
        }

        private static Panel MakeConn() =>
            new() { Dock = DockStyle.Fill, Height = 2, BackColor = AppTheme.BorderLight, Margin = new Padding(0, 11, 0, 0) };

        private static Label MakeInfoLabel(string text, bool primary) => new()
        {
            Text = text,
            Font = new Font("Segoe UI", primary ? 9.5f : 9f),
            ForeColor = primary ? AppTheme.TextPrimary : AppTheme.TextMuted,
            Width = 290,
            Height = 22,
            AutoEllipsis = true
        };

        private static Button MakeBtn(string text, Color bg)
        {
            var btn = new Button
            {
                Text = text,
                Width = 298,
                Height = 38,
                BackColor = bg,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern IntPtr CreateRoundRectRgn(int nL, int nT, int nR, int nB, int nW, int nH);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timer.Stop(); _timer.Dispose();
                foreach (var r in _routeOverlay.Routes) r.Stroke?.Dispose();
                foreach (var r in _driverOverlay.Routes) r.Stroke?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
