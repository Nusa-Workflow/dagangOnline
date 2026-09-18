namespace dagangOnline.Models;

public enum HeroVisualType
{
    SovereignAi,
    MarketIntelligence,
    UmkmOperations,
    HumanAiSupport
}

public class HeroAction
{
    public string Text { get; set; } = string.Empty;
    public string Href { get; set; } = "#";
    public bool IsExternal { get; set; } = false;
    public string? AriaLabel { get; set; }
}

public class HeroMetadataItem
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Status { get; set; } = "normal"; // "normal", "active", "live", "warning"
}

public class HeroSlide
{
    public string Id { get; set; } = string.Empty;
    public int Index { get; set; }
    public string Eyebrow { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public HeroAction PrimaryAction { get; set; } = new();
    public HeroAction SecondaryAction { get; set; } = new();
    public HeroVisualType VisualType { get; set; }
    public string SystemStatus { get; set; } = "OPERATIONAL";
    public List<HeroMetadataItem> Metadata { get; set; } = new();
}
