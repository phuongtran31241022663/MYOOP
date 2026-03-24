using System;
using System.Linq;

namespace OOP.Domain.Validators
{
    public static class UserValidator
    {
        public const int PhoneLength = 10;
        public const int MinPasswordLength = 6;

        /// <summary>
        /// Chuẩn hóa và kiểm tra số điện thoại: xóa ký tự đặc biệt, kiểm tra độ dài và đầu số 0
        /// </summary>
        public static string NormalizePhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                throw new ArgumentException("Số điện thoại không được để trống.");

            // Loại bỏ các ký tự ngăn cách phổ biến để lấy chuỗi số thuần túy
            string digits = phone
                .Replace(" ", "")
                .Replace("-", "")
                .Replace(".", "") // Thêm dấu chấm vì người Việt hay dùng (vd: 090.123.4567)
                .Replace("+", "");

            if (!digits.All(char.IsDigit))
                throw new ArgumentException("Số điện thoại chỉ được chứa các chữ số.");

            if (!digits.StartsWith("0"))
                throw new ArgumentException("Số điện thoại phải bắt đầu bằng chữ số 0.");

            if (digits.Length != PhoneLength)
                throw new ArgumentException($"Số điện thoại phải có đúng {PhoneLength} chữ số.");

            return digits;
        }

        /// <summary>
        /// Kiểm tra tính hợp lệ của mật khẩu
        /// </summary>
        public static void ValidatePassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Mật khẩu không được để trống.");

            if (password.Length < MinPasswordLength)
                throw new ArgumentException($"Mật khẩu phải có ít nhất {MinPasswordLength} ký tự.");
        }
    }
}