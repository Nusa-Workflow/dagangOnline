using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace dagangOnline.Domain.Chat;

public class KnowledgeDocument
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    [MaxLength(10)]
    public string Language { get; set; } = "id-ID";

    [MaxLength(50)]
    public string AccessLevel { get; set; } = "Public"; // Public, Mitra, Admin, Agent

    [MaxLength(100)]
    public string Region { get; set; } = "ID";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public int Version { get; set; } = 1;

    // Navigation property
    public virtual ICollection<KnowledgeChunk> Chunks { get; set; } = new List<KnowledgeChunk>();
}
