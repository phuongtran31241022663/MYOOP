﻿using System.Runtime.Serialization;
using OOP.Domain.Enums;

namespace OOP.Domain.Entities
{
    [DataContract]
    [KnownType(typeof(Passenger))]
    [KnownType(typeof(Driver))]
    [KnownType(typeof(Admin))]
    public abstract class User
    {
        [DataMember]
        public Guid Id { get; private set; }
        // Thông tin cá nhân
        [DataMember]
        public string Name { get; protected set; }

        [DataMember]
        public string Phone { get; protected set; }
        // Mật khẩu
        [DataMember]
        public string PasswordHash { get; protected set; }
        // Trạng thái tài khoản
        [DataMember]
        public bool IsActive { get; protected set; } = true;
        // Vai trò
        [DataMember]
        public UserRole Role { get; private set; }
        protected User() { }
        protected User(Guid id, string name, string phone, string hashedPassword, bool isActive, UserRole role)
        {
            Id = id;
            Name = name;
            Phone = phone;
            PasswordHash = hashedPassword;
            IsActive = isActive;
            Role = role;
        }
        // --- Methods ---
        public void UpdateProfile(string name, string phone)
        {
            Name = name;
            Phone = phone;
        }
        public bool VerifyPassword(string hashedInput)
        {
            return PasswordHash == hashedInput;
        }
        public void UpdatePassword(string newHashedPassword)
        {
            PasswordHash = newHashedPassword;
        }
        public void Deactivate()
        {
            IsActive = false;
        }

        public void Activate()
        {
            IsActive = true;
        }
        public virtual string GetInfo()
        {
            string shortId = Id == Guid.Empty ? "N/A" : Id.ToString()[..8];
            return $"[{Role}] ID: {shortId} | Tên: {Name} | Trạng thái: {(IsActive ? "Active" : "Banned")}";
        }
    }
}
