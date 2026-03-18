using OOP.Application.Services.Interfaces;
using OOP.Application.Validators;
using OOP.Domain.Entities;

namespace OOP.Presentation.CoreForms
{
    public class ProfileForm : Form
    {
        private readonly User _user;
        private readonly IUserService _userService;

        private TextBox _txtName = null!;
        private TextBox _txtPhone = null!;
        private Button _btnSave = null!;
        private Button _btnCancel = null!;
        private readonly ErrorProvider _errorProvider = new();

        public ProfileForm(User user, IUserService userService)
        {
            _user = user ?? throw new ArgumentNullException(nameof(user));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));

            InitForm();
            BuildUI();
        }

        private void InitForm()
        {
            Text = "Thông tin cá nhân";
            Size = new Size(420, 260);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = AppTheme.PageBg;
            Font = new Font("Segoe UI", 10F);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            _errorProvider.BlinkStyle = ErrorBlinkStyle.NeverBlink;
            _errorProvider.ContainerControl = this;
        }

        private void BuildUI()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20)
            };

            var lblName = new Label { Text = "Họ và tên", AutoSize = true, Top = 10, Left = 0 };
            _txtName = new TextBox
            {
                Left = 0,
                Top = 30,
                Width = 350,
                Text = _user.Name
            };

            var lblPhone = new Label { Text = "Số điện thoại", AutoSize = true, Top = 70, Left = 0 };
            _txtPhone = new TextBox
            {
                Left = 0,
                Top = 90,
                Width = 350,
                Text = _user.Phone
            };

            _btnSave = new Button
            {
                Text = "Lưu",
                Width = 90,
                Height = 34,
                Left = 170,
                Top = 140,
                BackColor = AppTheme.Success,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnSave.FlatAppearance.BorderSize = 0;
            _btnSave.Click += async (_, _) => await OnSave();

            _btnCancel = new Button
            {
                Text = "Hủy",
                Width = 90,
                Height = 34,
                Left = 260,
                Top = 140,
                FlatStyle = FlatStyle.Flat
            };
            _btnCancel.Click += (_, _) => Close();

            panel.Controls.Add(lblName);
            panel.Controls.Add(_txtName);
            panel.Controls.Add(lblPhone);
            panel.Controls.Add(_txtPhone);
            panel.Controls.Add(_btnSave);
            panel.Controls.Add(_btnCancel);

            Controls.Add(panel);
        }

        private async Task OnSave()
        {
            _errorProvider.Clear();
            string name = _txtName.Text.Trim();
            string phone = _txtPhone.Text.Trim();

            bool valid = true;
            if (string.IsNullOrWhiteSpace(name))
            {
                _errorProvider.SetError(_txtName, "Họ tên không được để trống.");
                valid = false;
            }

            try
            {
                UserValidator.ValidatePhone(phone);
            }
            catch (Exception ex)
            {
                _errorProvider.SetError(_txtPhone, ex.Message);
                valid = false;
            }

            if (!valid) return;

            try
            {
                await _userService.UpdateUserProfile(_user.Id, name, phone);
                MessageBox.Show("Cập nhật thành công.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}


