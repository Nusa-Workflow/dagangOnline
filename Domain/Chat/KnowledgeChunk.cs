using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace dagangOnline.Domain.Chat;

public class KnowledgeChunk
{
    [Key]
    public int Id { get; set; }

    public int KnowledgeDocumentId { get; set; }
    
    [ForeignKey("KnowledgeDocumentId")]
    public virtual KnowledgeDocument? Document { get; set; }

    [Required]
    public string Content { get; set; } = string.Empty;

    public string? EmbeddingJson { get; set; }

    [NotMapped]
    public float[]? Embedding
    {
        get => string.IsNullOrWhiteSpace(EmbeddingJson) ? null : JsonSerializer.Deserialize<float[]>(EmbeddingJson);
        set => EmbeddingJson = value == null ? null : JsonSerializer.Serialize(value);
    }

    public int TokenCount { get; set; }
    
    public int ChunkIndex { get; set; }

    [MaxLength(100)]
    public string SourceHeading { get; set; } = string.Empty;
}
