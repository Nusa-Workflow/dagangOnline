using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using dagangOnline.Application.DTOs;
using dagangOnline.Application.Interfaces;
using dagangOnline.Domain.Chat;

namespace dagangOnline.Application.Services.Voice;

public class CollaborativeAgentOrchestrator : ICollaborativeAgentOrchestrator
{
    private readonly INemotronVoiceAgentService _nemotronService;
    private readonly AgentAssistService? _agentAssistService;
    private readonly IAiChatService _aiChatService;
    private readonly ILogger<CollaborativeAgentOrchestrator> _logger;

    public CollaborativeAgentOrchestrator(
        INemotronVoiceAgentService nemotronService,
        AgentAssistService? agentAssistService,
        IAiChatService aiChatService,
        ILogger<CollaborativeAgentOrchestrator> logger)
    {
        _nemotronService = nemotronService;
        _agentAssistService = agentAssistService;
        _aiChatService = aiChatService;
        _logger = logger;
    }

    public Task<CollaborativeChatResponseDto> ProcessCollaborativeTurnAsync(
        CollaborativeChatRequestDto request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteCollaborativeChatAsync(request, null, cancellationToken);
    }

    public async Task<CollaborativeChatResponseDto> ExecuteCollaborativeChatAsync(
        CollaborativeChatRequestDto request,
        Conversation? session = null,
        CancellationToken cancellationToken = default)
    {
        var result = new CollaborativeChatResponseDto
        {
            Success = true,
            SessionId = request.SessionId
        };

        // 1. Voice Ingestion via Nemotron VoiceChat
        string effectiveText = request.TextMessage ?? "";
        if (!string.IsNullOrWhiteSpace(request.AudioBase64))
        {
            var transcription = await _nemotronService.TranscribeAudioBase64Async(
                request.AudioBase64,
                request.AudioMimeType,
                request.Language,
                cancellationToken);

            if (transcription.Success && !string.IsNullOrWhiteSpace(transcription.Transcript))
            {
                effectiveText = transcription.Transcript;
                result.UserTranscript = transcription.Transcript;
                result.AcousticTelemetry = transcription.AcousticProfile;
            }
        }
        else
        {
            result.UserTranscript = effectiveText;
            // Analyze acoustic metrics from text cadence
            result.AcousticTelemetry = await _nemotronService.AnalyzeAcousticStreamAsync(
                null,
                effectiveText,
                1.5,
                cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(effectiveText))
        {
            effectiveText = "Halo dagangOnline";
        }

        // 2. Cognitive Reasoning via Qwen & RAG Pipeline
        session ??= new Conversation
        {
            Id = (request.SessionId.HasValue && request.SessionId.Value != Guid.Empty) ? request.SessionId.Value : Guid.NewGuid(),
            CustomerId = "guest-user",
            Status = ConversationStatus.Open
        };

        AiSuggestionDto qwenSuggestion;
        try
        {
            if (_agentAssistService != null)
            {
                qwenSuggestion = await _agentAssistService.ProcessCustomerMessageAsync(session, effectiveText, cancellationToken);
            }
            else
            {
                qwenSuggestion = await _aiChatService.GetChatResponseAsync(effectiveText, "", cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AgentAssistService encountered error, falling back to direct Qwen chat.");
            qwenSuggestion = await _aiChatService.GetChatResponseAsync(effectiveText, "", cancellationToken);
        }

        result.QwenReasoningResponse = qwenSuggestion.SuggestedResponse;
        result.Intent = qwenSuggestion.Intent ?? "general";
        result.Priority = qwenSuggestion.Priority ?? "Normal";
        result.NeedsHuman = qwenSuggestion.NeedsHuman;
        result.GroundingState = qwenSuggestion.GroundingState ?? "Grounded";
        result.GroundingConfidence = qwenSuggestion.Confidence > 0 ? qwenSuggestion.Confidence : 0.95;
        result.MatchedCitations = qwenSuggestion.Citations ?? new List<string>();

        // 3. Spoken Dialogue Polishing & Synthesis via Nemotron VoiceChat
        var synthesisRequest = new VoiceSynthesisRequestDto
        {
            Text = qwenSuggestion.SuggestedResponse,
            TargetLanguage = qwenSuggestion.Language ?? request.Language,
            VoicePersona = result.AcousticTelemetry.EmotionTone == "Urgent" 
                ? "Nemotron-CS-Professional" 
                : "Nemotron-Nusantara-Warm",
            ReturnAudioStream = request.EnableVoiceSynthesis
        };

        var voiceResult = await _nemotronService.SynthesizeSpokenVoiceAsync(synthesisRequest, cancellationToken);
        if (voiceResult.Success)
        {
            result.NemotronSpokenScript = voiceResult.SpokenScript;
            if (request.EnableVoiceSynthesis)
            {
                result.SynthesizedAudioBase64 = voiceResult.AudioBase64;
                result.AudioFormat = voiceResult.AudioFormat;
            }
        }
        else
        {
            result.NemotronSpokenScript = qwenSuggestion.SuggestedResponse;
        }

        return result;
    }

    public async Task<CollaborativeChatResponseDto> GenerateDualAgentDraftsAsync(
        string customerMessage,
        Conversation session,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteCollaborativeChatAsync(new CollaborativeChatRequestDto
        {
            SessionId = session.Id,
            TextMessage = customerMessage,
            EnableVoiceSynthesis = true,
            IsAgentView = true
        }, session, cancellationToken);
    }
}
