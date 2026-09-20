using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using dagangOnline.Domain.Economic;

namespace dagangOnline.Application.Services.Economic;

public class ExplainableAiService
{
    private readonly EconomicGraphEngine _graphEngine;
    private readonly EconomicForecastingService _forecastingService;

    public ExplainableAiService(EconomicGraphEngine graphEngine, EconomicForecastingService forecastingService)
    {
        _graphEngine = graphEngine;
        _forecastingService = forecastingService;
    }

    public async Task<XaiExplanationDto> ExplainForecastAsync(string targetEntityId, int horizonMonths, CancellationToken cancellationToken = default)
    {
        var targetNode = _graphEngine.GetNode(targetEntityId);
        var graphFeatures = _graphEngine.ExtractFeatureVector(targetEntityId, horizonMonths);
        var forecast = await _forecastingService.GenerateForecastAsync(new ForecastRequestDto
        {
            TargetEntityId = targetEntityId,
            ForecastHorizonMonths = horizonMonths
        }, cancellationToken);

        var attributions = new List<FeatureAttribution>();
        var sensitivityGradients = new Dictionary<string, double>();
        var strategicInsights = new List<string>();
        var riskFactors = new List<string>();

        // 1. Calculate Feature Attributions from Graph Drivers & Topological Metrics
        double totalAbsContribution = graphFeatures.KeyDrivers.Sum(d => Math.Abs(d.ContributionScore)) + 0.5;

        foreach (var driver in graphFeatures.KeyDrivers)
        {
            double relativeImportance = Math.Round(Math.Abs(driver.ContributionScore) / totalAbsContribution, 3);
            double directionalImpact = driver.ContributionScore > 0 ? 1.0 : -1.0;

            attributions.Add(new FeatureAttribution
            {
                FeatureName = $"{driver.SourceNodeName} ({driver.Relation})",
                ImportanceScore = relativeImportance,
                DirectionalImpact = directionalImpact,
                Explanation = $"Node '{driver.SourceNodeName}' mentransmisikan tekanan via relasi {driver.Relation} dengan lag waktu {driver.LagMonths} bulan."
            });

            sensitivityGradients[driver.SourceNodeId] = Math.Round(driver.EdgeWeight * (1.0 / (1.0 + driver.LagMonths)), 3);
        }

        // Add topological momentum & volatility features
        attributions.Add(new FeatureAttribution
        {
            FeatureName = "Target Momentum & Volatilitas Pasar",
            ImportanceScore = Math.Round(0.25 / totalAbsContribution, 3),
            DirectionalImpact = targetNode != null && targetNode.MomentumIndex >= 0 ? 1.0 : -1.0,
            Explanation = "Inersia tren jangka pendek (30 hari terakhir) mempengaruhi lintasan baseline prediksi."
        });

        // 2. Generate Strategic Actionable Insights
        if (forecast.OverallTrend == ForecastTrend.Bullish || forecast.OverallTrend == ForecastTrend.StronglyBullish)
        {
            strategicInsights.Add($"Prospek {forecast.TargetEntityName} diproyeksikan ekspansif (+{forecast.ExpectedPercentageChange}% dalam {horizonMonths} bulan).");
            strategicInsights.Add("Rekomendasi Operasional: Mitra & pelaku usaha disarankan menambah kapasitas inventaris dan memperkuat rantai pasok sebelum eskalasi harga input.");
            strategicInsights.Add("Strategi Penetapan Harga: Lakukan penyesuaian margin berkala atau tawarkan paket bundling guna mempertahankan elastisitas permintaan konsumen.");
        }
        else if (forecast.OverallTrend == ForecastTrend.Bearish || forecast.OverallTrend == ForecastTrend.StronglyBearish)
        {
            strategicInsights.Add($"Tren {forecast.TargetEntityName} terindikasi mengalami kontraksi/penurunan ({forecast.ExpectedPercentageChange}% dalam {horizonMonths} bulan).");
            strategicInsights.Add("Rekomendasi Mitigasi Risiko: Prioritaskan efisiensi modal kerja, hindari penumpukan stok berlebih (lean inventory), dan diversifikasi saluran suplai lokal.");
            strategicInsights.Add("Dukungan Platform: Terapkan subsidi ongkir bersyarat atau program promosi bersama untuk menjaga daya saing merchant UMKM.");
        }
        else
        {
            strategicInsights.Add($"Indikator {forecast.TargetEntityName} stabil dalam koridor netral (perubahan {forecast.ExpectedPercentageChange}%).");
            strategicInsights.Add("Rekomendasi: Pantau perkembangan kebijakan suku bunga acuan dan harga komoditas global sebagai trigger pergeseran tren.");
        }

        // 3. Highlight Risk Factors based on shock exposure
        if (graphFeatures.CommodityCostPressure > 0.8)
        {
            riskFactors.Add("Sensitivitas tinggi terhadap volatilitas komoditas energi dan bahan pangan pokok.");
        }
        if (graphFeatures.AggregateShockExposure > 1.0)
        {
            riskFactors.Add("Kerapatan hubungan graf tinggi menyebabkan kerentanan terhadap efek rambatan (contagion effect) eksternal.");
        }
        if (graphFeatures.KeyDrivers.Any(k => k.LagMonths >= 2))
        {
            riskFactors.Add("Terdapat jeda transmisi (lag) 2 bulan ke atas, sinyal inflasi riil berpotensi baru terasa pada kuartal berikutnya.");
        }

        string summary = $"Model XAI mengidentifikasi {attributions.Count} fitur graf utama yang mempengaruhi {forecast.TargetEntityName}. Driver dengan bobot tertinggi adalah {attributions.FirstOrDefault()?.FeatureName ?? "Tren Historis"}.";

        return new XaiExplanationDto
        {
            TargetEntityId = targetEntityId,
            Summary = summary,
            FeatureAttributions = attributions.OrderByDescending(a => a.ImportanceScore).ToList(),
            StrategicInsights = strategicInsights,
            RiskFactors = riskFactors,
            SensitivityGradients = sensitivityGradients,
            GeneratedAt = DateTime.UtcNow
        };
    }

