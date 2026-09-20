using System;
using System.Collections.Generic;

namespace dagangOnline.Application.DTOs;

public class VoiceStreamChunkDto
{
    public string AudioBase64 { get; set; } = string.Empty;
    public string MimeType { get; set; } = "audio/webm";
    public int SampleRate { get; set; } = 16000;
    public bool IsFinalChunk { get; set; }
    public Guid SessionId { get; set; }
    public long ChunkSequence { get; set; }
    public string? ExpectedLanguage { get; set; }
    public bool IsAgentCurrentlySpeaking { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class AcousticTelemetryDto
{
    public double TurnTakingLatencyMs { get; set; } // Latency between user speech end and bot response start
    public double SpeechRateWpm { get; set; } // Words per minute
    public string EmotionTone { get; set; } = "Calm"; // Calm, Inquisitive, Urgent, Frustrated, Hesitant
    public double Confidence { get; set; } = 0.95;
    public bool BargeInDetected { get; set; } // Interruption occurred while agent/bot was speaking
    public double VoiceActivityScore { get; set; } // 0.0 - 1.0 (VAD)
    public string DetectedDialect { get; set; } = "id-ID"; // id-ID, jv-ID, su-ID, en-US
    public DateTime MeasuredAt { get; set; } = DateTime.UtcNow;
}

public class VoiceTranscriptionResponseDto
{
    public bool Success { get; set; }
    public string Transcript { get; set; } = string.Empty;
    public string DetectedLanguage { get; set; } = "id-ID";
    public double Confidence { get; set; } = 0.95;
    public double DurationSeconds { get; set; }
    public AcousticTelemetryDto AcousticProfile { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

public class VoiceSynthesisRequestDto
{
    public string Text { get; set; } = string.Empty;
    public string TargetLanguage { get; set; } = "id-ID";
    public string VoicePersona { get; set; } = "Nemotron-Nusantara-Warm"; // Conversational, Professional, Friendly
    public double SpeechSpeed { get; set; } = 1.0;
    public double Pitch { get; set; } = 1.0;
    public bool ReturnAudioStream { get; set; } = true;
}

public class VoiceSynthesisResponseDto
{
    public bool Success { get; set; }
    public string SpokenScript { get; set; } = string.Empty;
    public string AudioBase64 { get; set; } = string.Empty;
    public string AudioFormat { get; set; } = "audio/wav";
    public double DurationSeconds { get; set; }
    public int AudioByteSize { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class CollaborativeChatRequestDto
{
    public Guid? SessionId { get; set; }
    public string? TextMessage { get; set; }
    public string? TextPrompt { get => TextMessage; set => TextMessage = value; }
    public string? AudioBase64 { get; set; }
    public string AudioMimeType { get; set; } = "audio/webm";
    public string? MimeType { get => AudioMimeType; set => AudioMimeType = value ?? "audio/webm"; }
    public string Language { get; set; } = "id-ID";
    public bool EnableVoiceSynthesis { get; set; } = true;
    public bool ReturnAudio { get => EnableVoiceSynthesis; set => EnableVoiceSynthesis = value; }
    public bool IsAgentView { get; set; } = false;
}

public class CollaborativeChatResponseDto
{
    public bool Success { get; set; }
    public Guid? SessionId { get; set; }
    
    // Nemotron Voice Output
    public string UserTranscript { get; set; } = string.Empty;
    public string NemotronSpokenScript { get; set; } = string.Empty; // Concise conversational phrasing for speech
    public string SpokenResponse { get => NemotronSpokenScript; set => NemotronSpokenScript = value; }
    public string? SynthesizedAudioBase64 { get; set; }
    public string? AudioBase64 { get => SynthesizedAudioBase64; set => SynthesizedAudioBase64 = value; }
    public string AudioFormat { get; set; } = "audio/wav";
    
    // Qwen Cognitive Reasoning Output
    public string QwenReasoningResponse { get; set; } = string.Empty; // In-depth grounded text response
    public string GroundedTextResponse { get => QwenReasoningResponse; set => QwenReasoningResponse = value; }
    public string Intent { get; set; } = "general";
    public string Priority { get; set; } = "Normal";
    public bool NeedsHuman { get; set; }
    public string GroundingState { get; set; } = "Grounded";
    public double GroundingConfidence { get; set; } = 0.95;
    public List<string> MatchedCitations { get; set; } = new();
    
    // Acoustic Telemetry
    public AcousticTelemetryDto AcousticTelemetry { get; set; } = new();
    
    // Model metadata
    public string OrchestrationMode { get; set; } = "Nemotron-VoiceChat-11B + Qwen Dual-Agent";
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}

public class NemotronModelStatusDto
{
    public string ModelName { get; set; } = "NVIDIA-NemotronLabs-VoiceChat-11B";
    public string BlueprintReference { get; set; } = "NVIDIA-AI-Blueprints/nemotron-voice-agent";
    public string InferenceMode { get; set; } = "Interactive Streaming & Offline Inference";
    public bool IsReady { get; set; } = true;
    public double TargetTurnTakingLatencyMs { get; set; } = 280.0;
    public List<string> SupportedPersonas { get; set; } = new() { "Nemotron-Nusantara-Warm", "Nemotron-CS-Professional", "Nemotron-Empathetic-Helper" };
}
