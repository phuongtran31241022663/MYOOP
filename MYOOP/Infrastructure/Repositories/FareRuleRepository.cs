﻿using OOP.Domain.Entities;
using OOP.Domain.Enums;
using OOP.Domain.Interfaces;
using OOP.Infrastructure.Storage;

namespace OOP.Infrastructure.Repositories
{
    public class FareRuleRepository : BaseRepository<Fare>, IFareRuleRepository
    {
        private volatile bool _seeded = false;
        private readonly SemaphoreSlim _seedLock = new(1, 1);

        private const decimal DefaultCommission = 0.2m;

        public FareRuleRepository(IStorage storage) : base(storage, "farerules.json")
        {
        }

        public async Task EnsureSeeded()
        {
            if (_seeded) return;

            await _seedLock.WaitAsync();
            try
            {
                if (_seeded) return;
                await SeedDefaultRules();
                _seeded = true;
            }
            finally
            {
                _seedLock.Release();
            }
        }

        private async Task SeedDefaultRules()
        {
            await EnsureLoaded();
            if (Items.Count > 0) return;

            // Motorbike: Siêu rẻ
            Items.Add(new Fare(
                vehicleType: VehicleType.Motorbike,
                baseFare: 10000m,      // Giá mở cửa 10k
                pricePerKm: 5000m,     // 5k mỗi km
                minimumFare: 10000m,   // Giá sàn 10k (khớp với logic < 0.5km)
                commissionRate: DefaultCommission));

            // Car: Rẻ nhưng cao hơn xe máy
            Items.Add(new Fare(
                vehicleType: VehicleType.Car,
                baseFare: 15000m,     
                pricePerKm: 10000m,    // 10k mỗi km
                minimumFare: 20000m,   // Giá sàn cho xe hơi
                commissionRate: DefaultCommission));

            await Save();
        }

        public async Task<List<Fare>> GetAll()
        {
            await EnsureSeeded();
            return new List<Fare>(Items);
        }
        public async Task<Fare?> GetById(Guid id)
        {
            await EnsureSeeded();
            return Items.FirstOrDefault(r => r.Id == id);
        }
        public async Task<Fare?> GetByVehicleType(VehicleType type)
        {
            await EnsureSeeded();
            return Items.FirstOrDefault(r => r.VehicleType == type);
        }

        public async Task Add(Fare rule)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));

            await EnsureSeeded();

            await WriteLock.WaitAsync();
            try
            {
                if (Items.Any(r => r.VehicleType == rule.VehicleType))
                    throw new InvalidOperationException(
                        $"Đã tồn tại FareRule cho loại xe '{rule.VehicleType}'.");

                Items.Add(rule);
                await Save();
            }
            finally
            {
                WriteLock.Release();
            }
        }

        public async Task Update(Fare rule)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));

            await EnsureSeeded();

            await WriteLock.WaitAsync();
            try
            {
                var index = Items.FindIndex(r => r.Id == rule.Id);

                if (index == -1)
                    throw new KeyNotFoundException(
                        $"Không tìm thấy FareRule với Id '{rule.Id}'.");

                Items[index] = rule;
                await Save();
            }
            finally
            {
                WriteLock.Release();
            }
        }

        public async Task Remove(Guid ruleId)
        {
            await EnsureSeeded();

            await WriteLock.WaitAsync();
            try
            {
                var count = Items.RemoveAll(r => r.Id == ruleId);

                if (count == 0)
                    throw new KeyNotFoundException(
                        $"Không tìm thấy FareRule với Id '{ruleId}'.");

                await Save();
            }
            finally
            {
                WriteLock.Release();
            }
        }
    }
}