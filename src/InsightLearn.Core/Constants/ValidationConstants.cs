namespace InsightLearn.Core.Constants;

/// <summary>
/// Validation constants for business rules
/// Centralized location for magic numbers
/// </summary>
public static class ValidationConstants
{
    /// <summary>
    /// Payment and pricing limits
    /// </summary>
    public static class Payment
    {
        public const decimal MinAmount = 0.01m;
        public const decimal MaxAmount = 50000.00m;
        public const int MaxCouponUsageLimit = 100000;
        public const int MaxDiscountPercentage = 100;
    }

    /// <summary>
    /// User input limits
    /// </summary>
    public static class User
    {
        public const int MaxNameLength = 100;
        public const int MaxEmailLength = 255;
        public const int MaxPhoneLength = 20;
        public const int MaxBioLength = 2000;
    }

    /// <summary>
    /// Content limits
    /// </summary>
    public static class Content
    {
        public const int MaxCourseDescriptionLength = 5000;
        public const int MaxReviewLength = 2000;
        public const int MaxDiscussionLength = 10000;
    }
}