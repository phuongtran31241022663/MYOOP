﻿using OOP.Domain.Entities;

namespace OOP.Application.Services.Interfaces
{
    public interface IPaymentService
    {
        Task<Payment> CreatePayment(Trip trip);

        Task ProcessPayment(Guid paymentId);

        Task<Payment?> GetPayment(Guid paymentId);
    }
}