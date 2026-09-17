using System;

namespace dagangOnline.Mobile.Models;

public class ProductModel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    public string FormattedPrice => $"Rp {Price:N0}";
}
