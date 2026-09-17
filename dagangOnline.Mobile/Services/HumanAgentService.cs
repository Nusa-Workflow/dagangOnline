using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using dagangOnline.Mobile.Models;

namespace dagangOnline.Mobile.Services;

public interface IHumanAgentService
{
    Task<(ChatMessageModel? Message, string? Error)> SendMessageAsync(Guid sessionId, string messageText, CancellationToken cancellationToken = default);
    Task<bool> SubmitFeedbackAsync(Guid sessionId, Guid messageId, string feedbackType, string? reason, string? comment, CancellationToken cancellationToken = default);
}

public class HumanAgentService : IHumanAgentService
{
    private readonly HttpClient _httpClient;

    public HumanAgentService(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient { BaseAddress = new Uri("http://localhost:5000/") };
    }

    private class ChatApiPayload
    {
        public Guid? SessionId { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    private class ApiResponseWrapper<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
    }

    private class AiResponsePayload
    {
        public string SuggestedResponse { get; set; } = string.Empty;
        public string Intent { get; set; } = "unknown";
        public string Priority { get; set; } = "Normal";
        public bool NeedsHuman { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Language { get; set; } = "id-ID";
        public string GroundingState { get; set; } = "Grounded";
        public double Confidence { get; set; }
        public List<string> Citations { get; set; } = new();
        public int EvidenceCount { get; set; }
    }

    public async Task<(ChatMessageModel? Message, string? Error)> SendMessageAsync(Guid sessionId, string messageText, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/v1/agent/chat", new ChatApiPayload
            {
                SessionId = sessionId,
                Message = messageText
            }, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return (null, $"Server returned error: {response.StatusCode}");
            }

            var result = await response.Content.ReadFromJsonAsync<ApiResponseWrapper<AiResponsePayload>>(cancellationToken: cancellationToken);
            if (result == null || !result.Success || result.Data == null)
            {
                return (null, result?.Message ?? "Gagal mendapatkan respons AI.");
            }

            var aiMsg = new ChatMessageModel
            {
                Id = Guid.NewGuid(),
                Content = result.Data.SuggestedResponse,
                IsUser = false,
                Timestamp = DateTime.UtcNow,
                Citations = result.Data.Citations,
                Confidence = result.Data.Confidence,
                GroundingState = result.Data.GroundingState,
                IsEscalated = result.Data.NeedsHuman,
                EvidenceCount = result.Data.EvidenceCount
            };

            return (aiMsg, null);
        }
        catch (Exception ex)
        {
            return (null, $"Koneksi terputus: {ex.Message}");
        }
    }

    public async Task<bool> SubmitFeedbackAsync(Guid sessionId, Guid messageId, string feedbackType, string? reason, string? comment, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                ConversationId = sessionId,
                MessageId = messageId,
                FeedbackType = feedbackType,
                Reason = reason,
                CustomComment = comment,
                Language = "id-ID"
            };

            var response = await _httpClient.PostAsJsonAsync("api/v1/feedback", payload, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
