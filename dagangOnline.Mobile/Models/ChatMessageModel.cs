using System;
using System.Collections.Generic;

namespace dagangOnline.Mobile.Models;

public class ChatMessageModel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Content { get; set; } = string.Empty;
    public bool IsUser { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string TimestampFormatted => Timestamp.ToLocalTime().ToString("HH:mm");

    // RAG and Grounding metadata
    public List<string> Citations { get; set; } = new();
    public double Confidence { get; set; } = 1.0;
    public string GroundingState { get; set; } = "Grounded";
    public bool IsEscalated { get; set; }
    public int EvidenceCount { get; set; }

    public string EvidenceSummary => EvidenceCount > 0
        ? $"Berdasarkan {EvidenceCount} dokumen rujukan terverifikasi"
        : "Pengetahuan Umum Platform";

    // Feedback state
    public string? FeedbackGiven { get; set; } // "Like", "Unlike", or null
    public string? UnlikeReason { get; set; }
    public bool CanFeedback => !IsUser && FeedbackGiven == null;
}
