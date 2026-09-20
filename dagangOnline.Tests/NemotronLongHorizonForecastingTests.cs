using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using dagangOnline.Application.DTOs;
using dagangOnline.Application.Interfaces;
using dagangOnline.Application.Services.Economic;
using dagangOnline.Application.Services.Voice;
using dagangOnline.Controllers.Api.v1;
using dagangOnline.Models;

namespace dagangOnline.Tests;

public class NemotronLongHorizonForecastingTests
{
    private readonly ILongHorizonSyntheticDataEngine _syntheticEngine;
    private readonly INemotronVoiceAgentService _nemotronVoiceService;
    private readonly INemotronStrategicVoiceAgent _strategicVoiceAgent;

    public NemotronLongHorizonForecastingTests()
    {
        var testEnv = new TestHostEnvironment();

        _syntheticEngine = new LongHorizonSyntheticDataEngine(
            testEnv,
            NullLogger<LongHorizonSyntheticDataEngine>.Instance);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "NVIDIA_NEMOTRON_VOICE_URL", "https://api.nvidia.com/v1/voice/nemotron-voicechat-11b" },
                { "NVIDIA_API_KEY", "nvapi-test-key" }
            })
            .Build();

        _nemotronVoiceService = new NemotronVoiceAgentService(
            new HttpClient(),
            config,
            NullLogger<NemotronVoiceAgentService>.Instance);

        _strategicVoiceAgent = new NemotronStrategicVoiceAgent(
            _syntheticEngine,
            _nemotronVoiceService,
            NullLogger<NemotronStrategicVoiceAgent>.Instance);
    }

    [Fact]
    public async Task GenerateForecastAsync_ShouldProduce5YearHorizon_From2026To2031()
    {
        var req = new LongHorizonForecastRequestDto
        {
            Sector = "Retail",
            HorizonYears = 5,
            RegionalFocus = "Nasional",
            ScenarioType = "Baseline",
            Language = "id"
        };

        var result = await _syntheticEngine.GenerateForecastAsync(req);

        Assert.NotNull(result);
        Assert.Equal(2026, result.StartYear);
        Assert.Equal(2031, result.EndYear);
        Assert.Equal(6, result.Trajectory.Count); // 2026, 2027, 2028, 2029, 2030, 2031
        Assert.Equal("Retail", result.Sector);
        Assert.NotEmpty(result.StrategicNeedsIdentified);
        Assert.NotEmpty(result.ActionableRecommendations);

        // Check trajectory data integrity
        var p2026 = result.Trajectory.First(p => p.Year == 2026);
        var p2031 = result.Trajectory.First(p => p.Year == 2031);
        Assert.True(p2031.TechAdoptionRate > p2026.TechAdoptionRate);
        Assert.Contains("G_GeopoliticalTrade", p2031.SyntheticVector.Keys);
        Assert.Contains("Q_AiAgentReadiness", p2031.SyntheticVector.Keys);
    }

    [Fact]
    public async Task GenerateForecastAsync_ShouldProduce10YearHorizon_From2026To2036()
    {
        var req = new LongHorizonForecastRequestDto
        {
            Sector = "Logistics",
            HorizonYears = 10,
            RegionalFocus = "IKN_Kalimantan",
            ScenarioType = "TechGreenAcceleration",
            Language = "id"
        };

        var result = await _syntheticEngine.GenerateForecastAsync(req);

        Assert.NotNull(result);
        Assert.Equal(2026, result.StartYear);
        Assert.Equal(2036, result.EndYear);
        Assert.Equal(11, result.Trajectory.Count); // 2026 to 2036
        Assert.Equal("IKN_Kalimantan", result.RegionalFocus);

        var p2036 = result.Trajectory.First(p => p.Year == 2036);
        Assert.True(p2036.GreenLogisticsShare > 30.0); // IKN green corridor expansion
    }

    [Theory]
    [InlineData("id", "Halo rekan bisnis Indonesia")]
    [InlineData("su", "Sampurasun wargi sadaya")]
    [InlineData("jv", "Sugeng rawuh para mitra bisnis")]
    [InlineData("min", "Salamaik datang dunsanak sadonyo")]
    [InlineData("mad", "Salampet rabu taretan sadajana")]
    [InlineData("ban", "Om Swastyastu semeton sami")]
    [InlineData("bug", "Salama' ki silessureng maneng")]
    [InlineData("bjn", "Salamat datang bubuhan pambisnis sabarataan")]
    [InlineData("btk", "Horas ma di hita sasudena dongan bisnis")]
    [InlineData("en", "Greetings. This is your NVIDIA Nemotron")]
    public async Task GenerateStrategicForecastWithVoiceAsync_ShouldGenerateMultilingualBriefingAndAudio(string lang, string expectedPrefix)
    {
        var req = new LongHorizonForecastRequestDto
        {
            Sector = "Retail",
            HorizonYears = 5,
            RegionalFocus = "Nasional",
            Language = lang,
            ReturnVoiceAudio = true
        };

        var result = await _strategicVoiceAgent.GenerateStrategicForecastWithVoiceAsync(req);

        Assert.NotNull(result);
        Assert.Contains(expectedPrefix, result.SpokenVoiceScript);
        Assert.False(string.IsNullOrEmpty(result.VoiceAudioBase64));
        Assert.Equal("NVIDIA-NemotronLabs-VoiceChat-11B-Strategic", result.Telemetry.ModelVariant);
    }

    [Fact]
    public async Task Controller_LongHorizonForecast_ShouldEnforceHorizonRangeValidation()
    {
        var orchestrator = new FakeOrchestrator();
        var controller = new VoiceAgentController(orchestrator, _nemotronVoiceService, _strategicVoiceAgent);

        // Invalid: horizon 3 years (below min 5)
        var invalidReq = new LongHorizonForecastRequestDto
        {
            HorizonYears = 3
        };

        var badAction = await controller.GenerateLongHorizonForecast(invalidReq, CancellationToken.None);
        var badResult = Assert.IsType<BadRequestObjectResult>(badAction);
        var badPayload = Assert.IsType<ApiResponse<LongHorizonForecastResultDto>>(badResult.Value);
        Assert.False(badPayload.Success);
        Assert.Contains("5 hingga 10 tahun", badPayload.Message);

        // Valid: horizon 5 years
        var validReq = new LongHorizonForecastRequestDto
        {
            Sector = "Tech",
            HorizonYears = 5,
            RegionalFocus = "Nasional",
            Language = "id",
            ReturnVoiceAudio = true
        };

        var okAction = await controller.GenerateLongHorizonForecast(validReq, CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(okAction);
        var okPayload = Assert.IsType<ApiResponse<LongHorizonForecastResultDto>>(okResult.Value);
        Assert.True(okPayload.Success);
        Assert.NotNull(okPayload.Data);
        Assert.Equal(2026, okPayload.Data.StartYear);
        Assert.Equal(2031, okPayload.Data.EndYear);
    }

    [Fact]
    public void Controller_GetForecastOptions_ShouldReturnSupportedSectorsAndLanguages()
    {
        var orchestrator = new FakeOrchestrator();
        var controller = new VoiceAgentController(orchestrator, _nemotronVoiceService, _strategicVoiceAgent);

        var action = controller.GetForecastOptions();
        var okResult = Assert.IsType<OkObjectResult>(action);
        var payload = Assert.IsType<ApiResponse<object>>(okResult.Value);
        Assert.True(payload.Success);
        Assert.NotNull(payload.Data);
    }

    [Theory]
    [InlineData("kados pundi carane tuku beras neng kene", "jv-ID")]
    [InlineData("kumaha carana mésér produk ieu", "su-ID")]
    [InlineData("baa caronyo mambali galeh ondeh dunsanak", "min-ID")]
    [InlineData("dekremma carana melle bhabin ka'dinto taretan", "mad-ID")]
    [InlineData("kenken carane meli ajengan ring semeton", "ban-ID")]
    [InlineData("pekkogi carana melli barang silessureng", "bug-ID")]
    [InlineData("kayapa caranya manukar gasan bubuhan pian", "bjn-ID")]
    [InlineData("boha do carana manuhor dongan hita", "btk-ID")]
    [InlineData("how do I order this product online", "en-US")]
    [InlineData("bagaimana cara membeli produk lokal di toko ini", "id-ID")]
    public void LanguageService_ShouldDetectRegionalLanguagesAccurately(string input, string expectedCode)
    {
        var langService = new dagangOnline.Application.Services.Language.LanguageService();
        var detected = langService.DetectLanguage(input);
        Assert.Equal(expectedCode, detected);
    }

    [Fact]
    public async Task DatasetFineTuningEngine_ShouldIngestAndCalibrateFromImportDatasets()
    {
        var testEnv = new TestHostEnvironment();
        var engine = new DatasetFineTuningEngine(testEnv, NullLogger<DatasetFineTuningEngine>.Instance);

        var report = await engine.TrainModelFromDatasetsAsync();

        Assert.NotNull(report);
        Assert.True(report.Success);
        Assert.True(report.TotalRecordsAbsorbed > 0);
        Assert.True(report.UmkmProfilesLearned > 0);
        Assert.True(report.ResearchInstructionsLearned > 0);

        var status = engine.GetCurrentTrainingStatus();
        Assert.Equal("Trained & Active", status.Status);

        var retailProfile = engine.GetSectorEmpiricalMetrics("Retail");
        Assert.NotNull(retailProfile);
        Assert.True(retailProfile.AverageOmset > 0);
    }

    private class TestHostEnvironment : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "dagangOnline";
        public string WebRootPath { get; set; } = FindSolutionRoot();
        public IFileProvider WebRootFileProvider { get; set; } = null!;
        public string ContentRootPath { get; set; } = FindSolutionRoot();
        public IFileProvider ContentRootFileProvider { get; set; } = null!;

        private static string FindSolutionRoot()
        {
            var dir = Directory.GetCurrentDirectory();
            while (!string.IsNullOrEmpty(dir))
            {
                if (File.Exists(Path.Combine(dir, "dagangOnline.sln")) ||
                    (Directory.Exists(Path.Combine(dir, "Data", "Imports")) && File.Exists(Path.Combine(dir, "Data", "Imports", "research_mid_training_id.jsonl"))))
                {
                    return dir;
                }
                var parent = Directory.GetParent(dir);
                if (parent == null) break;
                dir = parent.FullName;
            }
            return Directory.GetCurrentDirectory();
        }
    }

    private class FakeOrchestrator : ICollaborativeAgentOrchestrator
    {
        public Task<CollaborativeChatResponseDto> ProcessCollaborativeTurnAsync(CollaborativeChatRequestDto request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new CollaborativeChatResponseDto());
        }

        public Task<CollaborativeChatResponseDto> ExecuteCollaborativeChatAsync(CollaborativeChatRequestDto request, dagangOnline.Domain.Chat.Conversation? session = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new CollaborativeChatResponseDto());
        }

        public Task<CollaborativeChatResponseDto> GenerateDualAgentDraftsAsync(string customerMessage, dagangOnline.Domain.Chat.Conversation session, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new CollaborativeChatResponseDto());
        }
    }
}
