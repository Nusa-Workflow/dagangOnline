using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using dagangOnline.Domain.Economic;

namespace dagangOnline.Application.Services.Economic;

public class EconomicGraphEngine
{
    private readonly ConcurrentDictionary<string, EconomicGraphNode> _nodes = new();
    private readonly List<EconomicGraphEdge> _edges = new();
    private readonly object _syncLock = new();

    public EconomicGraphEngine()
    {
        InitializeDefaultGraph();
    }

    private void InitializeDefaultGraph()
    {
        // 1. Macroeconomic Nodes
        AddNode(new EconomicGraphNode
        {
            Id = "INFLATION_CPI",
            Label = "Indeks Harga Konsumen (IHK) / Inflasi",
            Category = IndicatorCategory.Macroeconomic,
            CurrentValue = 2.85, // 2.85% YoY
            Unit = "% YoY",
            Volatility30d = 0.12,
            SentimentScore = -0.15,
            MomentumIndex = 0.04
        });

        AddNode(new EconomicGraphNode
        {
            Id = "BI_RATE",
            Label = "Bank Indonesia Policy Rate (BI-Rate)",
            Category = IndicatorCategory.Macroeconomic,
            CurrentValue = 6.00,
            Unit = "%",
            Volatility30d = 0.05,
            SentimentScore = 0.10,
            MomentumIndex = -0.02
        });

        AddNode(new EconomicGraphNode
        {
            Id = "USD_IDR",
            Label = "Nilai Tukar Rupiah (USD/IDR)",
            Category = IndicatorCategory.Macroeconomic,
            CurrentValue = 15850.0,
            Unit = "IDR",
            Volatility30d = 0.85,
            SentimentScore = -0.20,
            MomentumIndex = 0.06
        });

        AddNode(new EconomicGraphNode
        {
            Id = "GDP_GROWTH",
            Label = "Pertumbuhan Ekonomi Nasional (PDB)",
            Category = IndicatorCategory.Macroeconomic,
            CurrentValue = 5.08,
            Unit = "% YoY",
            Volatility30d = 0.08,
            SentimentScore = 0.35,
            MomentumIndex = 0.03
        });

        // 2. Commodity Nodes
        AddNode(new EconomicGraphNode
        {
            Id = "CRUDE_PALM_OIL",
            Label = "Kelapa Sawit (CPO Rotterdam/KPBN)",
            Category = IndicatorCategory.Commodity,
            CurrentValue = 12450.0,
            Unit = "IDR/kg",
            Volatility30d = 1.45,
            SentimentScore = 0.25,
            MomentumIndex = 0.08
        });

        AddNode(new EconomicGraphNode
        {
            Id = "BRENT_OIL",
            Label = "Minyak Mentah Dunia (Brent Crude)",
            Category = IndicatorCategory.Commodity,
            CurrentValue = 78.50,
            Unit = "USD/barrel",
            Volatility30d = 2.10,
            SentimentScore = -0.10,
            MomentumIndex = 0.05
        });

        AddNode(new EconomicGraphNode
        {
            Id = "RICE_PRICE",
            Label = "Beras Medium / Pangan Pokok Nasional",
            Category = IndicatorCategory.Commodity,
            CurrentValue = 14500.0,
            Unit = "IDR/kg",
            Volatility30d = 0.70,
            SentimentScore = -0.40,
            MomentumIndex = 0.03
        });

        AddNode(new EconomicGraphNode
        {
            Id = "COAL_PRICE",
            Label = "Batubara Newcastle",
            Category = IndicatorCategory.Commodity,
            CurrentValue = 135.0,
            Unit = "USD/ton",
            Volatility30d = 1.80,
            SentimentScore = 0.05,
            MomentumIndex = -0.04
        });

        // 3. Industry Sectors & Platform Microeconomics
        AddNode(new EconomicGraphNode
        {
            Id = "RETAIL_ECOMMERCE",
            Label = "Sektor Ritel & E-Commerce UMKM",
            Category = IndicatorCategory.IndustrySector,
            CurrentValue = 118.4, // Index base 100
            Unit = "Index Poin",
            Volatility30d = 0.65,
            SentimentScore = 0.30,
            MomentumIndex = 0.07
        });

        AddNode(new EconomicGraphNode
        {
            Id = "LOGISTICS_TRANSPORT",
            Label = "Sektor Logistik & Distribusi Rantai Pasok",
            Category = IndicatorCategory.IndustrySector,
            CurrentValue = 122.0,
            Unit = "Index Poin",
            Volatility30d = 0.90,
            SentimentScore = -0.05,
            MomentumIndex = 0.02
        });

        AddNode(new EconomicGraphNode
        {
            Id = "AGRICULTURE_FMCG",
            Label = "Sektor Agribisnis & FMCG Konsumsi",
            Category = IndicatorCategory.IndustrySector,
            CurrentValue = 112.5,
            Unit = "Index Poin",
            Volatility30d = 0.45,
            SentimentScore = 0.15,
            MomentumIndex = 0.04
        });

        AddNode(new EconomicGraphNode
        {
            Id = "SME_PROFIT_MARGIN",
            Label = "Rata-rata Margin Keuntungan Merchant UMKM",
            Category = IndicatorCategory.PlatformMicroeconomic,
            CurrentValue = 18.2,
            Unit = "%",
            Volatility30d = 0.50,
            SentimentScore = 0.10,
            MomentumIndex = -0.01
        });

        // 4. Graph Causal & Transmission Edges
        AddEdge("BRENT_OIL", "LOGISTICS_TRANSPORT", "DRIVES_COST", 0.78, 1, "Kenaikan harga minyak mentah meningkatkan biaya operasional armada logistik (lag 1 bulan).");
        AddEdge("LOGISTICS_TRANSPORT", "RETAIL_ECOMMERCE", "SUPPLY_TRANSMISSION", 0.65, 1, "Biaya logistik langsung mempengaruhi ongkos kirim dan margin ritel.");
        AddEdge("BRENT_OIL", "INFLATION_CPI", "TRANSMITS_INFLATION", 0.55, 2, "Transmisi harga energi global ke inflasi harga diatur pemerintah (lag 2 bulan).");
        AddEdge("CRUDE_PALM_OIL", "AGRICULTURE_FMCG", "REVENUE_DRIVER", 0.82, 0, "Kenaikan harga CPO mendorong daya beli petani kelapa sawit dan pendapatan FMCG daerah.");
        AddEdge("RICE_PRICE", "INFLATION_CPI", "CORE_BASKET_WEIGHT", 0.72, 0, "Beras memiliki bobot volatil food signifikan dalam perhitungan inflasi BPS.");
        AddEdge("USD_IDR", "INFLATION_CPI", "IMPORTED_INFLATION", 0.60, 2, "Pelemahan rupiah menaikkan biaya bahan baku impor (imported inflation).");
        AddEdge("BI_RATE", "USD_IDR", "POLICY_STABILIZER", -0.45, 1, "Kenaikan suku bunga BI-Rate mempersempit yield gap dan menopang stabilitas rupiah.");
        AddEdge("BI_RATE", "RETAIL_ECOMMERCE", "CREDIT_COST_PRESSURE", -0.38, 3, "Suku bunga tinggi memperketat likuiditas modal kerja dan kredit konsumen.");
        AddEdge("INFLATION_CPI", "SME_PROFIT_MARGIN", "MARGIN_COMPRESSION", -0.62, 1, "Tingginya inflasi menekan daya beli riil dan mempersempit margin pedagang.");
        AddEdge("RETAIL_ECOMMERCE", "SME_PROFIT_MARGIN", "SALES_VELOCITY_LEVER", 0.75, 0, "Pertumbuhan volume transaksi e-commerce menaikkan omzet dan laba bersih merchant.");
        AddEdge("GDP_GROWTH", "RETAIL_ECOMMERCE", "DEMAND_EXPANSION", 0.68, 1, "Pertumbuhan PDB berkorelasi positif kuat dengan belanja konsumsi rumah tangga.");
    }

