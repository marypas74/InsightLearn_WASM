using System.ComponentModel.DataAnnotations;

namespace InsightLearn.Core.DTOs.Admin;

public class RefundRequestDto
{
    [Required(ErrorMessage = "Refund amount is required")]
    [Range(0.01, 100000, ErrorMessage = "Refund amount must be between 0.01 and 100,000")]
    public decimal RefundAmount { get; set; }

    [Required(ErrorMessage = "Reason is required")]
    [StringLength(500, MinimumLength = 10, ErrorMessage = "Reason must be between 10 and 500 characters")]
    public string Reason { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Notes must be less than 1000 characters")]
    public string? Notes { get; set; }
}

public class RefundResponseDto
{
    public bool Success { get; set; }
    public Guid PaymentId { get; set; }
    public Guid RefundId { get; set; }
    public decimal RefundAmount { get; set; }
    public decimal OriginalAmount { get; set; }
    public DateTime RefundedAt { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? RefundProvider { get; set; } // Stripe, PayPal, etc.
}

public class PaymentStatsDto
{
    public decimal TotalRevenue { get; set; }
    public decimal TotalRefunded { get; set; }
    public int TotalTransactions { get; set; }
    public int SuccessfulTransactions { get; set; }
    public int FailedTransactions { get; set; }
    public int RefundedTransactions { get; set; }
    public Dictionary<string, decimal> RevenueByMethod { get; set; } = new();
    public Dictionary<string, int> TransactionsByStatus { get; set; } = new();
    public List<DailyRevenueDto> DailyRevenue { get; set; } = new();
}

public class DailyRevenueDto
{
    public DateTime Date { get; set; }
    public decimal Revenue { get; set; }
    public int Transactions { get; set; }
}
