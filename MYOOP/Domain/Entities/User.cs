using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using OOP.Domain.Validators;

namespace OOP.Domain.Entities
{
    [DataContract]
    [KnownType(typeof(Passenger))]
    [KnownType(typeof(Driver))]
    [KnownType(typeof(Admin))]
    public abstract class User
    {
        // Cấu hình PBKDF2
        private const int SaltSize = 16;
        private const int HashSize = 32;
        private const int Iterations = 100000;

        #region Properties
        [DataMember] public Guid Id { get; private set; }
        [DataMember] public DateTime CreatedAt { get; protected set; }
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
                name = value.Trim();
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
                phone = value.Trim();
            }
        }
        // Mật khẩu (đã hash với format: base64(salt):base64(hash))
        private string password = string.Empty;
        [DataMember]
        public string Password
        {
            get => password;
            private set => password = value;
        }
        #endregion
        #region Constructors
        protected User() { name = string.Empty; phone = string.Empty; password = string.Empty; CreatedAt = DateTime.UtcNow; }
        protected User(Guid id, string name, string phone, string password)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("ID không hợp lệ.");
            Id = id;
            CreatedAt = DateTime.UtcNow;

            Name = name;
            Phone = DomainValidators.UserValidator.NormalizePhone(phone);
            DomainValidators.UserValidator.ValidatePassword(password);
            // Store hashed password with PBKDF2
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
            Phone = DomainValidators.UserValidator.NormalizePhone(newPhone);
        }

        /// <summary>
        /// Hash password using PBKDF2 with random salt.
        /// Returns format: base64(salt):base64(hash)
        /// </summary>
        public static string HashPassword(string rawPassword)
        {
            // Generate random salt
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);

            // Hash password using PBKDF2
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                rawPassword,
                salt,
                Iterations,
                HashAlgorithmName.SHA256,
                HashSize);

            // Return salt:hash format
            return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
        }

        public bool VerifyPassword(string rawInput)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(rawInput))
                return false;
            var parts = password.Split(':');
            if (parts.Length != 2)
                return false;

            try
            {
                byte[] salt = Convert.FromBase64String(parts[0]);
                byte[] storedHash = Convert.FromBase64String(parts[1]);

                byte[] computedHash = Rfc2898DeriveBytes.Pbkdf2(
                    rawInput,
                    salt,
                    Iterations,
                    HashAlgorithmName.SHA256,
                    HashSize);

                return CryptographicOperations.FixedTimeEquals(storedHash, computedHash);
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

            // Store new hashed password
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
