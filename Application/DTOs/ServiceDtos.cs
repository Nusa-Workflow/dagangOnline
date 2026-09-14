using System.ComponentModel.DataAnnotations;
using dagangOnline.Domain;

namespace dagangOnline.Application.DTOs;

public class ServiceDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public PublicationStatus Status { get; set; }
    public bool IsFeatured { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateServiceDto
{
    [Required(ErrorMessage = "Nama layanan wajib diisi.")]
    [StringLength(200, ErrorMessage = "Maksimal 200 karakter.")]
    public string Name { get; set; } = string.Empty;

    public string? Slug { get; set; }

    [Required(ErrorMessage = "Ringkasan layanan wajib diisi.")]
    [StringLength(500, ErrorMessage = "Maksimal 500 karakter.")]
    public string Summary { get; set; } = string.Empty;

    public string? Description { get; set; }
    public PublicationStatus Status { get; set; } = PublicationStatus.Published;
    public bool IsFeatured { get; set; }
}

public class UpdateServiceDto
{
    [Required(ErrorMessage = "Nama layanan wajib diisi.")]
    [StringLength(200, ErrorMessage = "Maksimal 200 karakter.")]
    public string Name { get; set; } = string.Empty;

    public string? Slug { get; set; }

    [Required(ErrorMessage = "Ringkasan layanan wajib diisi.")]
    public string Summary { get; set; } = string.Empty;

    public string? Description { get; set; }
    public PublicationStatus Status { get; set; }
    public bool IsFeatured { get; set; }
}
