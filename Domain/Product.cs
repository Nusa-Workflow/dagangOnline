namespace dagangOnline.Domain;

public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? Currency { get; set; } = "IDR";
    public ProductCategory Category { get; set; } = ProductCategory.General;
    public PublicationStatus Status { get; set; } = PublicationStatus.Draft;
    public bool IsFeatured { get; set; }
    public string? ImageUrl { get; set; }
    public Guid? ServiceId { get; set; }
    public Service? Service { get; set; }
}

public enum ProductCategory
{
    General = 0,
    SaaS = 1,
    ServicePackage = 2,
    DigitalProduct = 3
}
