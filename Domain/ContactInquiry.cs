namespace dagangOnline.Domain;

public class ContactInquiry : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Company { get; set; }
    public string Category { get; set; } = "General";
    public bool ConsentAccepted { get; set; }
    public InquiryStatus Status { get; set; } = InquiryStatus.New;
    public string? ResponseSummary { get; set; }
    public DateTime? RepliedAt { get; set; }
    public string? Source { get; set; }
}
