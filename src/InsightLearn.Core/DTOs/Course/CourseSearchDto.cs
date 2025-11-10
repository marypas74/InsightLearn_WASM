namespace InsightLearn.Core.DTOs.Course;

/// <summary>
/// Query parameters for course search
/// </summary>
public class CourseSearchDto
{
    public string? Query { get; set; }
    public Guid? CategoryId { get; set; }
    public string? Level { get; set; } // Beginner, Intermediate, Advanced
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public double? MinRating { get; set; }
    public string? Language { get; set; }
    public bool? IsFree { get; set; }
    public bool? HasCertificate { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string SortBy { get; set; } = "Relevance"; // Relevance, Newest, Rating, Price, Popular
}
