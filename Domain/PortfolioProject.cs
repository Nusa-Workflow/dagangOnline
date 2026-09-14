namespace dagangOnline.Domain;

public class PortfolioProject : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Problem { get; set; } = string.Empty;
    public string Solution { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public string? ClientName { get; set; }
    public string? Category { get; set; }
    public string? Industry { get; set; }
    public PublicationStatus Status { get; set; } = PublicationStatus.Draft;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsFeatured { get; set; }
    public string? MediaUrl { get; set; }
    public string? ArchitectureSummary { get; set; }
    public ICollection<PortfolioTechnology> Technologies { get; set; } = new List<PortfolioTechnology>();
}
