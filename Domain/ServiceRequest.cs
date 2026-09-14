namespace dagangOnline.Domain;

public class ServiceRequest : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ClientName { get; set; }
    public string? ClientEmail { get; set; }
    public string? ClientPhone { get; set; }
    public string? BudgetRange { get; set; }
    public string Status { get; set; } = "New";
    public Guid? ServiceId { get; set; }
    public Service? Service { get; set; }
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
}
