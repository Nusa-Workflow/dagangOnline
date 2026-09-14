namespace dagangOnline.Domain;

public class Announcement : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public PublicationStatus Status { get; set; } = PublicationStatus.Draft;
    public DateTime? PublishedAt { get; set; }
    public bool IsPinned { get; set; }
}
