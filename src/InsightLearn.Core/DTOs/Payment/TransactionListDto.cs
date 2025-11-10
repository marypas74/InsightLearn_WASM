namespace InsightLearn.Core.DTOs.Payment;

public class TransactionListDto
{
    public List<TransactionDto> Transactions { get; set; } = new();
    public int TotalCount { get; set; }
    public decimal TotalRevenue { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
