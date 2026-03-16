﻿using OOP.Domain.Entities;
using OOP.Domain.Enums;

namespace OOP.Application.Validators
{
    public static class RatingValidator
    {
        public static void ValidateRating(int score, string comment)
        {
            if (score < 1 || score > 5)
                throw new ArgumentException("Điểm đánh giá phải từ 1 đến 5 sao.");

            if (score < 3 && string.IsNullOrWhiteSpace(comment))
                throw new ArgumentException("Vui lòng để lại góp ý khi đánh giá dưới 3 sao.");
            if (comment.Length < 5)
                throw new ArgumentException("Góp ý của bạn quá ngắn. Vui lòng mô tả chi tiết hơn.");
        }

        public static void ValidateTripForRating(Trip trip)
        {
            if (trip == null) throw new ArgumentNullException(nameof(trip));

            if (trip.Status != TripStatus.Completed)
                throw new InvalidOperationException(
                    $"Chỉ có thể đánh giá chuyến đã hoàn thành. Trạng thái hiện tại: {trip.Status}");

            if (trip.DriverId == null || trip.DriverId == Guid.Empty)
                throw new InvalidOperationException("Chuyến đi không có tài xế hợp lệ.");
        }
    }
}