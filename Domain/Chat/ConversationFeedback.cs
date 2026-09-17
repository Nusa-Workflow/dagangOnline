using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace dagangOnline.Domain.Chat;

public class ConversationFeedback
{
    [Key]
    public int Id { get; set; }

    public Guid ConversationId { get; set; }
    
    [ForeignKey("ConversationId")]
    public virtual Conversation? Conversation { get; set; }

    public Guid MessageId { get; set; }

    [ForeignKey("MessageId")]
    public virtual ConversationMessage? Message { get; set; }

    public string? UserId { get; set; } // Can be null if anonymous

    [Required]
    [MaxLength(20)]
    public string FeedbackType { get; set; } = "Like"; // "Like" or "Unlike"

    [MaxLength(100)]
    public string? ReasonCode { get; set; } // "NotRelevant", "Inaccurate", "BadLanguage", "Other"

    [MaxLength(1000)]
    public string? CustomReason { get; set; }

    [MaxLength(10)]
    public string Language { get; set; } = "id-ID";

    public double RetrievalScore { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
