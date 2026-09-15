using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace dagangOnline.Domain.Chat;

public class ChatMessage : BaseEntity
{
    public Guid ChatSessionId { get; set; }

    [Required]
    public string Content { get; set; } = string.Empty;

    public ChatMessageSenderRole SenderRole { get; set; }

    [ForeignKey(nameof(ChatSessionId))]
    public ChatSession? Session { get; set; }
}
