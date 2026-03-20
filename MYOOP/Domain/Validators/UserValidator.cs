using System;
using System.Linq;

namespace OOP.Domain.Validators
{
    public static class UserValidator
    {
        public const int PhoneLength = 10;
        public const int MinPasswordLength = 6;

        /// <summary>
        /// Validates phone number format: exactly 10 digits, starts with 0
        /// </summary>
        public static void ValidatePhone(string phone)
        {
            NormalizePhone(phone); // Throws if invalid
        }

        /// <summary>
        /// Validates and normalizes phone number. Returns trimmed 10-digit phone.
        /// </summary>
        public static string NormalizePhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                throw new ArgumentException("Số điện thoại không được để trống.");

            string trimmed = phone.Trim();

            if (!trimmed.All(char.IsDigit))
                throw new ArgumentException("Số điện thoại chỉ được chứa chữ số.");

            if (!trimmed.StartsWith("0"))
                throw new ArgumentException("Số điện thoại phải bắt đầu bằng 0.");

            if (trimmed.Length != PhoneLength)
                throw new ArgumentException($"Số điện thoại phải có {PhoneLength} chữ số.");

            return trimmed;
        }

        /// <summary>
        /// Validates password: not empty, minimum length
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
