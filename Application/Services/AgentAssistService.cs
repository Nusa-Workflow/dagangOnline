using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using dagangOnline.Application.DTOs;
using dagangOnline.Application.Interfaces;
using dagangOnline.Domain.Chat;
using dagangOnline.Services;
using global::dagangOnline.Application.Services.Economic;
using global::dagangOnline.Domain.Economic;

namespace dagangOnline.Application.Services;

public class AgentAssistService
{
    private readonly IAiChatService _aiChatService;
    private readonly GraphContextBuilder _graphContextBuilder;
    private readonly ILanguageService _languageService;
    private readonly IIndonesiaContextLayer _indonesiaContext;
    private readonly IEmbeddingService _embeddingService;
    private readonly IRetrievalService _retrievalService;
    private readonly IGroundingService _groundingService;
    private readonly IGuardrailService _guardrailService;
    private readonly EconomicMultiAgentSystem _multiAgentSystem;

    public AgentAssistService(
        IAiChatService aiChatService,
        GraphContextBuilder graphContextBuilder,
        ILanguageService languageService,
        IIndonesiaContextLayer indonesiaContext,
        IEmbeddingService embeddingService,
        IRetrievalService retrievalService,
        IGroundingService groundingService,
        IGuardrailService guardrailService,
        EconomicMultiAgentSystem multiAgentSystem)
    {
        _aiChatService = aiChatService;
        _graphContextBuilder = graphContextBuilder;
        _languageService = languageService;
        _indonesiaContext = indonesiaContext;
        _embeddingService = embeddingService;
        _retrievalService = retrievalService;
        _groundingService = groundingService;
        _guardrailService = guardrailService;
        _multiAgentSystem = multiAgentSystem;
    }