    public async Task<WhatIfScenarioResultDto> SimulateWhatIfAsync(WhatIfScenarioRequestDto request, CancellationToken cancellationToken = default)
    {
        // 1. Run baseline forecast
        var baseline = await _forecastingService.GenerateForecastAsync(new ForecastRequestDto
        {
            TargetEntityId = request.BaseTargetEntityId,
            ForecastHorizonMonths = request.HorizonMonths
        }, cancellationToken);

        // 2. Prepare shocked overrides
        var overrides = new Dictionary<string, double>();
        var impactTrace = new List<string>();

        foreach (var shock in request.VariableShocksPercentage)
        {
            var node = _graphEngine.GetNode(shock.Key);
            if (node != null)
            {
                double shockedValue = node.CurrentValue * (1.0 + (shock.Value / 100.0));
                overrides[shock.Key] = shockedValue;
                impactTrace.Add($"Simulasi guncangan: '{node.Label}' bergeser {shock.Value:+0.0;-0.0}% dari {node.CurrentValue:N1} ke {shockedValue:N1} {node.Unit}.");
            }
        }

        // 3. Run counterfactual forecast
        var shockedForecast = await _forecastingService.GenerateForecastAsync(new ForecastRequestDto
        {
            TargetEntityId = request.BaseTargetEntityId,
            ForecastHorizonMonths = request.HorizonMonths,
            CounterfactualOverrides = overrides
        }, cancellationToken);

        double baseFinal = baseline.Predictions.LastOrDefault()?.PredictedValue ?? baseline.BaselineValue;
        double shockedFinal = shockedForecast.Predictions.LastOrDefault()?.PredictedValue ?? shockedForecast.BaselineValue;
        double variancePct = baseFinal != 0 ? Math.Round(((shockedFinal - baseFinal) / baseFinal) * 100.0, 2) : 0.0;

        string recommendation = variancePct > 2.0
            ? "Skenario ini menimbulkan tekanan biaya signifikan (+ " + variancePct + "%). Diperlukan hedging harga atau penyesuaian bertahap."
            : variancePct < -2.0
                ? "Skenario ini memberikan relaksasi margin (+ ruang likuiditas " + Math.Abs(variancePct) + "%)."
                : "Dampak skenario relatif moderat terhadap proyeksi akhir (" + variancePct + "%).";

        return new WhatIfScenarioResultDto
        {
            ScenarioName = request.ScenarioName,
            TargetEntityId = request.BaseTargetEntityId,
            BaselineForecast = baseFinal,
            ShockedForecast = shockedFinal,
            VariancePercentage = variancePct,
            ImpactChainTrace = impactTrace,
            StrategicRecommendation = recommendation
        };
    }
}
