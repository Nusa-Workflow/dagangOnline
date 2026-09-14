namespace dagangOnline.Domain;

public class CarouselItem : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string? LinkUrl { get; set; }
    public int SortOrder { get; set; }
    public PublicationStatus Status { get; set; } = PublicationStatus.Draft;
    public bool IsActive { get; set; }
}
