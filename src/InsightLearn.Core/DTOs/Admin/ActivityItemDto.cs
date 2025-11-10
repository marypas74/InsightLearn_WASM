using System;

namespace InsightLearn.Core.DTOs.Admin
{
    public class ActivityItemDto
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public ActivityType Type { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? EntityId { get; set; }
        public string? EntityType { get; set; }
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string? UserAvatar { get; set; }
        public DateTime Timestamp { get; set; }
        public ActivitySeverity Severity { get; set; } = ActivitySeverity.Info;
        public Dictionary<string, object>? Metadata { get; set; }

        // Computed properties
        public string TimeAgo => GetTimeAgo(Timestamp);
        public string Icon => GetIconForType(Type);
        public string IconColor => GetColorForSeverity(Severity);

        private static string GetTimeAgo(DateTime timestamp)
        {
            var span = DateTime.UtcNow - timestamp;

            if (span.TotalSeconds < 60)
                return $"{(int)span.TotalSeconds}s ago";
            if (span.TotalMinutes < 60)
                return $"{(int)span.TotalMinutes}m ago";
            if (span.TotalHours < 24)
                return $"{(int)span.TotalHours}h ago";
            if (span.TotalDays < 30)
                return $"{(int)span.TotalDays}d ago";
            if (span.TotalDays < 365)
                return $"{(int)(span.TotalDays / 30)}mo ago";

            return $"{(int)(span.TotalDays / 365)}y ago";
        }

        private static string GetIconForType(ActivityType type)
        {
            return type switch
            {
                ActivityType.UserRegistered => "user-plus",
                ActivityType.UserLoggedIn => "sign-in-alt",
                ActivityType.UserLoggedOut => "sign-out-alt",
                ActivityType.UserUpdated => "user-edit",
                ActivityType.UserDeleted => "user-times",
                ActivityType.UserRoleChanged => "user-shield",

                ActivityType.CourseCreated => "graduation-cap",
                ActivityType.CoursePublished => "rocket",
                ActivityType.CourseUpdated => "edit",
                ActivityType.CourseDeleted => "trash",
                ActivityType.CourseArchived => "archive",

                ActivityType.EnrollmentCreated => "user-graduate",
                ActivityType.EnrollmentCompleted => "award",
                ActivityType.EnrollmentDropped => "user-slash",

                ActivityType.PaymentReceived => "credit-card",
                ActivityType.PaymentFailed => "exclamation-triangle",
                ActivityType.RefundProcessed => "undo",

                ActivityType.VideoUploaded => "video",
                ActivityType.VideoDeleted => "video-slash",

                ActivityType.SystemError => "exclamation-circle",
                ActivityType.SystemWarning => "exclamation-triangle",
                ActivityType.SystemInfo => "info-circle",

                _ => "circle"
            };
        }

        private static string GetColorForSeverity(ActivitySeverity severity)
        {
            return severity switch
            {
                ActivitySeverity.Success => "success",
                ActivitySeverity.Info => "info",
                ActivitySeverity.Warning => "warning",
                ActivitySeverity.Error => "danger",
                _ => "secondary"
            };
        }
    }

    public enum ActivityType
    {
        // User activities
        UserRegistered,
        UserLoggedIn,
        UserLoggedOut,
        UserUpdated,
        UserDeleted,
        UserRoleChanged,

        // Course activities
        CourseCreated,
        CoursePublished,
        CourseUpdated,
        CourseDeleted,
        CourseArchived,

        // Enrollment activities
        EnrollmentCreated,
        EnrollmentCompleted,
        EnrollmentDropped,

        // Payment activities
        PaymentReceived,
        PaymentFailed,
        RefundProcessed,

        // Video activities
        VideoUploaded,
        VideoDeleted,

        // System activities
        SystemError,
        SystemWarning,
        SystemInfo
    }

    public enum ActivitySeverity
    {
        Success,
        Info,
        Warning,
        Error
    }

    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageSize { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
        public bool HasPrevious => CurrentPage > 1;
        public bool HasNext => CurrentPage < TotalPages;
    }
}