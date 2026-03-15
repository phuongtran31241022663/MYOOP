using OOP.Application.Interfaces;
using OOP.Application.Services.Interfaces;
using OOP.Domain.Entities;
using OOP.Domain.Enums;
using OOP.Domain.Interfaces;

namespace OOP.Application.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepo;
        private readonly IFareRuleRepository _fareRuleRepo;

        // Việc TopUp ví tài xế là trách nhiệm của TripService (đã có sẵn).
        public PaymentService(
            IPaymentRepository paymentRepo,
            IFareRuleRepository fareRuleRepo)
        {
            _paymentRepo = paymentRepo ?? throw new ArgumentNullException(nameof(paymentRepo));
            _fareRuleRepo = fareRuleRepo ?? throw new ArgumentNullException(nameof(fareRuleRepo));
        }

        public async Task<Payment> CreatePayment(Trip trip)
        {
            if (trip == null)
                throw new ArgumentNullException(nameof(trip));

            if (trip.Status != TripStatus.Completed)
                throw new InvalidOperationException("Chỉ tạo payment cho trip đã hoàn thành.");

            if (trip.Fare <= 0)
                throw new InvalidOperationException("Trip chưa có cước phí. Gọi FareService.CalculateFare() trước.");

            var rule = await _fareRuleRepo.GetByVehicleType(trip.VehicleType)
                       ?? throw new InvalidOperationException(
                           $"Không tìm thấy bảng giá cho '{trip.VehicleType}'.");

            var payment = new Payment(trip.Id, trip.Fare, rule.CommissionRate);

            await _paymentRepo.Add(payment);
            return payment;
        }

        public async Task ProcessPayment(Guid paymentId)
        {
            var payment = await GetOrThrow(paymentId);

            // TopUp ví tài xế được xử lý trong TripService.CompleteTrip() sau khi gọi hàm này.
            payment.MarkPaid();
            await _paymentRepo.Update(payment);
        }

        public async Task<Payment?> GetPayment(Guid paymentId)
        {
            return await _paymentRepo.GetById(paymentId);
        }

        public async Task<Payment?> GetPaymentByTrip(Guid tripId)
        {
            return await _paymentRepo.GetByTripId(tripId);
        }

        // --- Helpers ---

        private async Task<Payment> GetOrThrow(Guid paymentId)
        {
            return await _paymentRepo.GetById(paymentId)
                   ?? throw new KeyNotFoundException(
                       $"Không tìm thấy payment với Id '{paymentId}'.");
        }
    }
}