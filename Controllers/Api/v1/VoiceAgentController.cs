using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using dagangOnline.Application.DTOs;
using dagangOnline.Application.Interfaces;
using dagangOnline.Models;

namespace dagangOnline.Controllers.Api.v1;

/// <summary>
/// API endpoints for NVIDIA Nemotron VoiceChat-11B &amp; Qwen Collaborative Voice/Text Agent.
/// Provides offline inference / interactive streaming ASR, acoustic telemetry, prosodic TTS,
/// barge-in detection, and dual-agent coordination.
/// </summary>
[ApiController]
[Route("api/v1/voice")]
[Produces("application/json")]
public class VoiceAgentController : ControllerBase
{
    private readonly ICollaborativeAgentOrchestrator _orchestrator;
    private readonly INemotronVoiceAgentService _nemotronService;
    private readonly INemotronStrategicVoiceAgent _strategicVoiceAgent;
    private readonly IDatasetFineTuningEngine? _fineTuningEngine;

    public VoiceAgentController(
        ICollaborativeAgentOrchestrator orchestrator,
        INemotronVoiceAgentService nemotronService,
        INemotronStrategicVoiceAgent strategicVoiceAgent,
        IDatasetFineTuningEngine? fineTuningEngine = null)
    {
        _orchestrator = orchestrator;
        _nemotronService = nemotronService;
        _strategicVoiceAgent = strategicVoiceAgent;
        _fineTuningEngine = fineTuningEngine;
    }

    /// <summary>
    /// Collaborative dual-agent processing: Nemotron listens &amp; decodes prosody/tone,
    /// Qwen reasons with RAG grounding, and Nemotron formats natural spoken audio.
    /// </summary>
    [HttpPost("chat")]
    [ProducesResponseType(typeof(ApiResponse<CollaborativeChatResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CollaborativeChatResponseDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CollaborativeChat(
        [FromBody] CollaborativeChatRequestDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.TextPrompt) && string.IsNullOrWhiteSpace(request.AudioBase64))
        {
            return BadRequest(ApiResponse<CollaborativeChatResponseDto>.Fail("Baik teks maupun rekaman suara harus diisi."));
        }

        try
        {
            var response = await _orchestrator.ProcessCollaborativeTurnAsync(request, cancellationToken);
            return Ok(ApiResponse<CollaborativeChatResponseDto>.Ok(response));
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<CollaborativeChatResponseDto>.Fail($"Gagal memproses giliran kolaborasi suara: {ex.Message}"));
        }
    }

