using OOP.Application.Services.Interfaces;
using OOP.Application.Validators;
using OOP.Domain.Entities;
using OOP.Domain.Interfaces;

namespace OOP.Application.Services
{
    public class AdminService : IAdminService
    {
        private readonly IUserRepository _userRepo;
        private readonly ITripRepository _tripRepo;
        private readonly IFareRuleRepository _fareRuleRepo;

        public AdminService(
            IUserRepository userRepo,
            ITripRepository tripRepo,
            IFareRuleRepository fareRuleRepo)
        {
            _userRepo = userRepo ?? throw new ArgumentNullException(nameof(userRepo));
            _tripRepo = tripRepo ?? throw new ArgumentNullException(nameof(tripRepo));
            _fareRuleRepo = fareRuleRepo ?? throw new ArgumentNullException(nameof(fareRuleRepo));
        }

        public async Task<List<User>> GetAllUsers()
        {
            return await _userRepo.GetAll();
        }

        public async Task<List<Trip>> GetAllTrips()
        {
            return await _tripRepo.GetAll();
        }

        public async Task<List<FareRule>> GetFareRules()
        {
            return await _fareRuleRepo.GetAll();
        }

        public async Task UpdateFareRule(FareRule rule)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));

            FareRuleValidator.ValidateRule(rule);

            await _fareRuleRepo.Update(rule);
        }

        public async Task DeactivateUser(Guid targetUserId, Guid currentAdminId)
        {
            var targetUser = await _userRepo.GetById(targetUserId);

            // 1. Chặn tự khóa tài khoản của chính mình
            if (targetUserId == currentAdminId)
            {
                throw new InvalidOperationException("Bạn không thể tự khóa tài khoản của chính mình.");
            }

            // 2. Chặn Admin này khóa Admin khác (Tùy theo quy định đồ án)
            if (targetUser is Admin)
            {
                throw new InvalidOperationException("Không có quyền tác động lên tài khoản Quản trị viên khác.");
            }

            // Nếu vượt qua các kiểm tra trên thì mới thực hiện khóa
            targetUser.Deactivate();
            await _userRepo.Update(targetUser);
        }

        public async Task ActivateUser(Guid userId)
        {
            var user = await GetOrThrow(userId);
            user.Activate();
            await _userRepo.Update(user);
        }

        // --- Helpers ---

        private async Task<User> GetOrThrow(Guid userId)
        {
            return await _userRepo.GetById(userId)
                   ?? throw new KeyNotFoundException($"Không tìm thấy user '{userId}'.");
        }
    }
}