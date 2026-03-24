using System.Text.RegularExpressions;

namespace OOP.Application.Validators
{
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