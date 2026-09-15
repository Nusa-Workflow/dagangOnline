namespace dagangOnline.Domain.Agents;

public class ReviewTask : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public string? ReviewerId { get; set; }
    public string? ReviewerName { get; set; }
    public ReviewTaskStatus Status { get; set; } = ReviewTaskStatus.Pending;
    public DateTime? AssignedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public enum ReviewTaskStatus
{
    Pending = 0,
    InReview = 1,
    Approved = 2,
    Rejected = 3
}
