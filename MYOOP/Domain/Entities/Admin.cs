﻿using System.Runtime.Serialization;

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
            string password)
            : base(id, name, phone, password)
        {
            // Admin luôn active - không cần IsActive property
        }

        public override string GetInfo()
        {
            return "TÀI KHOẢN QUẢN TRỊ VIÊN\n" + base.GetInfo();
        }
    }
}
