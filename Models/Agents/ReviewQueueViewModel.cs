using dagangOnline.Domain;
using dagangOnline.Domain.Agents;

namespace dagangOnline.Models.Agents;

public class ReviewQueueItemDto
{
    public Guid TaskId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSummary { get; set; } = string.Empty;
    public string ProductDescription { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public ProductCategory Category { get; set; }
    public string? OwnerName { get; set; }
    public string? OwnerId { get; set; }
    public DateTime SubmittedAt { get; set; }
    public ReviewTaskStatus TaskStatus { get; set; }
}

public class ReviewDecisionInput
{
    public Guid TaskId { get; set; }
    public string Action { get; set; } = "Approve"; // "Approve" or "Reject"
    public string? Reason { get; set; }
    public string? Notes { get; set; }
}
