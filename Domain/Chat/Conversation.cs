using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using dagangOnline.Models;

namespace dagangOnline.Domain.Chat;

public class Conversation : BaseEntity
{
    [Required]
    [StringLength(450)]
    public string CustomerId { get; set; } = string.Empty;

    public ConversationStatus Status { get; set; } = ConversationStatus.Open;

    public ConversationPriority Priority { get; set; } = ConversationPriority.Normal;

    public IntentType Intent { get; set; } = IntentType.unknown;

    public DateTime? ClosedAt { get; set; }

    [StringLength(450)]
    public string? AssignedAgentId { get; set; }

    [ForeignKey(nameof(CustomerId))]
    public ApplicationUser? Customer { get; set; }

    [ForeignKey(nameof(AssignedAgentId))]
    public ApplicationUser? AssignedAgent { get; set; }

    public ICollection<ConversationMessage> Messages { get; set; } = new List<ConversationMessage>();
}
