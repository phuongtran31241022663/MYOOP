using OOP.Application.Services.Interfaces;
using OOP.Domain.Entities;
using OOP.Domain.Enums;
using OOP.Presentation.Common.Theme;
using OOP.Presentation.Base;
using DriverEntity = OOP.Domain.Entities.Driver;

namespace OOP.Presentation.Screens.Driver
{
    /// <summary>
    /// Màn hình điều phối chuyến của tài xế.
    /// 3 trạng thái loại trừ nhau:
    ///   _pnlEmpty     — Inactive hoặc chưa có chuyến
    ///   _pnlRequest   — Có yêu cầu mới chờ accept/reject
    ///   _pnlActiveTrip — Đang có chuyến (step bar + action)
    /// </summary>
    public class DriverDashboardScreen : UserControl, IScreen
    {
        // ── Dependencies ──────────────────────────────────────────────────────
        private readonly DriverShell _shell;
        private readonly ITripService _tripService;
        private readonly IUserService _userService;
        private readonly ISimulationService _simulationService;

        // ── State ─────────────────────────────────────────────────────────────
        private Trip? _pendingTrip;
        private bool _isLoading = false;
        private readonly HashSet<Guid> _notifiedIds = new();

        // ── Stat labels ───────────────────────────────────────────────────────
        private Label _lblStatRating = null!;
        private Label _lblStatTrips = null!;
        private Label _lblStatIncome = null!;
        private Label _lblStatWallet = null!;
        private Label _lblRevenue = null!;

        // ── Empty panel ───────────────────────────────────────────────────────
        private Panel _pnlEmpty = null!;
        private Label _lblEmptyMsg = null!;

        // ── Request card ──────────────────────────────────────────────────────
        private Panel _pnlRequest = null!;
        private Label _lblReqPickup = null!;
        private Label _lblReqDest = null!;
        private Label _lblReqMeta = null!;
        private Button _btnAccept = null!;
        private Button _btnReject = null!;

        // ── Active trip panel ─────────────────────────────────────────────────
        private Panel _pnlActiveTrip = null!;
        private Label _lblActiveRoute = null!;
        private Label _lblActiveInfo = null!;
        private Panel _pnlStep1 = null!, _pnlStep2 = null!, _pnlStep3 = null!, _pnlStep4 = null!;
        private Panel _pnlConn1 = null!, _pnlConn2 = null!, _pnlConn3 = null!;
        private Button _btnMainAction = null!;
        private Button _btnViewMap = null!;

        // ── Log ───────────────────────────────────────────────────────────────
        private ListBox _lstLog = null!;

        // ── IScreen ───────────────────────────────────────────────────────────
        public string ScreenTitle => "Điều phối";
        public async Task OnNavigatedTo(object? parameter = null) => await RefreshAsync();
        public bool OnNavigatingFrom() => true;

        // ─────────────────────────────────────────────────────────────────────
        public DriverDashboardScreen(
            DriverShell shell,
            ITripService tripService,
            IUserService userService,
            ISimulationService simulationService)
        {
            DoubleBuffered = true; // Reduces flicker when repainting cards
            _shell = shell;
            _tripService = tripService;
            _userService = userService;
            _simulationService = simulationService;
            BuildUI();
        }

        // ── Build UI ──────────────────────────────────────────────────────────

        private void BuildUI()
        {
            BackColor = AppTheme.PageBg;

            // Top bar
            var topBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 54,
                BackColor = AppTheme.CardBg,
                Padding = new Padding(20, 0, 12, 0)
            };
            var lblTitle = new Label
            {
                Text = "Điều phối chuyến",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = AppTheme.TextPrimary,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            var btnRefresh = new Button
            {
                Text = "🔄",
                Dock = DockStyle.Right,
                Width = 44,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = AppTheme.TextMuted,
                Font = new Font("Segoe UI", 13f),
                Cursor = Cursors.Hand
            };
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Click += async (_, _) => await RefreshAsync();
            topBar.Controls.Add(btnRefresh);
            topBar.Controls.Add(lblTitle);

            // Stats strip
            var statsStrip = BuildStatsStrip();

            // Content area
            var content = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 12, 16, 8) };
            BuildEmptyPanel(content);
            BuildRequestCard(content);
            BuildActiveTripPanel(content);

