using System.ComponentModel.DataAnnotations;
using dagangOnline.Domain;

namespace dagangOnline.Application.DTOs;

public class ContactInquiryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Category { get; set; }
    public InquiryStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UpdateInquiryStatusDto
{
    [Required]
    public InquiryStatus Status { get; set; }
}
