using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using dagangOnline.Application.Services.Economic;
using dagangOnline.Domain.Economic;
using Xunit;

namespace dagangOnline.Tests;

public class EconomicIntelligenceTests
{
    private readonly EconomicGraphEngine _graphEngine;
    private readonly EconomicForecastingService _forecastingService;
    private readonly ExplainableAiService _xaiService;
    private readonly EconomicMultiAgentSystem _multiAgentSystem;

    public EconomicIntelligenceTests()
    {
        _graphEngine = new EconomicGraphEngine();
        _forecastingService = new EconomicForecastingService(_graphEngine);
        _xaiService = new ExplainableAiService(_graphEngine, _forecastingService);
        _multiAgentSystem = new EconomicMultiAgentSystem(_graphEngine, _forecastingService, _xaiService);
    }

    [Fact]
    public void GraphEngine_Should_Extract_Feature_Vector_And_Topological_Drivers()
    {
        // Act: Extract graph feature vector for Retail & E-commerce sector
        var featureVector = _graphEngine.ExtractFeatureVector("RETAIL_ECOMMERCE", 3);

        // Assert
        Assert.NotNull(featureVector);
        Assert.Equal("RETAIL_ECOMMERCE", featureVector.TargetEntityId);
        Assert.True(featureVector.InDegreeCentrality > 0, "Target node should have connected incoming drivers.");
        Assert.NotEmpty(featureVector.KeyDrivers);
        Assert.True(featureVector.RawFeatureVector.ContainsKey("transmission_score"));
        Assert.True(featureVector.RawFeatureVector.ContainsKey("in_degree"));

        // Verify connected driver features
        var driver = featureVector.KeyDrivers.First();
        Assert.False(string.IsNullOrEmpty(driver.SourceNodeId));
        Assert.True(driver.EdgeWeight != 0.0);
    }

    [Fact]
    public async Task ForecastingService_Should_Produce_MultiHorizon_Forecast_And_Confidence_Intervals()
    {
        // Arrange
        var request = new ForecastRequestDto
        {
            TargetEntityId = "INFLATION_CPI",
            ForecastHorizonMonths = 6,
            IncludeGraphFeatures = true
        };

        // Act
        var result = await _forecastingService.GenerateForecastAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("INFLATION_CPI", result.TargetEntityId);
        Assert.Equal(6, result.Predictions.Count);
        Assert.True(result.ModelConfidence > 0.5);

        // Verify confidence intervals consistency
        foreach (var point in result.Predictions)
        {
            Assert.True(point.ConfidenceLower95 <= point.PredictedValue, "Lower 95% bound must be <= predicted point.");
            Assert.True(point.ConfidenceUpper95 >= point.PredictedValue, "Upper 95% bound must be >= predicted point.");
            Assert.True(point.ConfidenceLower90 <= point.ConfidenceUpper90, "Lower 90% bound must be <= upper 90% bound.");
        }
    }

    [Fact]
    public async Task ExplainableAi_Should_Decompose_Attributions_And_Simulate_WhatIf()
    {
        // Act 1: XAI Explanation
        var xai = await _xaiService.ExplainForecastAsync("RETAIL_ECOMMERCE", 3);

        // Assert 1
        Assert.NotNull(xai);
        Assert.NotEmpty(xai.FeatureAttributions);
        Assert.NotEmpty(xai.StrategicInsights);
        Assert.True(xai.SensitivityGradients.Count > 0);

        // Act 2: Counterfactual What-If simulation (e.g. Brent oil shock +20%)
        var whatIf = await _xaiService.SimulateWhatIfAsync(new WhatIfScenarioRequestDto
        {
            BaseTargetEntityId = "RETAIL_ECOMMERCE",
            HorizonMonths = 3,
            ScenarioName = "Guncangan Lonjakan Minyak Mentah Global",
            VariableShocksPercentage = new Dictionary<string, double>
            {
                { "BRENT_OIL", 20.0 }
            }
        });

        // Assert 2
        Assert.NotNull(whatIf);
        Assert.NotEmpty(whatIf.ImpactChainTrace);
        Assert.False(string.IsNullOrWhiteSpace(whatIf.StrategicRecommendation));
    }

    [Fact]
    public async Task MultiAgentSystem_Should_Execute_Autonomous_Workflow_EndToEnd()
    {
        // Arrange
        var task = new MultiAgentTaskRequestDto
        {
            Query = "Bagaimana prospek harga CPO dan dampaknya terhadap daya beli sektor agribisnis?",
            TargetSectorOrCommodity = "CRUDE_PALM_OIL",
            HorizonMonths = 3,
            RequiresHumanSignoff = true
        };

        // Act
        var report = await _multiAgentSystem.ExecuteAutonomousWorkflowAsync(task);

        // Assert
        Assert.NotNull(report);
        Assert.NotEmpty(report.Title);
        Assert.NotEmpty(report.ExecutiveSummary);
        Assert.Equal(5, report.AgentWorkflowTrace.Count); // Orchestrator, Graph, Forecast, XAI, Human Gatekeeper
        Assert.True(report.AgentWorkflowTrace.All(t => t.Success));
        Assert.True(report.QualityFaithfulnessScore >= 0.90);
        Assert.NotEmpty(report.RecommendedActions);
    }
}
