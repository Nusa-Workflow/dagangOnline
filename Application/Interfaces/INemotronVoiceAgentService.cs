using System.IO;
using System.Threading;
using System.Threading.Tasks;
using dagangOnline.Application.DTOs;

namespace dagangOnline.Application.Interfaces;

public interface INemotronVoiceAgentService
{
    Task<VoiceTranscriptionResponseDto> TranscribeAudioAsync(
        Stream audioStream,
        string mimeType = "audio/webm",
        string? expectedLanguage = null,
        CancellationToken cancellationToken = default);

    Task<VoiceTranscriptionResponseDto> TranscribeAudioBase64Async(
        string audioBase64,
        string mimeType = "audio/webm",
        string? expectedLanguage = null,
        CancellationToken cancellationToken = default);

    Task<AcousticTelemetryDto> AnalyzeAcousticStreamAsync(
        string? audioBase64,
        string transcript,
        double speechDurationSeconds = 1.0,
        CancellationToken cancellationToken = default);

    Task<VoiceSynthesisResponseDto> SynthesizeSpokenVoiceAsync(
        VoiceSynthesisRequestDto request,
        CancellationToken cancellationToken = default);

    bool DetectBargeIn(string audioBase64, bool isAgentCurrentlySpeaking);

    NemotronModelStatusDto GetModelStatus();
}
