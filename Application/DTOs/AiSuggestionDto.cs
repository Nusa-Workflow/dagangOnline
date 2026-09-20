using System.Text.Json.Serialization;

namespace dagangOnline.Application.DTOs;

public class AiSuggestionDto
{
    [JsonPropertyName("suggested_response")]
    public string SuggestedResponse { get; set; } = string.Empty;

    [JsonPropertyName("intent")]
    public string Intent { get; set; } = "unknown";

    [JsonPropertyName("priority")]
    public string Priority { get; set; } = "Normal";

    [JsonPropertyName("needs_human")]
    public bool NeedsHuman { get; set; } = false;

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    [JsonPropertyName("language")]
    public string Language { get; set; } = "id-ID";

    [JsonPropertyName("grounding_state")]
    public string GroundingState { get; set; } = "Grounded";

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; } = 1.0;

    [JsonPropertyName("citations")]
    public List<string> Citations { get; set; } = new();

    [JsonPropertyName("evidence_count")]
    public int EvidenceCount { get; set; } = 0;

    [JsonPropertyName("spoken_response")]
    public string SpokenResponse { get; set; } = string.Empty;

    [JsonPropertyName("acoustic_tone")]
    public string AcousticTone { get; set; } = "Calm";

    [JsonPropertyName("turn_taking_latency_ms")]
    public double TurnTakingLatencyMs { get; set; } = 240.0;

    [JsonPropertyName("voice_audio_base64")]
    public string? VoiceAudioBase64 { get; set; }
}
