namespace dagangOnline.Domain;

public class ContentPage : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public PublicationStatus Status { get; set; } = PublicationStatus.Draft;
    public bool ShowInNavigation { get; set; }
}
