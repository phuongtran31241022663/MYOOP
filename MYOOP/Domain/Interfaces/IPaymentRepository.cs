﻿using OOP.Domain.Entities;

namespace OOP.Domain.Interfaces
{
    public interface IPaymentRepository
    {
        Task<List<Payment>> GetAll();
        Task<Payment?> GetById(Guid paymentId);
        Task<Payment?> GetByTripId(Guid tripId);
        Task Add(Payment payment);
        Task Update(Payment payment);
    }
}