            // Log
            var logBorder = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 92,
                BackColor = Color.FromArgb(240, 243, 248),
                Padding = new Padding(1, 1, 1, 0)
            };
            _lstLog = new ListBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = AppTheme.TextMuted,
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(248, 249, 251)
            };
            logBorder.Controls.Add(_lstLog);

            Controls.Add(content);
            Controls.Add(logBorder);
            Controls.Add(statsStrip);
            Controls.Add(topBar);
        }

        private Panel BuildStatsStrip()
        {
            var strip = new Panel
            {
                Dock = DockStyle.Top,
                Height = 68,
                BackColor = AppTheme.SidebarBg,
                Padding = new Padding(12, 8, 12, 8)
            };
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 5, RowCount = 1 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));

            _lblStatRating = MakeStatLabel();
            _lblStatTrips = MakeStatLabel();
            _lblStatIncome = MakeStatLabel();
            _lblStatWallet = MakeStatLabel();
            _lblRevenue = MakeStatLabel();
            _lblRevenue.TextAlign = ContentAlignment.MiddleLeft;
            _lblRevenue.Font = new Font("Segoe UI", 8.5f);

            layout.Controls.Add(_lblStatRating, 0, 0);
            layout.Controls.Add(_lblStatTrips, 1, 0);
            layout.Controls.Add(_lblStatIncome, 2, 0);
            layout.Controls.Add(_lblStatWallet, 3, 0);
            layout.Controls.Add(_lblRevenue, 4, 0);
            strip.Controls.Add(layout);
            return strip;
        }

        private static Label MakeStatLabel() => new()
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 9f),
            ForeColor = Color.White
        };

        private void BuildEmptyPanel(Panel parent)
        {
            _pnlEmpty = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            _lblEmptyMsg = new Label
            {
                Text = "Không có yêu cầu mới\nChờ hành khách đặt xe...",
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 11f),
                ForeColor = AppTheme.TextMuted
            };
            _pnlEmpty.Controls.Add(_lblEmptyMsg);
            parent.Controls.Add(_pnlEmpty);
        }

        private void BuildRequestCard(Panel parent)
        {
            _pnlRequest = new Panel
            {
                Dock = DockStyle.Top,
                Height = 210,
                BackColor = AppTheme.CardBg,
                Visible = false,
                Padding = new Padding(20, 14, 20, 14)
            };
            _pnlRequest.Paint += (s, e) =>
            {
                using var p = new Pen(AppTheme.Primary, 1.5f);
                e.Graphics.DrawRectangle(p, 0, 0, _pnlRequest.Width - 1, _pnlRequest.Height - 1);
            };

            var lblNew = new Label
            {
                Text = "🔔  Yêu cầu mới",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = AppTheme.TextPrimary,
                AutoSize = true,
                Location = new Point(20, 14)
            };
            _pnlRequest.Controls.Add(lblNew);

            int y = 46;
            _lblReqPickup = MakeReqLabel(AppTheme.Success); _lblReqPickup.Location = new Point(20, y); y += 26;
            _lblReqDest = MakeReqLabel(AppTheme.Danger); _lblReqDest.Location = new Point(20, y); y += 26;
            _lblReqMeta = MakeReqLabel(AppTheme.TextMuted);
            _lblReqMeta.Font = AppTheme.SmallFont;
            _lblReqMeta.Location = new Point(20, y); y += 30;

            _btnAccept = MakeActionBtn("✅  Nhận cuốc", AppTheme.Success, 165);
            _btnReject = MakeActionBtn("✕  Từ chối", Color.White, 105);
            _btnAccept.ForeColor = Color.White;
            _btnReject.ForeColor = AppTheme.TextMuted;
            _btnReject.FlatAppearance.BorderSize = 1;
            _btnReject.FlatAppearance.BorderColor = AppTheme.BorderLight;
            _btnAccept.Location = new Point(20, y);
            _btnReject.Location = new Point(192, y);
            _btnAccept.Click += async (_, _) => await OnAcceptClicked();
            _btnReject.Click += async (_, _) => await OnRejectClicked();

            _pnlRequest.Controls.AddRange(new Control[]
            {
                _lblReqPickup, _lblReqDest, _lblReqMeta, _btnAccept, _btnReject
            });
            parent.Controls.Add(_pnlRequest);
        }

        private static Label MakeReqLabel(Color color) => new()
        {
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = color,
            Width = 480,
            Height = 22,
            AutoEllipsis = true
        };

        private void BuildActiveTripPanel(Panel parent)
        {
            _pnlActiveTrip = new Panel
            {
                Dock = DockStyle.Top,
                Height = 260,
                BackColor = AppTheme.CardBg,
                Visible = false
            };

            // Route header
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 72, Padding = new Padding(20, 12, 20, 8) };
            _lblActiveRoute = new Label
            {
                Dock = DockStyle.Top,
                Height = 26,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = AppTheme.TextPrimary,
                TextAlign = ContentAlignment.MiddleLeft
            };
            _lblActiveInfo = new Label
            {
                Dock = DockStyle.Top,
                Height = 22,
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = AppTheme.TextMuted,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlHeader.Controls.Add(_lblActiveInfo);
            pnlHeader.Controls.Add(_lblActiveRoute);

            // Step bar
            var pnlStep = new Panel
            {
                Dock = DockStyle.Top,
                Height = 64,
                BackColor = Color.FromArgb(248, 250, 253),
                Padding = new Padding(20, 10, 20, 10)
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

            (_pnlStep1, _) = MakeStepDot("1", "Nhận");
            (_pnlStep2, _) = MakeStepDot("2", "Đến đón");
            (_pnlStep3, _) = MakeStepDot("3", "Bắt đầu");
            (_pnlStep4, _) = MakeStepDot("4", "Xong");
            _pnlConn1 = MakeConnector();
            _pnlConn2 = MakeConnector();
            _pnlConn3 = MakeConnector();

            stepLayout.Controls.Add(_pnlStep1.Parent ?? _pnlStep1, 0, 0);
            stepLayout.Controls.Add(_pnlConn1, 1, 0);
            stepLayout.Controls.Add(_pnlStep2.Parent ?? _pnlStep2, 2, 0);
            stepLayout.Controls.Add(_pnlConn2, 3, 0);
            stepLayout.Controls.Add(_pnlStep3.Parent ?? _pnlStep3, 4, 0);
            stepLayout.Controls.Add(_pnlConn3, 5, 0);
            stepLayout.Controls.Add(_pnlStep4.Parent ?? _pnlStep4, 6, 0);
            pnlStep.Controls.Add(stepLayout);

            // Action row
            var pnlAction = new Panel { Dock = DockStyle.Top, Height = 56, Padding = new Padding(20, 10, 20, 8) };
            _btnMainAction = MakeActionBtn("📍  Đã đến điểm đón", AppTheme.Primary, 220);
            _btnViewMap = MakeActionBtn("🗺  Bản đồ", Color.White, 120);
            _btnViewMap.ForeColor = AppTheme.Primary;
            _btnViewMap.FlatAppearance.BorderSize = 1;
            _btnViewMap.FlatAppearance.BorderColor = AppTheme.Primary;
            _btnMainAction.Dock = DockStyle.Left;
            _btnViewMap.Dock = DockStyle.Left;
            var spacer = new Panel { Width = 10, Dock = DockStyle.Left, BackColor = Color.Transparent };
            pnlAction.Controls.AddRange(new Control[] { _btnViewMap, spacer, _btnMainAction });
            _btnMainAction.Click += async (_, _) => await OnMainActionClicked();
            _btnViewMap.Click += async (_, _) => await _shell.Nav.NavigateTo(DriverShell.KEY_MAP);

            _pnlActiveTrip.Controls.Add(pnlAction);
            _pnlActiveTrip.Controls.Add(pnlStep);
            _pnlActiveTrip.Controls.Add(pnlHeader);
            parent.Controls.Add(_pnlActiveTrip);
        }

        // ── Public API ────────────────────────────────────────────────────────

        public async Task RefreshAsync()
        {
            if (_isLoading) return;
            _isLoading = true;
            try
            {
                var refreshed = await _userService.GetUserProfile(_shell.Driver.Id);
                if (refreshed is DriverEntity rd) _shell.Driver.RestoreFromSnapshot(rd);

                RefreshStatsStrip();

                if (_shell.Driver.Status == DriverStatus.Offline)
                {
                    ShowEmpty("Bạn đang Nghỉ\nBật Active trên header để nhận chuyến.");
                    return;
                }

                // Stale OnTrip recovery
                var allTrips = await _tripService.GetTripHistory(_shell.Driver.Id);
                var activeTrips = allTrips.Where(t =>
                    t.DriverId == _shell.Driver.Id &&
                    t.Status is TripStatus.Matched or TripStatus.Arrived or TripStatus.Started).ToList();

                if (_shell.Driver.Status == DriverStatus.OnTrip && !activeTrips.Any() && _shell.CurrentTrip == null)
                {
                    AddLog("[Cảnh báo] Phục hồi trạng thái OnTrip không hợp lệ...");
                    try
                    {
                        await _userService.ForceRecoverDriverStatus(_shell.Driver.Id);
                        var r2 = await _userService.GetUserProfile(_shell.Driver.Id);
                        if (r2 is DriverEntity rd2) _shell.Driver.RestoreFromSnapshot(rd2);
                        AddLog("Đã phục hồi về Active.");
                    }
                    catch (Exception ex) { AddLog($"[Lỗi phục hồi] {ex.Message}"); }
                }
                else if (_shell.CurrentTrip == null && activeTrips.Any())
                {
                    _shell.SetCurrentTrip(activeTrips.First());
                }

                UpdateRevenue(allTrips);

                if (_shell.CurrentTrip != null)
                {
                    ShowActiveTrip(_shell.CurrentTrip);
                    return;
                }

                // Tìm chuyến mới
                var Active = await _tripService.GetActiveTripsForDriver(_shell.Driver.Id);
                bool hasNew = false;
                foreach (var t in Active)
                {
                    if (_notifiedIds.Contains(t.Id)) continue;
                    _notifiedIds.Add(t.Id); hasNew = true;
                }
                _notifiedIds.IntersectWith(Active.Select(t => t.Id));

                if (Active.Any())
                {
                    _pendingTrip = Active.First();
                    ShowRequestCard(_pendingTrip);
                    if (hasNew) System.Media.SystemSounds.Asterisk.Play();
                }
                else
                {
                    _pendingTrip = null;
                    ShowEmpty("Không có yêu cầu mới\nChờ hành khách đặt xe...");
                }
            }
            catch (Exception ex) { AddLog($"[Lỗi] {ex.Message}"); }
            finally { _isLoading = false; }
        }

        public void AddLog(string msg)
        {
            if (InvokeRequired) { BeginInvoke(() => AddLog(msg)); return; }
            if (_lstLog.Items.Count >= 200) _lstLog.Items.RemoveAt(0);
            _lstLog.Items.Add($"[{DateTime.Now:HH:mm}] {msg}");
            _lstLog.TopIndex = _lstLog.Items.Count - 1;
        }

        // ── Show panels ───────────────────────────────────────────────────────

        private void ShowEmpty(string msg)
        {
            if (InvokeRequired) { BeginInvoke(() => ShowEmpty(msg)); return; }
            _pnlRequest.Visible = false;
            _pnlActiveTrip.Visible = false;
            _pnlEmpty.Visible = true;
            _lblEmptyMsg.Text = msg;
        }

        private void ShowRequestCard(Trip t)
        {
            if (InvokeRequired) { BeginInvoke(() => ShowRequestCard(t)); return; }
            _pnlEmpty.Visible = false;
            _pnlActiveTrip.Visible = false;
            _pnlRequest.Visible = true;
            _pnlRequest.BringToFront();

            _lblReqPickup.Text = $"📍 {t.Pickup.Name}";
            _lblReqDest.Text = $"🏁 {t.Destination.Name}";
            _lblReqMeta.Text = $"📏 {(t.Distance > 0 ? $"{t.Distance:F1} km" : "–")}   " +
                                  $"💰 {t.Fare:N0} đ" +
                                  $"🛵 {t.VehicleType}";
        }

        private void ShowActiveTrip(Trip t)
        {
            if (InvokeRequired) { BeginInvoke(() => ShowActiveTrip(t)); return; }
            _pnlEmpty.Visible = false;
            _pnlRequest.Visible = false;
            _pnlActiveTrip.Visible = true;

            _lblActiveRoute.Text = $"{t.Pickup.Name}  →  {t.Destination.Name}";
            _lblActiveInfo.Text = $"📏 {(t.Distance > 0 ? $"{t.Distance:F1} km" : "--")}   " +
                                    $"💰 {t.Fare:N0} đ";
            UpdateStepBar(t.Status);
            UpdateMainActionButton(t.Status);
        }

        // ── Accept / Reject ───────────────────────────────────────────────────

        private async Task OnAcceptClicked()
        {
            if (_pendingTrip == null) return;
            _btnAccept.Enabled = false;
            _btnReject.Enabled = false;

            try
            {
                var refreshed = await _userService.GetUserProfile(_shell.Driver.Id);
                if (refreshed is DriverEntity rd) _shell.Driver.RestoreFromSnapshot(rd);

                if (_shell.Driver.Status != DriverStatus.Available)
                {
                    MessageBox.Show("Tài xế không ở trạng thái sẵn sàng.", "Thông báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                bool ok = await _tripService.TryAssignDriver(_pendingTrip.Id, _shell.Driver.Id);
                if (!ok)
                {
                    MessageBox.Show("Chuyến đã được tài xế khác nhận.", "Thông báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    _pendingTrip = null;
                    await RefreshAsync();
                    return;
                }

                var trip = await _tripService.GetTrip(_pendingTrip.Id);
                _pendingTrip = null;

                var updatedDriver = await _userService.GetUserProfile(_shell.Driver.Id);
                if (updatedDriver is DriverEntity ud) _shell.Driver.RestoreFromSnapshot(ud);

                await _shell.OnTripAccepted(trip!);
                AddLog($"Đã nhận: {trip!.Pickup.Name} → {trip.Destination.Name}");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _btnAccept.Enabled = true;
                _btnReject.Enabled = true;
            }
        }

        private async Task OnRejectClicked()
        {
            if (_pendingTrip == null) return;
            try
            {
                await _tripService.RejectTrip(_pendingTrip.Id, _shell.Driver.Id, "Tài xế từ chối");
                _pendingTrip = null;
                AddLog("Đã từ chối chuyến.");
                await RefreshAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── Main action (step progression) ────────────────────────────────────

        private async Task OnMainActionClicked()
        {
            if (_shell.CurrentTrip == null) return;
            try
            {
                switch (_shell.CurrentTrip.Status)
                {
                    case TripStatus.Matched:
                        await _tripService.MarkArrived(_shell.CurrentTrip.Id);
                        AddLog("Đã đến điểm đón.");
                        break;
                    case TripStatus.Arrived:
                        await _tripService.StartTrip(_shell.CurrentTrip.Id);
                        AddLog("Chuyến đi đã bắt đầu.");
                        break;
                    case TripStatus.Started:
                        await OnCompleteClicked();
                        return;
                }

                var updated = await _tripService.GetTrip(_shell.CurrentTrip.Id);
                _shell.SetCurrentTrip(updated);
                if (updated != null) ShowActiveTrip(updated);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task OnCompleteClicked()
        {
            if (_shell.CurrentTrip == null) return;
            if (MessageBox.Show("Xác nhận đã đến điểm đến?", "Kết thúc chuyến",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            try
            {
                await _tripService.CompleteTrip(_shell.CurrentTrip.Id);
                var updated = await _tripService.GetTrip(_shell.CurrentTrip.Id);
                _shell.SetCurrentTrip(updated);
                AddLog("Chuyến đi hoàn thành. Chờ xác nhận thanh toán.");
                await _shell.Nav.NavigateTo(DriverShell.KEY_MAP, updated);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── Step bar ──────────────────────────────────────────────────────────

        private void UpdateStepBar(TripStatus s)
        {
            SetStep(_pnlStep1, "done");
            SetStep(_pnlStep2, s is TripStatus.Arrived or TripStatus.Started or TripStatus.Completed ? "done"
                              : s == TripStatus.Matched ? "active" : "todo");
            SetStep(_pnlStep3, s is TripStatus.Started or TripStatus.Completed ? "done"
                              : s == TripStatus.Arrived ? "active" : "todo");
            SetStep(_pnlStep4, s is TripStatus.Completed ? "done"
                              : s == TripStatus.Started ? "active" : "todo");

            _pnlConn1.BackColor = s is TripStatus.Arrived or TripStatus.Started or TripStatus.Completed
                ? AppTheme.Success : AppTheme.BorderLight;
            _pnlConn2.BackColor = s is TripStatus.Started or TripStatus.Completed
                ? AppTheme.Success : AppTheme.BorderLight;
            _pnlConn3.BackColor = s is TripStatus.Completed
                ? AppTheme.Success : AppTheme.BorderLight;
        }

        private static void SetStep(Panel dot, string state) =>
            dot.BackColor = state switch
            {
                "done" => AppTheme.Success,
                "active" => AppTheme.Primary,
                _ => AppTheme.BorderLight
            };

        private void UpdateMainActionButton(TripStatus status)
        {
            (_btnMainAction.Text, _btnMainAction.BackColor, _btnMainAction.Enabled) = status switch
            {
                TripStatus.Matched => ("📍  Đã đến điểm đón", AppTheme.Primary, true),
                TripStatus.Arrived => ("▶  Bắt đầu chuyến", AppTheme.Success, true),
                TripStatus.Started => ("💵  Hoàn thành & Thu tiền", Color.FromArgb(100, 60, 200), true),
                TripStatus.Completed => ("✅  Chuyến đã hoàn tất", Color.Gray, false),
                _ => ("❌  Chuyến bị hủy", Color.DarkRed, false)
            };
        }

        // ── Stats strip ───────────────────────────────────────────────────────

        private void RefreshStatsStrip()
        {
            if (InvokeRequired) { BeginInvoke(RefreshStatsStrip); return; }
            var d = _shell.Driver;
            _lblStatRating.Text = $"⭐ {d.AverageRating:F1}\nĐánh giá";
            _lblStatTrips.Text = $"🛵 {d.TotalTrips}\nChuyến";
            _lblStatIncome.Text = $"💰 {d.Income / 1000:F0}k\nThu nhập";
            _lblStatWallet.Text = $"👛 {d.Wallet / 1000:F0}k\nVí";
        }

        private void UpdateRevenue(List<Trip> trips)
        {
            if (InvokeRequired) { BeginInvoke(() => UpdateRevenue(trips)); return; }
            var today = DateTime.Today;
            var total = trips
                .Where(t => t.Status == TripStatus.Completed)
                .Where(t => (t.CompletedAt ?? t.RequestedAt).ToLocalTime().Date == today)
                .Sum(t => t.Fare);
            _lblRevenue.Text = $"Doanh thu hôm nay:\n{total:N0} đ";
        }

        // ── UI factories ──────────────────────────────────────────────────────

        private static Button MakeActionBtn(string text, Color bg, int width)
        {
            var btn = new Button
            {
                Text = text,
                Width = width,
                Height = 36,
                BackColor = bg,
                ForeColor = bg == Color.White ? AppTheme.TextPrimary : Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private static (Panel dot, Label lbl) MakeStepDot(string num, string text)
        {
            var wrapper = new Panel { Width = 48, Height = 48, BackColor = Color.Transparent };
            var dot = new Panel
            {
                Width = 26,
                Height = 26,
                BackColor = AppTheme.BorderLight,
                Location = new Point(11, 0)
            };
            FormHelper.MakeRound(dot, 13);   
            dot.Controls.Add(new Label
            {
                Text = num,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.White
            });
            var lbl = new Label
            {
                Text = text,
                Location = new Point(0, 30),
                Width = 48,
                Height = 16,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 8f),
                ForeColor = AppTheme.TextMuted
            };
            wrapper.Controls.AddRange(new Control[] { dot, lbl });
            return (dot, lbl);
        }

        private static Panel MakeConnector() =>
            new() { Dock = DockStyle.Fill, Height = 2, BackColor = AppTheme.BorderLight, Margin = new Padding(0, 12, 0, 0) };
    }
}
