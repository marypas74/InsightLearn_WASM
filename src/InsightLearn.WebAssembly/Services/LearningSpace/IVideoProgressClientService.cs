using InsightLearn.WebAssembly.Models;

namespace InsightLearn.WebAssembly.Services.LearningSpace;

public interface IVideoProgressClientService
{
    Task<ApiResponse<VideoProgressResponseDto>> TrackProgressAsync(TrackVideoProgressDto dto);
    Task<ApiResponse<LastPositionDto>> GetLastPositionAsync(Guid lessonId);
    Task<ApiResponse<LessonProgressDto>> GetLessonProgressAsync(Guid lessonId);
    Task<ApiResponse<List<LessonProgressDto>>> GetCourseProgressAsync(Guid courseId);
}

public class TrackVideoProgressDto
{
    public Guid LessonId { get; set; }
    public int CurrentTimestampSeconds { get; set; }
    public int TotalDurationSeconds { get; set; }
    public string? SessionId { get; set; }
    public decimal? PlaybackSpeed { get; set; }
    public bool? TabActive { get; set; }
}

public class VideoProgressResponseDto
{
    public Guid EngagementId { get; set; }
    public int CurrentTimestampSeconds { get; set; }
    public double CompletionPercentage { get; set; }
    public bool IsCompleted { get; set; }
    public decimal ValidationScore { get; set; }
    public bool CountsForPayout { get; set; }
}

public class LastPositionDto
{
    public Guid LessonId { get; set; }
    public int LastPosition { get; set; }
}

public class LessonProgressDto
{
    public Guid Id { get; set; }
    public Guid LessonId { get; set; }
    public string LessonTitle { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public int WatchedMinutes { get; set; }
    public int LessonDurationMinutes { get; set; }
    public double ProgressPercentage { get; set; }
    public DateTime? CompletedAt { get; set; }
}
