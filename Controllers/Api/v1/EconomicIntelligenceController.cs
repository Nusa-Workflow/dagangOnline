using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using dagangOnline.Application.DTOs;
using dagangOnline.Application.Services.Economic;
using dagangOnline.Domain.Economic;

namespace dagangOnline.Controllers.Api.v1;

[ApiController]
[Route("api/v1/economic")]
[Produces("application/json")]
public class EconomicIntelligenceController : ControllerBase
{
    private readonly EconomicGraphEngine _graphEngine;
    private readonly EconomicForecastingService _forecastingService;
    private readonly ExplainableAiService _xaiService;
    private readonly EconomicMultiAgentSystem _multiAgentSystem;

    public EconomicIntelligenceController(
        EconomicGraphEngine graphEngine,
        EconomicForecastingService forecastingService,
        ExplainableAiService xaiService,
        EconomicMultiAgentSystem multiAgentSystem)
    {
        _graphEngine = graphEngine;
        _forecastingService = forecastingService;
        _xaiService = xaiService;
        _multiAgentSystem = multiAgentSystem;
    }

    /// <summary>
    /// Tarik seluruh Knowledge Graph Model (Nodes dan Edges) indikator makro, komoditas, dan transmisi sektor.
    /// </summary>
    [HttpGet("graph")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public IActionResult GetKnowledgeGraph()
    {
        var nodes = _graphEngine.GetAllNodes();
        var edges = _graphEngine.GetAllEdges();

        return Ok(ApiResponse<object>.Ok(new
        {
            totalNodes = nodes.Count,
            totalEdges = edges.Count,
            nodes,
            edges
        }, "Knowledge Graph Model berhasil dimuat."));
    }

    /// <summary>
    /// Tarik Features API endpoint: Mengekstrak vektor fitur dari Graph Model untuk ML/DL model.
    /// </summary>
    [HttpPost("features")]
    [ProducesResponseType(typeof(ApiResponse<GraphFeatureVectorDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<GraphFeatureVectorDto>), StatusCodes.Status400BadRequest)]
    public IActionResult ExtractGraphFeatures([FromBody] ForecastRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.TargetEntityId))
        {
            return BadRequest(ApiResponse<GraphFeatureVectorDto>.Fail("TargetEntityId wajib ditentukan (contoh: INFLATION_CPI, BRENT_OIL, RETAIL_ECOMMERCE)."));
        }

        var featureVector = _graphEngine.ExtractFeatureVector(
            request.TargetEntityId,
            request.ForecastHorizonMonths <= 0 ? 3 : request.ForecastHorizonMonths,
            request.CounterfactualOverrides);

        return Ok(ApiResponse<GraphFeatureVectorDto>.Ok(featureVector, "Vektor fitur berhasil diekstraksi dari Graph Model."));
    }

    /// <summary>
    /// Prediksi indikator ekonomi/sektor menggunakan model Machine Learning & Deep Learning berbasis fitur graf.
    /// </summary>
    [HttpPost("forecast")]
    [ProducesResponseType(typeof(ApiResponse<ForecastResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GenerateForecast([FromBody] ForecastRequestDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.TargetEntityId))
        {
            return BadRequest(ApiResponse<ForecastResultDto>.Fail("TargetEntityId wajib diisi."));
        }

        var result = await _forecastingService.GenerateForecastAsync(request, cancellationToken);
        return Ok(ApiResponse<ForecastResultDto>.Ok(result, "Prediksi ML/DL ekonomi berhasil dihitung."));
    }

    /// <summary>
    /// Model Explainable AI (XAI) untuk pengambilan keputusan strategis (SHAP feature attribution & sensitivitas).
    /// </summary>
    [HttpPost("explain")]
    [ProducesResponseType(typeof(ApiResponse<XaiExplanationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExplainPrediction([FromQuery] string targetEntityId, [FromQuery] int horizonMonths = 3, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(targetEntityId))
        {
            return BadRequest(ApiResponse<XaiExplanationDto>.Fail("targetEntityId wajib ditentukan."));
        }

        var explanation = await _xaiService.ExplainForecastAsync(targetEntityId, horizonMonths, cancellationToken);
        return Ok(ApiResponse<XaiExplanationDto>.Ok(explanation, "Analisis Explainable AI (XAI) berhasil digenerasi."));
    }

    /// <summary>
    /// Simulasi skenario What-If untuk mengukur dampak guncangan variabel ekonomi terhadap proyeksi.
    /// </summary>
    [HttpPost("what-if")]
    [ProducesResponseType(typeof(ApiResponse<WhatIfScenarioResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SimulateWhatIf([FromBody] WhatIfScenarioRequestDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.BaseTargetEntityId))
        {
            return BadRequest(ApiResponse<WhatIfScenarioResultDto>.Fail("BaseTargetEntityId wajib diisi."));
        }

        var result = await _xaiService.SimulateWhatIfAsync(request, cancellationToken);
        return Ok(ApiResponse<WhatIfScenarioResultDto>.Ok(result, "Simulasi skenario What-If berhasil diselesaikan."));
    }

    /// <summary>
    /// Eksekusi workflow otonom Agentic AI & Multi-Agent untuk riset, pemantauan ekonomi, dan laporan intelijen.
    /// </summary>
    [HttpPost("agent/analyze")]
    [ProducesResponseType(typeof(ApiResponse<MultiAgentAnalysisReportDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExecuteMultiAgentWorkflow([FromBody] MultiAgentTaskRequestDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Query) && string.IsNullOrWhiteSpace(request.TargetSectorOrCommodity))
        {
            return BadRequest(ApiResponse<MultiAgentAnalysisReportDto>.Fail("Query atau TargetSectorOrCommodity wajib diisi."));
        }

        var report = await _multiAgentSystem.ExecuteAutonomousWorkflowAsync(request, cancellationToken);
        return Ok(ApiResponse<MultiAgentAnalysisReportDto>.Ok(report, "Analisis Multi-Agent System selesai dieksekusi."));
    }
}
