// ─── EditFareRuleForm (dialog nội tuyến) ──────────────────────────────────────
// Drop this class into AdminForm.cs, replacing the existing EditFareRuleForm.

using OOP.Domain.Entities;

public class EditFareRuleForm : Form
{
    // Kết quả sau khi nhấn OK — AdminDashboardForm đọc từ đây
    public decimal NewBaseFare { get; private set; }
    public decimal NewPricePerKm { get; private set; }
    public decimal NewMinimumFare { get; private set; }
    public decimal NewCommissionRate { get; private set; }  // 0..1

    // FIX #1: removed NewPricePerMinute — Fare entity has no PricePerMinute.
    //         The field existed here but was never part of Fare.Update(), so
    //         the value was silently discarded. The matching _numPricePerMinute
    //         was null! → NullReferenceException on every Save click.

    private NumericUpDown _numBaseFare = null!;
    private NumericUpDown _numPricePerKm = null!;
    private NumericUpDown _numMinimumFare = null!;
    private NumericUpDown _numCommission = null!;

    private static readonly Color Blue = Color.FromArgb(0, 122, 255);
    private static readonly Color Green = Color.FromArgb(0, 150, 80);

    public EditFareRuleForm(Fare rule)
    {
        Text = $"Chỉnh sửa bảng giá – {rule.VehicleType}";
        // FIX #2: reduced height from 440 to match actual row count (no phantom row 3)
        Size = new Size(440, 380);
        MinimumSize = new Size(400, 360);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.White;
        Font = new Font("Segoe UI", 10F);

        BuildUI(rule);
    }

    private void BuildUI(Fare rule)
    {
        // FIX #2: RowCount = 6 (header + 4 fields + button row).
        //         Old code had RowCount = 7 but only placed content in rows
        //         0,1,2,4,5,6 — row 3 was blank, wasting 48px and making
        //         the layout look misaligned.
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 6,
            ColumnCount = 2,
            Padding = new Padding(24, 20, 24, 12)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 46));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 54));
        for (int i = 0; i < 6; i++)
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

        // Row 0 — header
        var lblHeader = new Label
        {
            Text = $"Loại xe: {rule.VehicleType}",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = Blue,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };
        layout.Controls.Add(lblHeader, 0, 0);
        layout.SetColumnSpan(lblHeader, 2);

        // Rows 1–4 — inputs (consecutive, no gap)
        _numBaseFare = AddNumRow(layout, "Giá mở cửa (đ):", 1, rule.BaseFare, 0, 500_000);
        _numPricePerKm = AddNumRow(layout, "Giá / km (đ):", 2, rule.PricePerKm, 0, 100_000);
        _numMinimumFare = AddNumRow(layout, "Giá tối thiểu (đ):", 3, rule.MinimumFare, 0, 500_000);
        _numCommission = AddNumRow(layout, "Hoa hồng (%):", 4, rule.CommissionRate * 100, 0, 100,
                                    decimalPlaces: 0);

        // Row 5 — buttons
        var btnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 6, 0, 0)
        };

        var btnCancel = new Button
        {
            Text = "Hủy",
            Width = 90,
            Height = 34,
            DialogResult = DialogResult.Cancel,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 10)
        };
        btnCancel.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);

        var btnSave = new Button
        {
            Text = "Lưu",
            Width = 90,
            Height = 34,
            BackColor = Green,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };
        btnSave.FlatAppearance.BorderSize = 0;
        btnSave.Click += OnSaveClicked;

        btnPanel.Controls.Add(btnCancel);
        btnPanel.Controls.Add(btnSave);

        layout.Controls.Add(btnPanel, 0, 5);
        layout.SetColumnSpan(btnPanel, 2);

        Controls.Add(layout);
        AcceptButton = btnSave;
        CancelButton = btnCancel;
    }

    private void OnSaveClicked(object? sender, EventArgs e)
    {
        decimal baseFare = _numBaseFare.Value;
        decimal perKm = _numPricePerKm.Value;
        decimal minFare = _numMinimumFare.Value;
        decimal commission = _numCommission.Value / 100m;

        if (minFare < baseFare)
        {
            MessageBox.Show("Giá tối thiểu không được thấp hơn giá mở cửa.",
                "Không hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _numMinimumFare.Focus();
            return;
        }

        NewBaseFare = baseFare;
        NewPricePerKm = perKm;
        NewMinimumFare = minFare;
        NewCommissionRate = commission;

        DialogResult = DialogResult.OK;
    }

    private static NumericUpDown AddNumRow(
        TableLayoutPanel layout,
        string labelText,
        int row,
        decimal value,
        decimal min,
        decimal max,
        int decimalPlaces = 0)
    {
        var lbl = new Label
        {
            Text = labelText,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight,
            ForeColor = Color.FromArgb(70, 70, 70)
        };

        var num = new NumericUpDown
        {
            Dock = DockStyle.Fill,
            Minimum = min,
            Maximum = max,
            Value = Math.Clamp(value, min, max),
            DecimalPlaces = decimalPlaces,
            ThousandsSeparator = decimalPlaces == 0,
            Font = new Font("Segoe UI", 10.5f)
        };

        layout.Controls.Add(lbl, 0, row);
        layout.Controls.Add(num, 1, row);
        return num;
    }
}
