﻿using OOP.Application.Validators;
using OOP.Domain.Entities;
using OOP.Domain.Interfaces;
using System.Security.Cryptography;
using System.Text;
using OOP.Application.Services.Interfaces;

namespace OOP.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepo;

        public UserService(IUserRepository userRepo)
        {
            _userRepo = userRepo ?? throw new ArgumentNullException(nameof(userRepo));
        }       
        // --- Profile ---

        public async Task<User?> GetUserProfile(Guid userId)
        {
            return await _userRepo.GetById(userId);
        }

        public async Task UpdateUserProfile(Guid userId, string name, string phone)
        {
            var user = await GetOrThrow(userId);
            var trimmedPhone = phone.Trim();

            if (!string.Equals(user.Phone, trimmedPhone, StringComparison.OrdinalIgnoreCase))
            {
                if (await _userRepo.ExistsByPhone(trimmedPhone))
                    throw new InvalidOperationException($"Số điện thoại '{trimmedPhone}' đã được đăng ký.");
            }

            user.UpdateProfile(name, trimmedPhone);
            UserValidator.ValidateUserUpdate(user);
            await _userRepo.Update(user);
        }

        public async Task ResetPassword(Guid userId, string newPassword)
        {
            UserValidator.ValidatePassword(newPassword);

            var user = await GetOrThrow(userId);
            user.UpdatePassword(Hash(newPassword));
            await _userRepo.Update(user);
        }

        public async Task DeactivateUser(Guid userId)
        {
            var user = await GetOrThrow(userId);
            user.Deactivate();
            await _userRepo.Update(user);
        }

        public async Task UpdateDriverLocation(Guid driverId, Location location)
        {
            if (location == null) throw new ArgumentNullException(nameof(location));

            var user = await GetOrThrow(driverId);
            if (user is not Driver driver)
                throw new InvalidOperationException("User không phải là Driver.");

            driver.UpdateLocation(location);
            await _userRepo.Update(driver);
        }

        // --- Helpers ---

        private async Task EnsurePhoneNotExists(string phone)
        {
            if (await _userRepo.ExistsByPhone(phone))
                throw new InvalidOperationException($"Số điện thoại '{phone}' đã được đăng ký.");
        }

        private async Task<User> GetOrThrow(Guid userId)
        {
            return await _userRepo.GetById(userId)
                   ?? throw new KeyNotFoundException($"Không tìm thấy user với Id '{userId}'.");
        }

        private static string Hash(string raw)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
            return Convert.ToBase64String(bytes);
        }
    }
}
