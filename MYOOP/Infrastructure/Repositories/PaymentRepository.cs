using OOP.Domain.Entities;
using OOP.Domain.Interfaces;
using OOP.Infrastructure.Storage;

namespace OOP.Infrastructure.Repositories
{
    public class PaymentRepository : BaseRepository<Payment>, IPaymentRepository
    {
        public PaymentRepository(IStorage storage)
            : base(storage, "payments.json") { }

        public async Task<List<Payment>> GetAll()
        {
            await EnsureLoaded();
            return new List<Payment>(Items);
        }

        public async Task<Payment?> GetById(Guid paymentId)
        {
            await EnsureLoaded();
            return Items.FirstOrDefault(p => p.Id == paymentId);
        }

        public async Task<Payment?> GetByTripId(Guid tripId)
        {
            await EnsureLoaded();
            return Items.FirstOrDefault(p => p.TripId == tripId);
        }

        public async Task Add(Payment payment)
        {
            if (payment == null) throw new ArgumentNullException(nameof(payment));

            await EnsureLoaded();

            await WriteLock.WaitAsync();
            try
            {
                if (Items.Any(p => p.Id == payment.Id))
                    throw new InvalidOperationException(
                        $"Payment với Id '{payment.Id}' đã tồn tại.");

                if (Items.Any(p => p.TripId == payment.TripId))
                    throw new InvalidOperationException(
                        $"Chuyến đi '{payment.TripId}' đã có payment.");

                Items.Add(payment);
                await Save();
            }
            finally
            {
                WriteLock.Release();
            }
        }

        public async Task Update(Payment payment)
        {
            if (payment == null) throw new ArgumentNullException(nameof(payment));

            await EnsureLoaded();

            await WriteLock.WaitAsync();
            try
            {
                var index = Items.FindIndex(p => p.Id == payment.Id);

                if (index == -1)
                    throw new KeyNotFoundException(
                        $"Không tìm thấy payment với Id '{payment.Id}'.");

                Items[index] = payment;
                await Save();
            }
            finally
            {
                WriteLock.Release();
            }
        }
    }
}