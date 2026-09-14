namespace dagangOnline.Domain;

public class MitraProfile : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string BusinessName { get; set; } = string.Empty;
    public string? BusinessType { get; set; }
    public string? Description { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Region { get; set; }
    public bool IsVerified { get; set; }
}
