namespace dagangOnline.Domain;

public class PortfolioTechnology : BaseEntity
{
    public Guid PortfolioProjectId { get; set; }
    public PortfolioProject PortfolioProject { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
}
