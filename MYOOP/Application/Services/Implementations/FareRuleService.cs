using OOP.Application.Services.Interfaces;
using OOP.Domain.Entities;
using OOP.Domain.Enums;
using OOP.Domain.Interfaces;

namespace OOP.Application.Services
{
    public class FareRuleService : IFareRuleService
    {
        private readonly IFareRuleRepository _fareRuleRepo;

        public FareRuleService(IFareRuleRepository fareRuleRepo)
        {
            _fareRuleRepo = fareRuleRepo ?? throw new ArgumentNullException(nameof(fareRuleRepo));
        }

        public async Task<FareRule?> GetFareRule(VehicleType vehicleType)
        {
            return await _fareRuleRepo.GetByVehicleType(vehicleType);
        }

        public async Task<decimal> CalculateFare(Trip trip)
        {
            if (trip == null) throw new ArgumentNullException(nameof(trip));

            var rule = await _fareRuleRepo.GetByVehicleType(trip.VehicleType)
                        ?? throw new InvalidOperationException($"Không tìm thấy cấu hình giá cho loại xe '{trip.VehicleType}'.");

            var fare = rule.CalculateFare(trip.Distance);

            trip.ApplyFare(fare);

            return fare;
        }
    }
}