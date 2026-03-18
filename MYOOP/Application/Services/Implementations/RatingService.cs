﻿using OOP.Application.Services.Interfaces;
using OOP.Application.Validators;
using OOP.Domain.Entities;
using OOP.Domain.Interfaces;

namespace OOP.Application.Services
{
    public class RatingService : IRatingService
    {
        private readonly IRatingRepository _ratingRepo;
        private readonly IUserRepository _userRepo;
        private readonly ITripRepository _tripRepo;

        public RatingService(
            IRatingRepository ratingRepo,
            IUserRepository userRepo,
            ITripRepository tripRepo)
        {
            _ratingRepo = ratingRepo ?? throw new ArgumentNullException(nameof(ratingRepo));
            _userRepo = userRepo ?? throw new ArgumentNullException(nameof(userRepo));
            _tripRepo = tripRepo ?? throw new ArgumentNullException(nameof(tripRepo));
        }

        public async Task<Rating> CreateRating(
            Guid tripId,
            Guid passengerId,
            int score,
            string comment)
        {
            RatingValidator.ValidateRating(score, comment);

            var trip = await _tripRepo.GetById(tripId)
                       ?? throw new KeyNotFoundException($"Không tìm thấy trip '{tripId}'.");

            RatingValidator.ValidateTripForRating(trip);

            if (await _ratingRepo.ExistsForTrip(tripId))
                throw new InvalidOperationException("Chuyến đi này đã được đánh giá.");

            if (trip.PassengerId != passengerId)
                throw new InvalidOperationException("Bạn không phải hành khách của chuyến đi này.");

            if (!trip.DriverId.HasValue)
                throw new InvalidOperationException("Chuyến đi không có tài xế.");

            var rating = new Rating(
                tripId: tripId,
                driverId: trip.DriverId.Value,
                passengerId: passengerId,
                score: score,
                comment: comment);

            await _ratingRepo.Add(rating);

            var user = await _userRepo.GetById(trip.DriverId.Value);
            if (user is Driver driver)
            {
                trip.MarkAsRated();
                await _tripRepo.Update(trip);
                driver.UpdateRating(score);
                await _userRepo.Update(driver);
            }

            return rating;
        }

        public async Task<Rating?> GetRatingByTrip(Guid tripId)
        {
            return await _ratingRepo.GetByTripId(tripId);
        }

        public async Task<List<Rating>> GetRatingsByDriver(Guid driverId)
        {
            return await _ratingRepo.GetByDriverId(driverId);
        }
    }
}