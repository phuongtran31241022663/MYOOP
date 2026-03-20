using System;

namespace OOP.Domain.Validators
{
    public static class RatingValidator
    {
        public static void ValidateRating(int score)
        {
            if (score < 1 || score > 5)
                throw new ArgumentException("Điểm đánh giá phải từ 1 đến 5.");
        }
    }
}
