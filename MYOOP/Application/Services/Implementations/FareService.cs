using OOP.Application.Services.Interfaces;
using OOP.Domain.Entities;
using OOP.Domain.Enums;
using OOP.Domain.Interfaces;

namespace OOP.Application.Services
{
    public class FareService : IFareService
    {
        private readonly IFareRepository _fareRuleRepo;

        public FareService(IFareRepository fareRuleRepo)
        {
            _fareRuleRepo = fareRuleRepo ?? throw new ArgumentNullException(nameof(fareRuleRepo));
        }

        public async Task<Fare?> GetFareRule(VehicleType VehicleType)
        {
            return await _fareRuleRepo.GetByVehicleType(VehicleType);
        }
        public async Task<decimal> CalculateFare(Trip trip)
        {
            if (trip == null) throw new ArgumentNullException(nameof(trip));

            var rule = await _fareRuleRepo.GetByVehicleType(trip.VehicleType)
                ?? throw new InvalidOperationException(
                    $"Không tìm thấy cấu hình giá cho loại xe '{trip.VehicleType}'.");
            return rule.CalculateFare(trip.Distance);
        }
    }
}
