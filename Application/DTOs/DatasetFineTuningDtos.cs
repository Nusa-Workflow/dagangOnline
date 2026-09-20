using System;
using System.Collections.Generic;

namespace dagangOnline.Application.DTOs;

public class DatasetFineTuningReportDto
{
    public bool Success { get; set; } = true;
    public string Status { get; set; } = "Trained & Active";
    public DateTime TrainedAt { get; set; } = DateTime.UtcNow;
    public int TotalRecordsAbsorbed { get; set; }
    public int UmkmProfilesLearned { get; set; }
    public int ResearchInstructionsLearned { get; set; }
    public int MultilingualTasksLearned { get; set; }
    public int KnowledgeChunksIndexed { get; set; }
    public Dictionary<string, SectorEmpiricalSummaryDto> SectorEmpiricalProfiles { get; set; } = new();
    public List<string> LearnedHypotheses { get; set; } = new();
    public string SummaryMessage { get; set; } = "";
}

public class SectorEmpiricalSummaryDto
{
    public string SectorName { get; set; } = "";
    public int RecordCount { get; set; }
    public decimal AverageOmset { get; set; }
    public decimal AverageAset { get; set; }
    public decimal AverageLaba { get; set; }
    public double LegalitasRatio { get; set; }
    public string DominantMarketplace { get; set; } = "";
}
