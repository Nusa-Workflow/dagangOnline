using System.ComponentModel.DataAnnotations;
using dagangOnline.Domain;

namespace dagangOnline.Application.DTOs;

public class PortfolioDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Problem { get; set; }
    public string? Solution { get; set; }
    public string? Result { get; set; }
    public PublicationStatus Status { get; set; }
    public bool IsFeatured { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreatePortfolioDto
{
    [Required(ErrorMessage = "Judul proyek portfolio wajib diisi.")]
    [StringLength(200, ErrorMessage = "Maksimal 200 karakter.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ringkasan proyek wajib diisi.")]
    [StringLength(500, ErrorMessage = "Maksimal 500 karakter.")]
    public string Summary { get; set; } = string.Empty;

    public string? Description { get; set; }
    public string? Problem { get; set; }
    public string? Solution { get; set; }
    public string? Result { get; set; }
    public PublicationStatus Status { get; set; } = PublicationStatus.Published;
    public bool IsFeatured { get; set; }
}

public class UpdatePortfolioDto
{
    [Required(ErrorMessage = "Judul proyek portfolio wajib diisi.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ringkasan proyek wajib diisi.")]
    public string Summary { get; set; } = string.Empty;

    public string? Description { get; set; }
    public string? Problem { get; set; }
    public string? Solution { get; set; }
    public string? Result { get; set; }
    public PublicationStatus Status { get; set; }
    public bool IsFeatured { get; set; }
}
