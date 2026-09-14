using System.ComponentModel.DataAnnotations;
using dagangOnline.Domain;

namespace dagangOnline.Application.DTOs;

public class ProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "IDR";
    public ProductCategory Category { get; set; }
    public PublicationStatus Status { get; set; }
    public bool IsFeatured { get; set; }
    public Guid? ServiceId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateProductDto
{
    [Required(ErrorMessage = "Nama produk wajib diisi.")]
    [StringLength(200, ErrorMessage = "Maksimal 200 karakter.")]
    public string Name { get; set; } = string.Empty;

    public string? Slug { get; set; }

    [Required(ErrorMessage = "Ringkasan produk wajib diisi.")]
    public string Summary { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Range(0, 10000000000, ErrorMessage = "Harga harus bernilai positif.")]
    public decimal Price { get; set; }

    public string Currency { get; set; } = "IDR";
    public ProductCategory Category { get; set; } = ProductCategory.General;
    public PublicationStatus Status { get; set; } = PublicationStatus.Published;
    public bool IsFeatured { get; set; }
    public Guid? ServiceId { get; set; }
}

public class UpdateProductDto
{
    [Required(ErrorMessage = "Nama produk wajib diisi.")]
    public string Name { get; set; } = string.Empty;

    public string? Slug { get; set; }

    [Required(ErrorMessage = "Ringkasan produk wajib diisi.")]
    public string Summary { get; set; } = string.Empty;

    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "IDR";
    public ProductCategory Category { get; set; }
    public PublicationStatus Status { get; set; }
    public bool IsFeatured { get; set; }
    public Guid? ServiceId { get; set; }
}
