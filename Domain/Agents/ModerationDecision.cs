namespace dagangOnline.Domain.Agents;

public class ModerationDecision : BaseEntity
{
    public Guid ReviewTaskId { get; set; }
    public ReviewTask? ReviewTask { get; set; }
    public Guid ProductId { get; set; }
    public string ReviewerId { get; set; } = string.Empty;
    public string ReviewerName { get; set; } = string.Empty;
    public string Decision { get; set; } = string.Empty; // "Approved" or "Rejected"
    public string? Reason { get; set; }
    public string? Notes { get; set; }
}
