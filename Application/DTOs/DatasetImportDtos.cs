using System;
using System.Collections.Generic;

namespace dagangOnline.Application.DTOs;

public enum DatasetTargetType
{
    AutoDetect,
    KnowledgeRAG,
    EconomicIndicator,
    ProductCatalog
}

public class DatasetImportOptions
{
    public DatasetTargetType TargetType { get; set; } = DatasetTargetType.AutoDetect;
    public string DefaultCategory { get; set; } = "General";
    public string DefaultLanguage { get; set; } = "id-ID";
    public string AccessLevel { get; set; } = "Public";
    public bool OverwriteExisting { get; set; } = false;
    public int MaxRowsToProcess { get; set; } = 10000;
}

public class DatasetImportResultDto
{
    public bool Success { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string DetectedFormat { get; set; } = string.Empty; // .jsonl, .txt, .csv, .xlsx
    public DatasetTargetType TargetType { get; set; }
    public int TotalRecordsParsed { get; set; }
    public int ImportedCount { get; set; }
    public int SkippedCount { get; set; }
    public int ErrorCount { get; set; }
    public List<string> Messages { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}

public class ImportFileInfo
{
    public string FileName { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string Extension { get; set; } = string.Empty;
    public DateTime LastModified { get; set; }
    public bool IsSupported { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class SupportedFormatInfoDto
{
    public string Extension { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SampleRecord { get; set; } = string.Empty;
}
