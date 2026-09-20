using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using dagangOnline.Application.DTOs;
using dagangOnline.Application.Interfaces;
using dagangOnline.Application.Services;
using dagangOnline.Application.Services.Economic;
using dagangOnline.Application.Services.Knowledge;
using dagangOnline.Application.Services.Language;
using dagangOnline.Application.Services.RAG;
using dagangOnline.Application.Services.Voice;
using dagangOnline.Domain.Chat;
using Xunit;

namespace dagangOnline.Tests;

public class NemotronVoiceCollaborationTests
{
    private readonly IConfiguration _config;
    private readonly NemotronVoiceAgentService _nemotronService;

    public NemotronVoiceCollaborationTests()
    {
        var inMemorySettings = new System.Collections.Generic.Dictionary<string, string?>
        {
            {"NVIDIA_NEMOTRON_VOICE_URL", "https://api.nvidia.com/v1/voice/nemotron-voicechat-11b"},
            {"GROQ_API_KEY", "mock_key_for_testing"}
        };

        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var httpClient = new HttpClient();
        _nemotronService = new NemotronVoiceAgentService(
            httpClient,
            _config,
            NullLogger<NemotronVoiceAgentService>.Instance);
    }

    [Fact]
    public void ModelStatus_ShouldReflectNemotronVoiceChat11BAndBlueprint()
    {
        // Act
        var status = _nemotronService.GetModelStatus();

        // Assert
        Assert.NotNull(status);
        Assert.Contains("NVIDIA-NemotronLabs-VoiceChat-11B", status.ModelName);
        Assert.Equal("NVIDIA-AI-Blueprints/nemotron-voice-agent", status.BlueprintReference);
        Assert.True(status.IsReady);
        Assert.True(status.TargetTurnTakingLatencyMs <= 300.0);
        Assert.Contains("Nemotron-Nusantara-Warm", status.SupportedPersonas);
    }

    [Fact]
    public async Task TranscribeAudioBase64Async_ShouldReturnValidTranscriptAndTelemetry()
    {
        // Arrange: Generate dummy base64 PCM audio
        byte[] fakeWavHeader = Encoding.ASCII.GetBytes("RIFF1234WAVEfmt ");
        string base64Audio = Convert.ToBase64String(fakeWavHeader);

        // Act
        var result = await _nemotronService.TranscribeAudioBase64Async(base64Audio, "audio/wav", "id-ID");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.False(string.IsNullOrWhiteSpace(result.Transcript));
        Assert.True(result.Confidence > 0.8);
        Assert.NotNull(result.AcousticProfile);
        Assert.True(result.AcousticProfile.TurnTakingLatencyMs > 0);
    }

    [Fact]
    public async Task AnalyzeAcousticStreamAsync_ShouldDetectToneAndCadence()
    {
        // Arrange
        string transcript = "Saya ingin komplain pesanan saya belum sampai padahal sudah 3 hari";

        // Act
        var telemetry = await _nemotronService.AnalyzeAcousticStreamAsync(null, transcript, 2.5);

        // Assert
        Assert.NotNull(telemetry);
        Assert.Contains(telemetry.EmotionTone, new[] { "Calm", "Urgent", "Inquisitive", "Hesitant", "Tenang", "Cemas / Bingung", "Marah / Frustrasi" });
        Assert.True(telemetry.SpeechRateWpm > 50);
        Assert.True(telemetry.TurnTakingLatencyMs >= 80);
    }

    [Fact]
    public async Task SynthesizeSpokenVoiceAsync_ShouldGenerateValidWavAudioAndProsody()
    {
        // Arrange
        var request = new VoiceSynthesisRequestDto
        {
            Text = "Halo Kak! Pesanan Anda dengan nomor DO-8821 sedang dalam proses pengiriman kurir ekspedisi.",
            TargetLanguage = "id-ID",
            ReturnAudioStream = true
        };

        // Act
        var response = await _nemotronService.SynthesizeSpokenVoiceAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.False(string.IsNullOrWhiteSpace(response.SpokenScript));
        Assert.False(string.IsNullOrWhiteSpace(response.AudioBase64));
        Assert.Equal("audio/wav", response.AudioFormat);

        // Verify that returned Base64 is valid WAV binary with RIFF header
        byte[] audioBytes = Convert.FromBase64String(response.AudioBase64);
        Assert.True(audioBytes.Length > 44);
        string riffHeader = Encoding.ASCII.GetString(audioBytes, 0, 4);
        Assert.Equal("RIFF", riffHeader);
    }

    [Fact]
    public void DetectBargeIn_WhenAgentIsSpeakingAndVoiceDetected_ShouldReturnTrue()
    {
        // Arrange: Audio packet present while agent is currently outputting voice
        string audioPacket = Convert.ToBase64String(new byte[128]);

        // Act
        bool bargeInDetected = _nemotronService.DetectBargeIn(audioPacket, isAgentCurrentlySpeaking: true);
        bool noBargeIn = _nemotronService.DetectBargeIn(audioPacket, isAgentCurrentlySpeaking: false);

        // Assert
        Assert.True(bargeInDetected);
        Assert.False(noBargeIn);
    }

    [Fact]
    public async Task CollaborativeOrchestrator_ShouldExecuteDualAgentTurn()
    {
        // Arrange
        var mockAiService = new MockAiChatService();

        var orchestrator = new CollaborativeAgentOrchestrator(
            _nemotronService,
            null,
            mockAiService,
            NullLogger<CollaborativeAgentOrchestrator>.Instance);

        var chatRequest = new CollaborativeChatRequestDto
        {
            TextPrompt = "Bagaimana cara membeli batik tulis nusantara?",
            ReturnAudio = true
        };

        // Act
        var result = await orchestrator.ProcessCollaborativeTurnAsync(chatRequest);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.False(string.IsNullOrWhiteSpace(result.GroundedTextResponse));
        Assert.False(string.IsNullOrWhiteSpace(result.SpokenResponse));
        Assert.False(string.IsNullOrWhiteSpace(result.AudioBase64));
        Assert.NotNull(result.AcousticTelemetry);
        Assert.Contains("Nemotron", result.OrchestrationMode);
        Assert.Contains("Qwen", result.OrchestrationMode);
    }

    // Mock AI service returning predictable Qwen responses
    private class MockAiChatService : IAiChatService
    {
        public Task<AiSuggestionDto> GetChatResponseAsync(string userMessage, string contextData, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new AiSuggestionDto
            {
                SuggestedResponse = "Tentu! Untuk membeli Batik Tulis Nusantara di dagangOnline, silakan pilih produk pada katalog dan lakukan checkout.",
                Confidence = 0.98,
                Intent = "order_inquiry",
                Language = "id",
                Priority = "Normal",
                NeedsHuman = false,
                GroundingState = "Grounded"
            });
        }
    }
}
