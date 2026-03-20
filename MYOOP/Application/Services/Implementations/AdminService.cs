using OOP.Application.Services.Interfaces;
using OOP.Application.Services.Models;
using OOP.Domain.Entities;
using OOP.Domain.Interfaces;
using OOP.Domain.Enums;

namespace OOP.Application.Services
{
    public class AdminService : IAdminService
    {
        private readonly IUserRepository _userRepo;
        private readonly ITripRepository _tripRepo;
        private readonly IFareRepository _fareRuleRepo;
        private readonly IPaymentRepository _paymentRepo;

        public AdminService(
            IUserRepository userRepo,
            ITripRepository tripRepo,
            IFareRepository fareRuleRepo,
            IPaymentRepository paymentRepo)
        {
            _userRepo = userRepo ?? throw new ArgumentNullException(nameof(userRepo));
            _tripRepo = tripRepo ?? throw new ArgumentNullException(nameof(tripRepo));
            _fareRuleRepo = fareRuleRepo ?? throw new ArgumentNullException(nameof(fareRuleRepo));
            _paymentRepo = paymentRepo ?? throw new ArgumentNullException(nameof(paymentRepo));
        }

        public async Task<List<User>> GetAllUsers()
        {
            return await _userRepo.GetAll();
        }

        public async Task<List<Trip>> GetAllTrips()
        {
            return await _tripRepo.GetAll();
        }

        public async Task<List<Fare>> GetFareRules()
        {
            return await _fareRuleRepo.GetAll();
        }

        public async Task<Fare> CreateFareRule(Fare rule)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));

            // Validation is handled by property setters
            await _fareRuleRepo.Add(rule);
            return rule;
        }

     public async Task UpdateFareRule(Fare rule)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));

            // Validation is handled by property setters
            await _fareRuleRepo.Update(rule);
        }

        public async Task DeactivateUser(Guid targetId, Guid adminId)
        {
            var user = await GetOrThrow(targetId);

            // Chỉ Passenger và Driver mới có IsActive - Admin luôn active
            switch (user)
            {
                case Passenger passenger:
                    passenger.Deactivate(adminId);
                    break;
                case Driver driver:
                    driver.Deactivate(adminId);
                    break;
                case Admin:
                    throw new InvalidOperationException("Không thể khóa tài khoản admin.");
                default:
                    throw new InvalidOperationException("Loại người dùng không hợp lệ.");
            }

            await _userRepo.Update(user);
        }
        public async Task ActivateUser(Guid userId)
        {
            var user = await GetOrThrow(userId);

            // Chỉ Passenger và Driver mới có IsActive - Admin luôn active
            switch (user)
            {
                case Passenger passenger:
                    passenger.Activate();
                    break;
                case Driver driver:
                    driver.Activate();
                    break;
                case Admin:
                    throw new InvalidOperationException("Tài khoản admin đã luôn hoạt động.");
                default:
                    throw new InvalidOperationException("Loại người dùng không hợp lệ.");
            }

            await _userRepo.Update(user);
        }

        public async Task<TripReport> GetTripReport()
        {
            var trips = await _tripRepo.GetAll();
            var payments = await _paymentRepo.GetAll();

            int totalTrips = trips.Count;
            decimal totalRevenue = trips
                .Where(t => t.Status == TripStatus.Completed)
                .Sum(t => t.Fare);

            decimal totalCommission = payments.Sum(p => p.Commission);
            decimal totalDriverIncome = payments.Sum(p => p.DriverIncome);

            return new TripReport
            {
                TotalTrips = totalTrips,
                TotalRevenue = totalRevenue,
                TotalCommission = totalCommission,
                TotalDriverIncome = totalDriverIncome
            };
        }

        // --- Helpers ---

        private async Task<User> GetOrThrow(Guid userId)
        {
            return await _userRepo.GetById(userId)
                   ?? throw new KeyNotFoundException($"Không tìm thấy user '{userId}'.");
        }
    }
}
