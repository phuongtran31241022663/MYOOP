using System.Text.RegularExpressions;

namespace OOP.Domain.Validators
{
    public class DomainValidators
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
        public class VehicleValidator
        {
            public List<string> Validate(
                string plateNumber, // Biển số xe
                string brand,       // Hãng xe
                string model,       // Dòng xe
                string color,       // Màu sắc
                int capacity,       // Số chỗ ngồi
                bool isCar)         // Có phải là ô tô không?
            {
                var errors = new List<string>();

                // Kiểm tra định dạng biển số (Ví dụ: 29A-12345 hoặc 51B-1234)
                if (!Regex.IsMatch(plateNumber, @"^\d{2}[A-Z]-\d{4,5}$"))
                {
                    errors.Add("Định dạng biển số không hợp lệ (Ví dụ đúng: 29A-12345).");
                }

                // Kiểm tra số chỗ ngồi cơ bản
                if (capacity <= 0)
                {
                    errors.Add("Số chỗ ngồi phải lớn hơn 0.");
                }

                // Logic dành riêng cho Ô tô
                if (isCar && capacity < 4)
                {
                    errors.Add("Xe ô tô phải có ít nhất 4 chỗ ngồi.");
                }

                // Logic dành riêng cho Xe máy
                if (!isCar && capacity != 2)
                {
                    errors.Add("Xe máy mặc định phải có 2 chỗ ngồi.");
                }

                return errors;
            }
        }
    }
}
