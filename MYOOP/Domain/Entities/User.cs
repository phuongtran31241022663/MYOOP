using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;

namespace OOP.Domain.Entities
{
    [DataContract]
    [KnownType(typeof(Passenger))]
    [KnownType(typeof(Driver))]
    [KnownType(typeof(Admin))]
    public abstract class User
    {
        #region Properties
        [DataMember] public Guid Id { get; private set; }
        // Thông tin cá nhân
        private string name = string.Empty;
        [DataMember]
        public string Name
        {
            get => name;
            protected set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Họ tên không được để trống.");

                name = value;
            }
        }
        private string phone = string.Empty;
        [DataMember]
        public string Phone
        {
            get => phone;
            protected set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Số điện thoại không được để trống.");

                string digits = value.Replace(" ", "").Replace("+", "").Replace("-", "");

                if (!digits.All(char.IsDigit))
                    throw new ArgumentException("Số điện thoại chỉ được chứa chữ số.");

                if (!digits.StartsWith("0"))
                    throw new ArgumentException("Số điện thoại phải bắt đầu bằng 0.");

                if (digits.Length != 10)
                    throw new ArgumentException("Số điện thoại phải có 10 chữ số.");

                phone = digits;
            }
        }
        // Mật khẩu
        private string password = string.Empty;
        [DataMember]
        public string Password
        {
            get => password;
            protected set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Mật khẩu không được để trống.");

                if (value.Length < 6)
                    throw new ArgumentException("Mật khẩu phải có ít nhất 6 ký tự.");

                password = value;
            }
        }
        #endregion
        #region Constructors
        protected User() { name = string.Empty; phone = string.Empty; password = string.Empty; }
        protected User(Guid id, string name, string phone, string password)
        {
            List<string> errors = new List<string>();
            if (id == Guid.Empty) errors.Add("ID không hợp lệ.");
            else Id = id;

            // Validate raw password before hashing
            if (string.IsNullOrWhiteSpace(password))
                errors.Add("Mật khẩu không được để trống.");
            else if (password.Length < 6)
                errors.Add("Mật khẩu phải có ít nhất 6 ký tự.");

            try { Name = name; }
            catch (ArgumentException ex) { errors.Add(ex.Message); }
            try { Phone = phone; }
            catch (ArgumentException ex) { errors.Add(ex.Message); }

            if (errors.Count > 0)
            {
                throw new ArgumentException(string.Join("\n", errors));
            }

            Name = name;
            Phone = phone;
            // Store hashed password directly without validation (hash will be longer than 6 chars)
            this.password = HashPassword(password);
        }
        #endregion
        #region Methods
        public void UpdateName(string newName)
        {
            Name = newName;
        }
        public void UpdatePhone(string newPhone)
        {
            Phone = newPhone;
        }
        public static string HashPassword(string rawPassword)
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(rawPassword);
            byte[] hashBytes = SHA256.HashData(inputBytes);
            return Convert.ToBase64String(hashBytes);
        }
        public bool VerifyPassword(string rawInput)
        {
            if (string.IsNullOrEmpty(Password))
                return false;
                
            try
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(rawInput);
                byte[] hashBytes = SHA256.HashData(inputBytes);
                return CryptographicOperations.FixedTimeEquals(
                    Convert.FromBase64String(Password),
                    hashBytes
                );
            }
            catch (FormatException)
            {
                return false;
            }
        }
        public void ChangePassword(string oldRaw, string newRaw)
        {
            if (!VerifyPassword(oldRaw))
                throw new UnauthorizedAccessException("Sai mật khẩu cũ.");

            if (oldRaw == newRaw)
                throw new InvalidOperationException("Mật khẩu mới không được trùng với mật khẩu cũ.");

            // Validate new password length before hashing
            if (newRaw.Length < 6)
                throw new ArgumentException("Mật khẩu mới phải có ít nhất 6 ký tự.");

            // Store hashed password directly
            this.password = HashPassword(newRaw);
        }
        public virtual string GetInfo()
        {
            string shortId = Id == Guid.Empty ? "N/A" : Id.ToString()[..8];
            return $"ID: {shortId} | Tên: {Name}";
        }
        #endregion
    }
}
