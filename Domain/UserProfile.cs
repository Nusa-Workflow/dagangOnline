namespace dagangOnline.Domain;

public class UserProfile : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Bio { get; set; }
    public string? Location { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? AvatarUrl { get; set; }
}