    public void AddNode(EconomicGraphNode node)
    {
        _nodes[node.Id] = node;
    }

    public void AddEdge(string sourceId, string targetId, string relation, double weight, int lagMonths, string description)
    {
        lock (_syncLock)
        {
            _edges.Add(new EconomicGraphEdge
            {
                SourceId = sourceId,
                TargetId = targetId,
                RelationType = relation,
                Weight = weight,
                LeadLagMonths = lagMonths,
                Description = description
            });
        }
    }

    public List<EconomicGraphNode> GetAllNodes() => _nodes.Values.ToList();

    public List<EconomicGraphEdge> GetAllEdges()
    {
        lock (_syncLock)
        {
            return _edges.ToList();
        }
    }

    public EconomicGraphNode? GetNode(string id)
    {
        _nodes.TryGetValue(id, out var node);
        return node;
    }

    public GraphFeatureVectorDto ExtractFeatureVector(string targetEntityId, int horizonMonths, Dictionary<string, double>? overrides = null)
    {
        var target = GetNode(targetEntityId) ?? new EconomicGraphNode
        {
            Id = targetEntityId,
            Label = targetEntityId,
            Category = IndicatorCategory.IndustrySector,
            CurrentValue = 100.0,
            Unit = "Index",
            Volatility30d = 0.5,
            SentimentScore = 0.0,
            MomentumIndex = 0.0
        };

        var allEdges = GetAllEdges();
        var incomingEdges = allEdges.Where(e => e.TargetId == targetEntityId).ToList();
        var outgoingEdges = allEdges.Where(e => e.SourceId == targetEntityId).ToList();

        double inDegree = incomingEdges.Count;
        double outDegree = outgoingEdges.Count;

        var keyDrivers = new List<ConnectedDriverFeature>();
        double weightedTransmissionSum = 0.0;
        double totalWeight = 0.0;
        double sentimentWeightedSum = 0.0;
        double commodityPressureSum = 0.0;
        double velocitySum = 0.0;

        foreach (var edge in incomingEdges)
        {
            var sourceNode = GetNode(edge.SourceId);
            if (sourceNode == null) continue;

            double sourceVal = sourceNode.CurrentValue;
            if (overrides != null && overrides.TryGetValue(edge.SourceId, out var overrideVal))
            {
                sourceVal = overrideVal;
            }

            double shockContribution = (sourceVal * edge.Weight) * (1.0 + sourceNode.MomentumIndex);
            double normalizedContribution = Math.Round(edge.Weight * (1.0 + (sourceNode.SentimentScore * 0.2)), 4);

            weightedTransmissionSum += shockContribution;
            totalWeight += Math.Abs(edge.Weight);
            sentimentWeightedSum += sourceNode.SentimentScore * Math.Abs(edge.Weight);
            velocitySum += edge.LeadLagMonths * Math.Abs(edge.Weight);

            if (sourceNode.Category == IndicatorCategory.Commodity)
            {
                commodityPressureSum += edge.Weight * (1.0 + sourceNode.Volatility30d);
            }

            keyDrivers.Add(new ConnectedDriverFeature
            {
                SourceNodeId = sourceNode.Id,
                SourceNodeName = sourceNode.Label,
                Relation = edge.RelationType,
                CurrentValue = sourceVal,
                EdgeWeight = edge.Weight,
                ContributionScore = normalizedContribution,
                LagMonths = edge.LeadLagMonths
            });
        }

        double aggregateTransmissionScore = totalWeight > 0 ? Math.Round(weightedTransmissionSum / totalWeight, 4) : 0.0;
        double macroSentimentBias = totalWeight > 0 ? Math.Round(sentimentWeightedSum / totalWeight, 4) : target.SentimentScore;
        double transmissionVelocity = totalWeight > 0 ? Math.Round(velocitySum / totalWeight, 2) : 1.0;

        // Construct raw feature dictionary for ML/DL models
        var rawFeatures = new Dictionary<string, double>
        {
            { "in_degree", inDegree },
            { "out_degree", outDegree },
            { "target_baseline_value", target.CurrentValue },
            { "target_volatility", target.Volatility30d },
            { "target_momentum", target.MomentumIndex },
            { "target_sentiment", target.SentimentScore },
            { "transmission_score", aggregateTransmissionScore },
            { "macro_sentiment_bias", macroSentimentBias },
            { "commodity_cost_pressure", Math.Round(commodityPressureSum, 4) },
            { "lead_lag_velocity", transmissionVelocity },
            { "horizon_months", (double)horizonMonths }
        };

        // Add explicit driver values into feature vector
        foreach (var kd in keyDrivers)
        {
            rawFeatures[$"driver_{kd.SourceNodeId.ToLowerInvariant()}_val"] = kd.CurrentValue;
            rawFeatures[$"driver_{kd.SourceNodeId.ToLowerInvariant()}_weight"] = kd.EdgeWeight;
        }

        return new GraphFeatureVectorDto
        {
            TargetEntityId = target.Id,
            TargetEntityName = target.Label,
            Category = target.Category,
            InDegreeCentrality = inDegree,
            OutDegreeCentrality = outDegree,
            AggregateShockExposure = Math.Round(totalWeight * target.Volatility30d, 4),
            WeightedTransmissionScore = aggregateTransmissionScore,
            MacroSentimentBias = macroSentimentBias,
            CommodityCostPressure = Math.Round(commodityPressureSum, 4),
            LeadLagTransmissionVelocity = transmissionVelocity,
            KeyDrivers = keyDrivers.OrderByDescending(k => Math.Abs(k.ContributionScore)).ToList(),
            RawFeatureVector = rawFeatures,
            ExtractedAt = DateTime.UtcNow
        };
    }
}