    public async Task<AiSuggestionDto> ProcessCustomerMessageAsync(Conversation session, string message, CancellationToken cancellationToken = default)
    {
        // 1. Language Detection & Normalization
        var detectedLang = _languageService.DetectLanguage(message);
        var normalizedQuery = _languageService.NormalizeQuery(message, detectedLang);

        // 2. Input Guardrails
        var inputGuardrail = _guardrailService.ValidateInput(message);
        if (!inputGuardrail.IsAllowed)
        {
            session.Status = ConversationStatus.Escalated;
            return new AiSuggestionDto
            {
                SuggestedResponse = "Maaf, permintaan Anda tidak dapat diproses karena melanggar kebijakan keamanan sistem.",
                Intent = "security_violation",
                Priority = "High",
                NeedsHuman = true,
                Reason = inputGuardrail.ViolationReason ?? "Input Guardrail Triggered",
                Language = detectedLang,
                GroundingState = "Ungrounded",
                Confidence = 0.0
            };
        }

        // 3. Indonesia Context Layer
        var localContext = await _indonesiaContext.EnrichContextWithLocalKnowledgeAsync(message, detectedLang, cancellationToken);

        // 4. Hybrid Retrieval (pgvector dense + sparse FTS)
        var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(normalizedQuery, cancellationToken);
        var retrievedChunks = await _retrievalService.RetrieveHybridAsync(normalizedQuery, queryEmbedding, topK: 4, cancellationToken);

        // 5. Retrieval Guardrails (Role/Tenant check)
        var retrievalGuardrail = _guardrailService.ValidateRetrievalAccess(session.CustomerId, "User", retrievedChunks);
        if (!retrievalGuardrail.IsAllowed)
        {
            retrievedChunks.Clear();
        }

        // 6. Knowledge Graph Representation
        var graphContext = await _graphContextBuilder.BuildGraphContextAsync(session, cancellationToken);

        // 7. Compose Grounded Context conditioning
        var contextBuilder = new StringBuilder();
        contextBuilder.AppendLine("=== RETRIEVED KNOWLEDGE EVIDENCE ===");
        if (retrievedChunks.Any())
        {
            foreach (var chunk in retrievedChunks)
            {
                contextBuilder.AppendLine($"[Dokumen: {chunk.DocumentTitle}] (Skor: {chunk.Score:F2})");
                contextBuilder.AppendLine(chunk.Content);
                contextBuilder.AppendLine();
            }
        }
        else
        {
            contextBuilder.AppendLine("Tidak ada dokumen spesifik yang terindeks.");
        }

        contextBuilder.AppendLine(localContext);
        contextBuilder.AppendLine("=== GRAPH CONTEXT ===");
        contextBuilder.AppendLine(graphContext);

        // 7b. Autonomous Economic Intelligence Agent Workflow (if applicable)
        var lowerMsg = message.ToLowerInvariant();
        if (lowerMsg.Contains("ekonomi") || lowerMsg.Contains("inflasi") || lowerMsg.Contains("harga") || 
            lowerMsg.Contains("prediksi") || lowerMsg.Contains("pasar") || lowerMsg.Contains("komoditas") ||
            lowerMsg.Contains("cpo") || lowerMsg.Contains("minyak") || lowerMsg.Contains("prospek"))
        {
            try
            {
                var econReport = await _multiAgentSystem.ExecuteAutonomousWorkflowAsync(new Domain.Economic.MultiAgentTaskRequestDto
                {
                    Query = message,
                    HorizonMonths = 3
                }, cancellationToken);

                contextBuilder.AppendLine();
                contextBuilder.AppendLine("=== ECONOMIC INTELLIGENCE & FORECASTING INSIGHTS ===");
                contextBuilder.AppendLine($"[Target]: {econReport.Forecast.TargetEntityName} (Baseline: {econReport.Forecast.BaselineValue} {econReport.Forecast.Unit})");
                contextBuilder.AppendLine($"[Proyeksi Tren]: {econReport.Forecast.OverallTrend} ({econReport.Forecast.ExpectedPercentageChange:+0.0;-0.0}% dalam 3 bulan)");
                contextBuilder.AppendLine($"[Ringkasan Eksekutif]: {econReport.ExecutiveSummary}");
                contextBuilder.AppendLine("[Rekomendasi Strategis]:");
                foreach (var rec in econReport.RecommendedActions.Take(3))
                {
                    contextBuilder.AppendLine($"- {rec}");
                }
            }
            catch
            {
                // Fallback gracefully without breaking chat pipeline
            }
        }

        // 8. Causal Language Model Generation (Groq Qwen CLM)
        var aiSuggestion = await _aiChatService.GetChatResponseAsync(message, contextBuilder.ToString(), cancellationToken);
        aiSuggestion.Language = detectedLang;
        aiSuggestion.EvidenceCount = retrievedChunks.Count;

        // 9. Grounding & Hallucination Check
        var grounding = await _groundingService.AssessGroundingAsync(aiSuggestion.SuggestedResponse, retrievedChunks, cancellationToken);
        aiSuggestion.GroundingState = grounding.State.ToString();
        aiSuggestion.Confidence = grounding.ConfidenceScore;
        aiSuggestion.Citations = grounding.MatchedCitations;

        // 10. Output Guardrails
        var outputGuardrail = _guardrailService.ValidateOutput(aiSuggestion.SuggestedResponse, retrievedChunks);
        if (!outputGuardrail.IsAllowed)
        {
            aiSuggestion.SuggestedResponse = "Respons dibatasi oleh kebijakan keamanan output.";
            aiSuggestion.NeedsHuman = true;
            aiSuggestion.Reason = outputGuardrail.ViolationReason ?? "Output Guardrail Triggered";
        }

        // 11. Escalation Policy
        if (grounding.RequiresEscalation || aiSuggestion.NeedsHuman)
        {
            session.Status = ConversationStatus.Escalated;
            aiSuggestion.NeedsHuman = true;
            if (string.IsNullOrEmpty(aiSuggestion.Reason))
            {
                aiSuggestion.Reason = "Tingkat pembuktian rendah (Low Grounding) atau memerlukan penanganan manusia.";
            }
        }

        // Update Session Metadata
        if (Enum.TryParse<ConversationPriority>(aiSuggestion.Priority, true, out var parsedPriority))
        {
            session.Priority = parsedPriority;
        }

        if (Enum.TryParse<IntentType>(aiSuggestion.Intent, true, out var parsedIntent))
        {
            session.Intent = parsedIntent;
        }
        else
        {
            session.Intent = IntentType.unknown;
        }

        return aiSuggestion;
    }
}
