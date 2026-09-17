using System;
using System.Collections.Generic;

namespace dagangOnline.Application.DTOs;

public class RetrievalResultDto
{
    public int ChunkId { get; set; }
    public int DocumentId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string DocumentTitle { get; set; } = string.Empty;
    public string SourceUri { get; set; } = string.Empty;
    public double Score { get; set; }
    public string RetrievalType { get; set; } = "Dense"; // Dense, Sparse, Hybrid
    public string? Language { get; set; }
}

public enum GroundingState
{
    Grounded,
    PartiallyGrounded,
    Ungrounded,
    NeedsHumanReview
}

public class GroundingAssessmentDto
{
    public GroundingState State { get; set; }
    public double ConfidenceScore { get; set; }
    public List<string> MatchedCitations { get; set; } = new();
    public List<string> HallucinationFlags { get; set; } = new();
    public bool RequiresEscalation => State == GroundingState.NeedsHumanReview || State == GroundingState.Ungrounded;
}

public class GuardrailResultDto
{
    public bool IsAllowed { get; set; } = true;
    public string? ViolationReason { get; set; }
    public string BlockedCategory { get; set; } = string.Empty;
}

public class FeedbackSubmissionDto
{
    public Guid ConversationId { get; set; }
    public Guid MessageId { get; set; }
    public string FeedbackType { get; set; } = "Like"; // Like or Unlike
    public string? Reason { get; set; } // "Jawaban tidak relevan", "Informasi kurang tepat / kurang lengkap", "Bahasa atau penjelasan kurang sesuai", "Lainnya"
    public string? CustomComment { get; set; }
    public string? Language { get; set; }
}

public class FeedbackSummaryDto
{
    public int TotalLikes { get; set; }
    public int TotalUnlikes { get; set; }
    public double SatisfactionRate => (TotalLikes + TotalUnlikes) > 0 ? (double)TotalLikes / (TotalLikes + TotalUnlikes) * 100 : 0;
    public Dictionary<string, int> TopUnlikeReasons { get; set; } = new();
}

public class RetrievalMetricsDto
{
    public double RecallAtK { get; set; }
    public double PrecisionAtK { get; set; }
    public double HitAtK { get; set; }
    public double MeanReciprocalRank { get; set; }
    public double NormalizedDcg { get; set; }
    public double ContextRelevance { get; set; }
    public double ContextCoverage { get; set; }
}
