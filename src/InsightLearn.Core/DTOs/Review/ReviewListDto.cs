namespace InsightLearn.Core.DTOs.Review;

public class ReviewListDto
{
    public List<ReviewDto> Reviews { get; set; } = new();
    public int TotalCount { get; set; }
    public double AverageRating { get; set; }
    public ReviewStatisticsDto? Statistics { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
