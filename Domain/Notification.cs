namespace dagangOnline.Domain;

public class Notification : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "Info";
    public bool IsRead { get; set; }
    public string? RecipientUserId { get; set; }
    public string? RecipientEmail { get; set; }
    public string? ActionUrl { get; set; }
}
