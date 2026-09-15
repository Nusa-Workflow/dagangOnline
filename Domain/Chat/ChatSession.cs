using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using dagangOnline.Models;

namespace dagangOnline.Domain.Chat;

public class ChatSession : BaseEntity
{
    [Required]
    [StringLength(450)]
    public string UserId { get; set; } = string.Empty;

    public ChatSessionStatus Status { get; set; } = ChatSessionStatus.ActiveWithBot;

    [StringLength(450)]
    public string? AssignedAgentId { get; set; }

    [ForeignKey(nameof(UserId))]
    public ApplicationUser? User { get; set; }

    [ForeignKey(nameof(AssignedAgentId))]
    public ApplicationUser? AssignedAgent { get; set; }

    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}