    /// <summary>
    /// Transcribes audio stream or base64 chunk using NVIDIA Nemotron VoiceChat-11B ASR.
    /// </summary>
    [HttpPost("transcribe")]
    [ProducesResponseType(typeof(ApiResponse<VoiceTranscriptionResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<VoiceTranscriptionResponseDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> TranscribeAudio(
        [FromBody] VoiceStreamChunkDto chunk,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(chunk.AudioBase64))
        {
            return BadRequest(ApiResponse<VoiceTranscriptionResponseDto>.Fail("Audio chunk kosong."));
        }

        var result = await _nemotronService.TranscribeAudioBase64Async(
            chunk.AudioBase64,
            chunk.MimeType,
            chunk.ExpectedLanguage,
            cancellationToken);

        return Ok(ApiResponse<VoiceTranscriptionResponseDto>.Ok(result));
    }

    /// <summary>
    /// Transcribes uploaded audio file directly.
    /// </summary>
    [HttpPost("transcribe-file")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<VoiceTranscriptionResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> TranscribeAudioFile(
        IFormFile file,
        [FromQuery] string? language = null,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(ApiResponse<VoiceTranscriptionResponseDto>.Fail("File audio tidak ditemukan."));
        }

        using var stream = file.OpenReadStream();
        var result = await _nemotronService.TranscribeAudioAsync(
            stream,
            file.ContentType,
            language,
            cancellationToken);

        return Ok(ApiResponse<VoiceTranscriptionResponseDto>.Ok(result));
    }

    /// <summary>
    /// Synthesizes spoken prosody script and audio WAV using NVIDIA Nemotron VoiceChat-11B pipeline.
    /// </summary>
    [HttpPost("synthesize")]
    [ProducesResponseType(typeof(ApiResponse<VoiceSynthesisResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SynthesizeVoice(
        [FromBody] VoiceSynthesisRequestDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return BadRequest(ApiResponse<VoiceSynthesisResponseDto>.Fail("Teks untuk sintesis suara tidak boleh kosong."));
        }

        var response = await _nemotronService.SynthesizeSpokenVoiceAsync(request, cancellationToken);
        return Ok(ApiResponse<VoiceSynthesisResponseDto>.Ok(response));
    }

    /// <summary>
    /// Checks if incoming customer audio stream constitutes a barge-in interruption
    /// while the agent or chatbot is currently speaking.
    /// </summary>
    [HttpPost("barge-in")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public IActionResult CheckBargeIn([FromBody] VoiceStreamChunkDto chunk)
    {
        var isInterruption = _nemotronService.DetectBargeIn(chunk.AudioBase64, chunk.IsAgentCurrentlySpeaking);
        return Ok(ApiResponse<bool>.Ok(isInterruption, isInterruption ? "Barge-in detected." : "No interruption."));
    }

    /// <summary>
    /// Returns model telemetry, benchmark targets, and offline/streaming status.
    /// </summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(ApiResponse<NemotronModelStatusDto>), StatusCodes.Status200OK)]
    public IActionResult GetModelStatus()
    {
        var status = _nemotronService.GetModelStatus();
        return Ok(ApiResponse<NemotronModelStatusDto>.Ok(status));
    }

    /// <summary>
    /// NVIDIA Nemotron Strategic Voice Agent: Real-time 5-10 year (2026-2036) forecasting
    /// using analytics & synthetic data, Indonesian context (IKN, downstreaming), and multilingual voice output.
    /// </summary>
    [HttpPost("forecast/long-horizon")]
    [ProducesResponseType(typeof(ApiResponse<LongHorizonForecastResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<LongHorizonForecastResultDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateLongHorizonForecast(
        [FromBody] LongHorizonForecastRequestDto request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            return BadRequest(ApiResponse<LongHorizonForecastResultDto>.Fail("Request parameter tidak boleh kosong."));
        }

        if (request.HorizonYears < 5 || request.HorizonYears > 10)
        {
            return BadRequest(ApiResponse<LongHorizonForecastResultDto>.Fail("Horizon prediksi harus berkisar antara 5 hingga 10 tahun ke depan (2026 - 2036)."));
        }

        try
        {
            var result = await _strategicVoiceAgent.GenerateStrategicForecastWithVoiceAsync(request, cancellationToken);
            return Ok(ApiResponse<LongHorizonForecastResultDto>.Ok(result, "Berhasil menghasilkan prediksi masa depan 2026-2036 dengan audio Nemotron."));
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<LongHorizonForecastResultDto>.Fail($"Gagal memproses prediksi 5-10 tahun: {ex.Message}"));
        }
    }

    /// <summary>
    /// Returns supported options for long-horizon forecasting (sectors, scenarios, regions, languages).
    /// </summary>
    [HttpGet("forecast/options")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public IActionResult GetForecastOptions()
    {
        var options = new
        {
            startYear = 2026,
            minHorizonYears = 5,
            maxHorizonYears = 10,
            sectors = new[] { "Retail", "Agri", "F&B", "Tech", "Logistics", "Energy" },
            scenarios = new[] { "Baseline", "TechGreenAcceleration", "GeopoliticalShock", "DemographicDividendMax" },
            regions = new[] { "Nasional", "IKN_Kalimantan", "Jawa_Bali", "Sumatera", "Indonesia_Timur" },
            languages = new[]
            {
                new { code = "id", name = "Bahasa Indonesia", region = "Nasional", persona = "Nemotron-Nusantara-Warm" },
                new { code = "jv", name = "Basa Jawa", region = "Jawa Tengah, Jatim, DIY", persona = "Nemotron-Jawa-Kusuma" },
                new { code = "su", name = "Basa Sunda", region = "Jawa Barat & Banten", persona = "Nemotron-Sunda-Prabu" },
                new { code = "min", name = "Baso Minangkabau", region = "Sumatera Barat", persona = "Nemotron-Minang-Tuanku" },
                new { code = "mad", name = "Basa Madhura", region = "Madura & Tapal Kuda", persona = "Nemotron-Madura-Trunojoyo" },
                new { code = "ban", name = "Basa Bali", region = "Bali", persona = "Nemotron-Bali-Dewata" },
                new { code = "bug", name = "Basa Bugis", region = "Sulawesi Selatan", persona = "Nemotron-Bugis-Sawerigading" },
                new { code = "bjn", name = "Bahasa Banjar", region = "Kalimantan Selatan & IKN", persona = "Nemotron-Banjar-Pangeran" },
                new { code = "btk", name = "Bahasa Batak", region = "Sumatera Utara", persona = "Nemotron-Batak-Singamangaraja" },
                new { code = "en", name = "English (Global)", region = "Global Enterprise", persona = "Nemotron-Global-Executive" }
            }
        };

        return Ok(ApiResponse<object>.Ok(options));
    }

    /// <summary>
    /// Trains and calibrates Nemotron voice and reasoning models using imported datasets
    /// (dataset_umkm_align.json, research_mid_training_id.jsonl, scaled_multilingual_fine_tuning.csv, etc.)
    /// </summary>
    [HttpPost("train-from-dataset")]
    [ProducesResponseType(typeof(ApiResponse<DatasetFineTuningReportDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> TrainFromDataset(CancellationToken cancellationToken)
    {
        if (_fineTuningEngine == null)
        {
            return Ok(ApiResponse<DatasetFineTuningReportDto>.Fail("Layanan fine-tuning dataset tidak tersedia."));
        }

        var report = await _fineTuningEngine.TrainModelFromDatasetsAsync(cancellationToken);
        return Ok(ApiResponse<DatasetFineTuningReportDto>.Ok(report, report.SummaryMessage));
    }

    /// <summary>
    /// Returns current training and empirical dataset calibration status.
    /// </summary>
    [HttpGet("training-status")]
    [ProducesResponseType(typeof(ApiResponse<DatasetFineTuningReportDto>), StatusCodes.Status200OK)]
    public IActionResult GetTrainingStatus()
    {
        if (_fineTuningEngine == null)
        {
            return Ok(ApiResponse<DatasetFineTuningReportDto>.Fail("Layanan fine-tuning dataset tidak aktif."));
        }

        var status = _fineTuningEngine.GetCurrentTrainingStatus();
        return Ok(ApiResponse<DatasetFineTuningReportDto>.Ok(status));
    }
}
