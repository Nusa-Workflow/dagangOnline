using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace dagangOnline.Domain.Chat;

public class ConversationMessage : BaseEntity
{
    public Guid ConversationId { get; set; }

    [Required]
    public string Content { get; set; } = string.Empty;

    public SenderType SenderType { get; set; }

    public string? Metadata { get; set; }
    public float? Confidence { get; set; }

    [ForeignKey(nameof(ConversationId))]
    public Conversation? Conversation { get; set; }
}
