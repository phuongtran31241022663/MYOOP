using OOP.Application.Services.Interfaces;
using OOP.Domain.Entities;
using OOP.Domain.Enums;
using OOP.Domain.Interfaces;

namespace OOP
{
    public class FareRuleForm : Form
    {
        // --- Controls ---
        private ComboBox ComboVehicleType = null!;
        private TextBox TextBoxBaseFare = null!;
        private TextBox TextBoxPricePerKm = null!;
        private TextBox TextBoxMinimumFare = null!;
        private TextBox TextBoxCommissionRate = null!;
        private Button ButtonSave = null!;
        private Button ButtonCancel = null!;

        private readonly IFareRuleRepository _fareRuleRepo;
        private readonly Guid? _fareRuleId;

        public FareRuleForm(IFareRuleRepository fareRuleRepo, Guid? fareRuleId = null)
        {
            _fareRuleRepo = fareRuleRepo ?? throw new ArgumentNullException(nameof(fareRuleRepo));
            _fareRuleId = fareRuleId;

            InitializeUI();
            Load += async (_, _) => await OnFormLoad();
        }

        private void InitializeUI()
        {
            Text = _fareRuleId == null ? "Thêm bảng giá cước" : "Chỉnh sửa bảng giá cước";
            Size = new Size(420, 340);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            // --- Labels ---
            AddLabel("Loại xe:", 40, 30);
            AddLabel("Giá khởi điểm:", 40, 70);
            AddLabel("Giá / km:", 40, 110);
            AddLabel("Giá tối thiểu:", 40, 150);
            // FIX: Label cho commission
            AddLabel("Hoa hồng (%):", 40, 190);

            // --- Inputs ---
            ComboVehicleType = new ComboBox
            {
                Location = new Point(200, 28),
                Width = 180,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            ComboVehicleType.DataSource = Enum.GetValues(typeof(VehicleType));

            TextBoxBaseFare = MakeInput(200, 68);
            TextBoxPricePerKm = MakeInput(200, 108);
            TextBoxMinimumFare = MakeInput(200, 148);
            // FIX: CommissionRate input — nhập phần trăm (vd: 10 = 10%)
            TextBoxCommissionRate = MakeInput(200, 188);
            TextBoxCommissionRate.PlaceholderText = "Ví dụ: 10 (= 10%)";

            ButtonSave = new Button
            {
                Text = "Lưu",
                Location = new Point(100, 255),
                Width = 100,
                Height = 36
            };
            ButtonCancel = new Button
            {
                Text = "Hủy",
                Location = new Point(215, 255),
                Width = 100,
                Height = 36
            };

            ButtonSave.Click += async (_, _) => await OnSaveClicked();
            ButtonCancel.Click += (_, _) => Close();

            Controls.Add(ComboVehicleType);
            Controls.Add(TextBoxBaseFare);
            Controls.Add(TextBoxPricePerKm);
            Controls.Add(TextBoxMinimumFare);
            Controls.Add(TextBoxCommissionRate);
            Controls.Add(ButtonSave);
            Controls.Add(ButtonCancel);
        }

        private async Task OnFormLoad()
        {
            if (_fareRuleId == null) return;

            var rule = await _fareRuleRepo.GetById(_fareRuleId.Value);
            if (rule == null)
            {
                MessageBox.Show("Không tìm thấy bảng giá cước.", "Lỗi");
                Close();
                return;
            }

            ComboVehicleType.SelectedItem = rule.VehicleType;
            TextBoxBaseFare.Text = rule.BaseFare.ToString();
            TextBoxPricePerKm.Text = rule.PricePerKm.ToString();
            TextBoxMinimumFare.Text = rule.MinimumFare.ToString();
            // FIX: Hiển thị CommissionRate dưới dạng % cho dễ nhập (0.10 → "10")
            TextBoxCommissionRate.Text = (rule.CommissionRate * 100).ToString("0.##");
        }

        private async Task OnSaveClicked()
        {
            if (!TryParseInputs(out decimal baseFare, out decimal pricePerKm,
                                out decimal minimumFare, out decimal commissionRate))
                return;

            try
            {
                var vehicleType = (VehicleType)ComboVehicleType.SelectedItem!;

                if (_fareRuleId != null)
                {
                    var rule = await _fareRuleRepo.GetById(_fareRuleId.Value);
                    if (rule == null) { MessageBox.Show("Không tìm thấy bản ghi."); return; }

                    rule.Update(baseFare, pricePerKm, minimumFare, commissionRate);
                    await _fareRuleRepo.Update(rule);
                }
                else
                {
                    var rule = new FareRule(vehicleType, baseFare, pricePerKm, minimumFare, commissionRate);
                    await _fareRuleRepo.Add(rule);
                }

                MessageBox.Show("Đã lưu bảng giá cước thành công!", "Thành công",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show($"Dữ liệu không hợp lệ: {ex.Message}", "Lỗi nhập liệu",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lưu: {ex.Message}", "Lỗi hệ thống",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── Validation ───────────────────────────────────────────────────────

        private bool TryParseInputs(
            out decimal baseFare,
            out decimal pricePerKm,
            out decimal minimumFare,
            out decimal commissionRate)
        {
            baseFare = pricePerKm = minimumFare = commissionRate = 0;

            if (!decimal.TryParse(TextBoxBaseFare.Text, out baseFare) || baseFare < 0)
            {
                ShowInputError("Giá khởi điểm không hợp lệ.", TextBoxBaseFare); return false;
            }
            if (!decimal.TryParse(TextBoxPricePerKm.Text, out pricePerKm) || pricePerKm < 0)
            {
                ShowInputError("Giá/km không hợp lệ.", TextBoxPricePerKm); return false;
            }
            if (!decimal.TryParse(TextBoxMinimumFare.Text, out minimumFare) || minimumFare < 0)
            {
                ShowInputError("Giá tối thiểu không hợp lệ.", TextBoxMinimumFare); return false;
            }

            // FIX: Parse commission từ % (vd: "10") → decimal (0.10)
            if (!decimal.TryParse(TextBoxCommissionRate.Text, out decimal commissionPct)
                || commissionPct < 0 || commissionPct > 100)
            {
                ShowInputError("Hoa hồng phải từ 0 đến 100 (%).", TextBoxCommissionRate);
                return false;
            }
            commissionRate = commissionPct / 100m;
            return true;
        }

        private static void ShowInputError(string message, Control focusTarget)
        {
            MessageBox.Show(message, "Lỗi nhập liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            focusTarget.Focus();
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private void AddLabel(string text, int x, int y)
        {
            Controls.Add(new Label
            {
                Text = text,
                Location = new Point(x, y + 3),
                AutoSize = true
            });
        }

        private static TextBox MakeInput(int x, int y) =>
            new TextBox { Location = new Point(x, y), Width = 180 };
    }
}