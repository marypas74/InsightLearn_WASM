namespace InsightLearn.WebAssembly.Models.Admin;

public class UserListItem
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool IsVerified { get; set; }
    public bool IsInstructor { get; set; }
    public DateTime DateJoined { get; set; }
    public DateTime? LastLoginDate { get; set; }
    public decimal WalletBalance { get; set; }
    public bool LockoutEnabled { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public List<string> Roles { get; set; } = new();
    public int TotalCourses { get; set; }
    public int TotalEnrollments { get; set; }
}

public class UserDetail : UserListItem
{
    public string? Bio { get; set; }
    public string? ProfileImageUrl { get; set; }
    public string UserType { get; set; } = "Student";
    public bool RegistrationCompleted { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
}

public class UserUpdateRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsInstructor { get; set; }
    public bool IsVerified { get; set; }
    public decimal WalletBalance { get; set; }
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}
