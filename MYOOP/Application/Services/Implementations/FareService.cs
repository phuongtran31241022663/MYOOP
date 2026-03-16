using OOP.Application.Services.Interfaces;
using OOP.Domain.Entities;
using OOP.Domain.Enums;
using OOP.Domain.Interfaces;

namespace OOP.Application.Services
{
    public class FareService : IFareService
    {
        private readonly IFareRuleRepository _fareRuleRepo;

        public FareService(IFareRuleRepository fareRuleRepo)
        {
            _fareRuleRepo = fareRuleRepo ?? throw new ArgumentNullException(nameof(fareRuleRepo));
        }

        public async Task<Fare?> GetFareRule(VehicleType vehicleType)
        {
            return await _fareRuleRepo.GetByVehicleType(vehicleType);
        }

        /// <summary>
        /// Calculates the fare for the given trip, applies it to the trip via
        /// <see cref="Trip.ApplyFare"/>, and returns the computed amount.
        /// The side-effect (ApplyFare) is intentional: the caller is responsible
        /// for persisting the updated trip afterwards.
        /// </summary>
        public async Task<decimal> CalculateFare(Trip trip)
        {
            if (trip == null) throw new ArgumentNullException(nameof(trip));

            var rule = await _fareRuleRepo.GetByVehicleType(trip.VehicleType)
                ?? throw new InvalidOperationException(
                    $"Không tìm thấy cấu hình giá cho loại xe '{trip.VehicleType}'.");

            if (trip.Distance < 0)
                throw new InvalidOperationException("Chuyến đi chưa có khoảng cách hợp lệ.");

            decimal fare = rule.CalculateFare(trip.Distance);

            trip.ApplyFare(fare);   // intentional side-effect — see XML doc above

            return fare;
        }
    }
}
