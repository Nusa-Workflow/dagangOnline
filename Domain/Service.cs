namespace dagangOnline.Domain;

public class Service : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public PublicationStatus Status { get; set; } = PublicationStatus.Draft;
    public bool IsFeatured { get; set; }
    public decimal? StartingPrice { get; set; }
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
