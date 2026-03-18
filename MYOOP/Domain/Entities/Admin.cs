﻿using OOP.Domain.Enums;
using System.Runtime.Serialization;

namespace OOP.Domain.Entities
{
    [DataContract]
    public class Admin : User
    {
        protected Admin() { }
        public Admin(
            Guid id,
            string name,
            string phone,
            string hashedPassword,
            bool isActive)
            : base(id, name, phone, hashedPassword, isActive, UserRole.Admin)
        {
        }

        public override string GetInfo()
        {
            return "TÀI KHOẢN QUẢN TRỊ VIÊN\n" + base.GetInfo();
        }
    }
}