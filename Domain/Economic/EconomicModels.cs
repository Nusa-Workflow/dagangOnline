using System;
using System.Collections.Generic;

namespace dagangOnline.Domain.Economic;

public enum IndicatorCategory
{
    Macroeconomic,
    Commodity,
    IndustrySector,
    PolicyAndSentiment,
    PlatformMicroeconomic
}

public enum ForecastTrend
{
    StronglyBearish,
    Bearish,
    Neutral,
    Bullish,
    StronglyBullish
}

public class EconomicGraphNode
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public IndicatorCategory Category { get; set; }
    public double CurrentValue { get; set; }
    public string Unit { get; set; } = string.Empty;
    public double Volatility30d { get; set; }
    public double SentimentScore { get; set; } // -1.0 to 1.0
    public double MomentumIndex { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public Dictionary<string, string> Metadata { get; set; } = new();
}

public class EconomicGraphEdge
{
    public string SourceId { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public string RelationType { get; set; } = string.Empty; // e.g., "IMPACTS", "DRIVES_COST", "TRANSMITS_INFLATION", "LEADS_BY_MONTHS"
    public double Weight { get; set; } // Correlation or transmission strength (0.0 - 1.0)
    public int LeadLagMonths { get; set; } // Time lag in months
    public string Description { get; set; } = string.Empty;
}

public class GraphFeatureVectorDto
{
    public string TargetEntityId { get; set; } = string.Empty;
    public string TargetEntityName { get; set; } = string.Empty;
    public IndicatorCategory Category { get; set; }
    public double InDegreeCentrality { get; set; }
    public double OutDegreeCentrality { get; set; }
    public double AggregateShockExposure { get; set; }
    public double WeightedTransmissionScore { get; set; }
    public double MacroSentimentBias { get; set; }
    public double CommodityCostPressure { get; set; }
    public double LeadLagTransmissionVelocity { get; set; }
    public List<ConnectedDriverFeature> KeyDrivers { get; set; } = new();
    public Dictionary<string, double> RawFeatureVector { get; set; } = new();
    public DateTime ExtractedAt { get; set; } = DateTime.UtcNow;
}

public class ConnectedDriverFeature
{
    public string SourceNodeId { get; set; } = string.Empty;
    public string SourceNodeName { get; set; } = string.Empty;
    public string Relation { get; set; } = string.Empty;
    public double CurrentValue { get; set; }
    public double EdgeWeight { get; set; }
    public double ContributionScore { get; set; }
    public int LagMonths { get; set; }
}

public class ForecastRequestDto
{
    public string TargetEntityId { get; set; } = string.Empty; // e.g. "INFLATION_CPI", "CPO_PRICE", "RETAIL_SECTOR_GROWTH"
    public int ForecastHorizonMonths { get; set; } = 3; // 1, 3, 6, 12 months
    public bool IncludeGraphFeatures { get; set; } = true;
    public Dictionary<string, double>? CounterfactualOverrides { get; set; } // For What-If scenario simulations
}

public class ForecastPoint
{
    public int HorizonMonth { get; set; }
    public DateTime ForecastDate { get; set; }
    public double PredictedValue { get; set; }
    public double ConfidenceLower95 { get; set; }
    public double ConfidenceUpper95 { get; set; }
    public double ConfidenceLower90 { get; set; }
    public double ConfidenceUpper90 { get; set; }
}

public class ForecastResultDto
{
    public string TargetEntityId { get; set; } = string.Empty;
    public string TargetEntityName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public double BaselineValue { get; set; }
    public ForecastTrend OverallTrend { get; set; }
    public double ExpectedPercentageChange { get; set; }
    public List<ForecastPoint> Predictions { get; set; } = new();
    public double ModelConfidence { get; set; }
    public string ModelArchitecture { get; set; } = "Graph-Informed Deep Econometric Ensemble (VAR + Latent Transformer)";
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

public class FeatureAttribution
{
    public string FeatureName { get; set; } = string.Empty;
    public double ImportanceScore { get; set; } // 0.0 to 1.0 (relative weight)
    public double DirectionalImpact { get; set; } // Negative or positive effect on forecast
    public string Explanation { get; set; } = string.Empty;
}

public class XaiExplanationDto
{
    public string TargetEntityId { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public List<FeatureAttribution> FeatureAttributions { get; set; } = new();
    public List<string> StrategicInsights { get; set; } = new();
    public List<string> RiskFactors { get; set; } = new();
    public Dictionary<string, double> SensitivityGradients { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

public class WhatIfScenarioRequestDto
{
    public string BaseTargetEntityId { get; set; } = string.Empty;
    public int HorizonMonths { get; set; } = 3;
    public string ScenarioName { get; set; } = string.Empty;
    public Dictionary<string, double> VariableShocksPercentage { get; set; } = new(); // e.g. {"CRUDE_OIL": 15.0, "BI_RATE": 0.5}
}

public class WhatIfScenarioResultDto
{
    public string ScenarioName { get; set; } = string.Empty;
    public string TargetEntityId { get; set; } = string.Empty;
    public double BaselineForecast { get; set; }
    public double ShockedForecast { get; set; }
    public double VariancePercentage { get; set; }
    public List<string> ImpactChainTrace { get; set; } = new();
    public string StrategicRecommendation { get; set; } = string.Empty;
}

public class MultiAgentTaskRequestDto
{
    public string Query { get; set; } = string.Empty;
    public string TargetSectorOrCommodity { get; set; } = string.Empty;
    public int HorizonMonths { get; set; } = 3;
    public bool RequiresHumanSignoff { get; set; } = false;
}

public class AgentExecutionStep
{
    public string AgentName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string OutputSummary { get; set; } = string.Empty;
    public double ExecutionDurationMs { get; set; }
    public bool Success { get; set; } = true;
}

public class MultiAgentAnalysisReportDto
{
    public Guid ReportId { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string ExecutiveSummary { get; set; } = string.Empty;
    public GraphFeatureVectorDto GraphFeatures { get; set; } = new();
    public ForecastResultDto Forecast { get; set; } = new();
    public XaiExplanationDto Explanation { get; set; } = new();
    public List<AgentExecutionStep> AgentWorkflowTrace { get; set; } = new();
    public List<string> RecommendedActions { get; set; } = new();
    public double QualityFaithfulnessScore { get; set; }
    public bool IsReviewedByHuman { get; set; }
    public string? ReviewerNotes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
