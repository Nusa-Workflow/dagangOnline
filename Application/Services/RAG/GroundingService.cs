using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using dagangOnline.Application.DTOs;
using dagangOnline.Application.Interfaces;

namespace dagangOnline.Application.Services.RAG;

public class GroundingService : IGroundingService
{
    public Task<GroundingAssessmentDto> AssessGroundingAsync(string generatedResponse, List<RetrievalResultDto> retrievedEvidence, CancellationToken cancellationToken = default)
    {
        var assessment = new GroundingAssessmentDto();

        if (string.IsNullOrWhiteSpace(generatedResponse))
        {
            assessment.State = GroundingState.Ungrounded;
            assessment.ConfidenceScore = 0.0;
            return Task.FromResult(assessment);
        }

        if (retrievedEvidence == null || retrievedEvidence.Count == 0)
        {
            assessment.State = GroundingState.NeedsHumanReview;
            assessment.ConfidenceScore = 0.2;
            assessment.HallucinationFlags.Add("Tidak ada dokumen rujukan yang ditemukan.");
            return Task.FromResult(assessment);
        }

        // Tokenize response into informative words (length > 3)
        var responseWords = Regex.Matches(generatedResponse.ToLowerInvariant(), @"\b[\w'-]{4,}\b")
                                 .Select(m => m.Value)
                                 .ToHashSet();

        if (responseWords.Count == 0)
        {
            assessment.State = GroundingState.Grounded;
            assessment.ConfidenceScore = 0.9;
            return Task.FromResult(assessment);
        }

        // Combine all evidence content
        var combinedEvidence = string.Join(" ", retrievedEvidence.Select(e => e.Content)).ToLowerInvariant();
        var evidenceWords = Regex.Matches(combinedEvidence, @"\b[\w'-]{4,}\b")
                                 .Select(m => m.Value)
                                 .ToHashSet();

        // Calculate overlap
        int supportedWords = responseWords.Count(w => evidenceWords.Contains(w));
        double overlapRatio = (double)supportedWords / responseWords.Count;
        assessment.ConfidenceScore = Math.Round(overlapRatio, 2);

        // Identify matched source titles as citations
        foreach (var ev in retrievedEvidence)
        {
            if (!string.IsNullOrEmpty(ev.DocumentTitle) && !assessment.MatchedCitations.Contains(ev.DocumentTitle))
            {
                assessment.MatchedCitations.Add(ev.DocumentTitle);
            }
        }

        if (overlapRatio >= 0.65)
        {
            assessment.State = GroundingState.Grounded;
        }
        else if (overlapRatio >= 0.40)
        {
            assessment.State = GroundingState.PartiallyGrounded;
            assessment.HallucinationFlags.Add("Sebagian informasi tidak secara eksplisit tertera pada dokumen rujukan.");
        }
        else
        {
            assessment.State = GroundingState.NeedsHumanReview;
            assessment.HallucinationFlags.Add("Tingkat kesesuaian jawaban dengan bukti dokumen di bawah ambang batas aman (Low Grounding).");
        }

        return Task.FromResult(assessment);
    }
}
