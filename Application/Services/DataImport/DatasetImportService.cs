using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using dagangOnline.Application.DTOs;
using dagangOnline.Application.Interfaces;
using dagangOnline.Application.Services.Economic;
using dagangOnline.Data;
using dagangOnline.Domain;
using dagangOnline.Domain.Chat;
using dagangOnline.Domain.Economic;

namespace dagangOnline.Application.Services.DataImport;

public class DatasetImportService : IDatasetImportService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly EconomicGraphEngine _graphEngine;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<DatasetImportService> _logger;

    public DatasetImportService(
        ApplicationDbContext dbContext,
        EconomicGraphEngine graphEngine,
        IWebHostEnvironment env,
        ILogger<DatasetImportService> logger)
    {
        _dbContext = dbContext;
        _graphEngine = graphEngine;
        _env = env;
        _logger = logger;
    }

    public List<SupportedFormatInfoDto> GetSupportedFormats()
    {
        return new List<SupportedFormatInfoDto>
        {
            new SupportedFormatInfoDto
            {
                Extension = ".jsonl",
                Name = "JSON Lines Dataset",
                MimeType = "application/x-jsonlines",
                Description = "Satu JSON object per baris. Sangat fleksibel untuk RAG knowledge chunks, indikator graf ekonomi, atau data katalog produk.",
                SampleRecord = "{\"title\": \"Pedoman Logistik UMKM\", \"content\": \"Layanan COD tersedia di Pulau Jawa dan Bali.\", \"category\": \"Logistik\"}"
            },
            new SupportedFormatInfoDto
            {
                Extension = ".txt",
                Name = "Text Corpus Document",
                MimeType = "text/plain",
                Description = "File teks polos atau catatan kebijakan/SOP. Teks otomatis di-chunking per paragraf menjadi Knowledge Chunks untuk pencarian RAG AI.",
                SampleRecord = "BAB 1: KETENTUAN TRANSAKSI\r\nSemua transaksi mitra UMKM dilindungi jaminan pembayaran dagangOnline.\r\n\r\nBAB 2: LOGISTIK & PENGIRIMAN..."
            },
            new SupportedFormatInfoDto
            {
                Extension = ".csv",
                Name = "Comma-Separated Values",
                MimeType = "text/csv",
                Description = "File tabel berbasis koma atau titik-koma. Cocok untuk data katalog produk, histori harga komoditas, atau indikator makroekonomi.",
                SampleRecord = "Id,Label,Category,CurrentValue,Unit\nINFLATION_CPI,Inflasi IHK,Macroeconomic,2.85,% YoY"
            },
            new SupportedFormatInfoDto
            {
                Extension = ".xlsx",
                Name = "Microsoft Excel Spreadsheet",
                MimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                Description = "File spreadsheet Excel OpenXML (.xlsx). Sheet pertama akan dibaca secara otomatis sebagai baris data tabular.",
                SampleRecord = "[Sheet 1 Table Data: Kolom Header -> Baris Record]"
            },
            new SupportedFormatInfoDto
            {
                Extension = ".json",
                Name = "Standard JSON Document / Array",
                MimeType = "application/json",
                Description = "File dokumen JSON standar, baik berupa objek tunggal maupun array record dataset.",
                SampleRecord = "[{\"nama_usaha\": \"Toko Berkah\", \"omset\": 50000000}]"
            }
        };
    }

    public string GetImportsDirectoryPath(string? customPath = null)
    {
        if (!string.IsNullOrWhiteSpace(customPath))
        {
            return Path.IsPathRooted(customPath)
                ? customPath
                : Path.Combine(_env.ContentRootPath, customPath);
        }

        return Path.Combine(_env.ContentRootPath, "Data", "Imports");
    }

    public List<ImportFileInfo> GetAvailableImportFiles(string? directoryPath = null)
    {
        var targetDir = GetImportsDirectoryPath(directoryPath);
        var result = new List<ImportFileInfo>();

        if (!Directory.Exists(targetDir))
        {
            return result;
        }

        var supportedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".jsonl", ".jsonlines", ".txt", ".text", ".csv", ".xlsx", ".xls", ".json"
        };

        var dirInfo = new DirectoryInfo(targetDir);
        foreach (var file in dirInfo.GetFiles())
        {
            if (file.Name.Equals(".gitkeep", StringComparison.OrdinalIgnoreCase) ||
                file.Name.Equals("README.md", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var ext = file.Extension.ToLowerInvariant();
            var isSupported = supportedExtensions.Contains(ext);

            result.Add(new ImportFileInfo
            {
                FileName = file.Name,
                RelativePath = Path.Combine("Data", "Imports", file.Name),
                SizeBytes = file.Length,
                Extension = ext,
                LastModified = file.LastWriteTimeUtc,
                IsSupported = isSupported,
                Description = isSupported ? $"File {ext.ToUpperInvariant()}" : "Format tidak didukung"
            });
        }

        return result.OrderByDescending(f => f.LastModified).ToList();
    }

    public async Task<DatasetImportResultDto> ImportFromFileAsync(
        string filePath,
        DatasetImportOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            return new DatasetImportResultDto
            {
                Success = false,
                FileName = Path.GetFileName(filePath),
                Errors = { $"File '{filePath}' tidak ditemukan." }
            };
        }

        var fileName = Path.GetFileName(filePath);
        await using var stream = File.OpenRead(filePath);
        return await ImportStreamAsync(stream, fileName, options, cancellationToken);
    }

    public async Task<List<DatasetImportResultDto>> ScanAndImportDirectoryAsync(
        string? directoryPath = null,
        DatasetImportOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var targetDir = GetImportsDirectoryPath(directoryPath);
        var results = new List<DatasetImportResultDto>();

        if (!Directory.Exists(targetDir))
        {
            _logger.LogWarning("Direktori imports '{Dir}' tidak ditemukan.", targetDir);
            return results;
        }

        var files = GetAvailableImportFiles(targetDir).Where(f => f.IsSupported).ToList();
        foreach (var file in files)
        {
            var fullPath = Path.Combine(targetDir, file.FileName);
            var result = await ImportFromFileAsync(fullPath, options, cancellationToken);
            results.Add(result);
        }

        return results;
    }

    public async Task<DatasetImportResultDto> ImportStreamAsync(
        Stream stream,
        string fileName,
        DatasetImportOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new DatasetImportOptions();
        var ext = Path.GetExtension(fileName).ToLowerInvariant();

        var result = new DatasetImportResultDto
        {
            FileName = fileName,
            DetectedFormat = ext,
            TargetType = options.TargetType
        };

        try
        {
            switch (ext)
            {
                case ".jsonl":
                case ".jsonlines":
                    await ProcessJsonLinesAsync(stream, fileName, options, result, cancellationToken);
                    break;

                case ".json":
                    await ProcessJsonDocumentAsync(stream, fileName, options, result, cancellationToken);
                    break;

                case ".txt":
                case ".text":
                    await ProcessTextCorpusAsync(stream, fileName, options, result, cancellationToken);
                    break;

                case ".csv":
                    await ProcessCsvAsync(stream, fileName, options, result, cancellationToken);
                    break;

                case ".xlsx":
                case ".xls":
                    await ProcessExcelAsync(stream, fileName, options, result, cancellationToken);
                    break;

                default:
                    result.Success = false;
                    result.ErrorCount = 1;
                    result.Errors.Add($"Format ekstensi '{ext}' tidak didukung. Gunakan .jsonl, .txt, .csv, .xlsx, atau .json.");
                    return result;
            }

            result.Success = result.ErrorCount == 0 || result.ImportedCount > 0;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gagal mengimpor file dataset '{FileName}'", fileName);
            result.Success = false;
            result.ErrorCount++;
            result.Errors.Add($"Kesalahan sistem saat memproses file: {ex.Message}");
            return result;
        }
    }

    #region JSON Lines (.jsonl) Processor

    private async Task ProcessJsonLinesAsync(
        Stream stream,
        string fileName,
        DatasetImportOptions options,
        DatasetImportResultDto result,
        CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        string? line;
        int lineNumber = 0;

        var knowledgeChunksToInsert = new List<KnowledgeChunk>();
        var productsToInsert = new List<Product>();
        int nodesUpdatedCount = 0;

        // Document wrapper for RAG if knowledge items found
        KnowledgeDocument? currentDoc = null;

        while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
        {
            lineNumber++;
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#") || trimmed.StartsWith("//"))
            {
                continue;
            }

            result.TotalRecordsParsed++;
            if (result.TotalRecordsParsed > options.MaxRowsToProcess)
            {
                result.Messages.Add($"Batas maksimum pemrosesan {options.MaxRowsToProcess} baris tercapai.");
                break;
            }

            try
            {
                using var jsonDoc = JsonDocument.Parse(trimmed);
                var root = jsonDoc.RootElement;

                // Determine target type
                var targetType = options.TargetType;
                if (targetType == DatasetTargetType.AutoDetect)
                {
                    targetType = DetectTargetType(root);
                }

                switch (targetType)
                {
                    case DatasetTargetType.KnowledgeRAG:
                        if (currentDoc == null)
                        {
                            var title = Path.GetFileNameWithoutExtension(fileName);
                            if (root.TryGetProperty("documentTitle", out var docTitleProp))
                                title = docTitleProp.GetString() ?? title;

                            currentDoc = new KnowledgeDocument
                            {
                                Title = title,
                                Category = options.DefaultCategory,
                                Language = options.DefaultLanguage,
                                AccessLevel = options.AccessLevel,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            };
                            _dbContext.KnowledgeDocuments.Add(currentDoc);
                            await _dbContext.SaveChangesAsync(cancellationToken);
                        }

                        var content = ExtractContentFromJson(root);
                        if (!string.IsNullOrWhiteSpace(content))
                        {
                            var heading = root.TryGetProperty("heading", out var hProp) ? hProp.GetString() ?? "" : "";
                            var chunk = new KnowledgeChunk
                            {
                                KnowledgeDocumentId = currentDoc.Id,
                                Content = content,
                                SourceHeading = heading,
                                ChunkIndex = knowledgeChunksToInsert.Count,
                                TokenCount = EstimateTokenCount(content)
                            };
                            knowledgeChunksToInsert.Add(chunk);
                            result.ImportedCount++;
                        }
                        else
                        {
                            result.SkippedCount++;
                        }
                        break;

                    case DatasetTargetType.EconomicIndicator:
                        var updated = UpdateEconomicNodeFromJson(root);
                        if (updated)
                        {
                            nodesUpdatedCount++;
                            result.ImportedCount++;
                        }
                        else
                        {
                            result.SkippedCount++;
                        }
                        break;

                    case DatasetTargetType.ProductCatalog:
                        var product = ParseProductFromJson(root, options);
                        if (product != null)
                        {
                            productsToInsert.Add(product);
                            result.ImportedCount++;
                        }
                        else
                        {
                            result.SkippedCount++;
                        }
                        break;

                    default:
                        // Fallback generic knowledge chunk
                        var rawText = trimmed;
                        if (currentDoc == null)
                        {
                            currentDoc = new KnowledgeDocument
                            {
                                Title = Path.GetFileNameWithoutExtension(fileName),
                                Category = options.DefaultCategory,
                                Language = options.DefaultLanguage,
                                AccessLevel = options.AccessLevel
                            };
                            _dbContext.KnowledgeDocuments.Add(currentDoc);
                            await _dbContext.SaveChangesAsync(cancellationToken);
                        }
                        knowledgeChunksToInsert.Add(new KnowledgeChunk
                        {
                            KnowledgeDocumentId = currentDoc.Id,
                            Content = rawText,
                            ChunkIndex = knowledgeChunksToInsert.Count,
                            TokenCount = EstimateTokenCount(rawText)
                        });
                        result.ImportedCount++;
                        break;
                }
            }
            catch (JsonException jex)
            {
                result.ErrorCount++;
                result.Errors.Add($"Baris {lineNumber}: JSON tidak valid - {jex.Message}");
            }
        }

        // Save batch RAG chunks
        if (knowledgeChunksToInsert.Count > 0)
        {
            _dbContext.KnowledgeChunks.AddRange(knowledgeChunksToInsert);
            await _dbContext.SaveChangesAsync(cancellationToken);
            result.Messages.Add($"Berhasil menyimpan {knowledgeChunksToInsert.Count} Knowledge Chunks untuk RAG.");
        }

        // Save batch products
        if (productsToInsert.Count > 0)
        {
            _dbContext.Products.AddRange(productsToInsert);
            await _dbContext.SaveChangesAsync(cancellationToken);
            result.Messages.Add($"Berhasil menambahkan {productsToInsert.Count} produk ke katalog.");
        }

        if (nodesUpdatedCount > 0)
        {
            result.Messages.Add($"Berhasil memperbarui/menambahkan {nodesUpdatedCount} entitas node pada Economic Graph Engine.");
        }
    }

    private static DatasetTargetType DetectTargetType(JsonElement root)
    {
        if (root.TryGetProperty("content", out _) || root.TryGetProperty("prompt", out _) ||
            root.TryGetProperty("text", out _) || root.TryGetProperty("answer", out _))
        {
            return DatasetTargetType.KnowledgeRAG;
        }

        if (root.TryGetProperty("currentValue", out _) || root.TryGetProperty("volatility30d", out _) ||
            (root.TryGetProperty("id", out _) && root.TryGetProperty("unit", out _)))
        {
            return DatasetTargetType.EconomicIndicator;
        }

        if (root.TryGetProperty("price", out _) && (root.TryGetProperty("name", out _) || root.TryGetProperty("productName", out _)))
        {
            return DatasetTargetType.ProductCatalog;
        }

        return DatasetTargetType.KnowledgeRAG;
    }

    private static string ExtractContentFromJson(JsonElement root)
    {
        if (root.TryGetProperty("content", out var cProp) && cProp.ValueKind == JsonValueKind.String)
            return cProp.GetString() ?? "";

        if (root.TryGetProperty("text", out var tProp) && tProp.ValueKind == JsonValueKind.String)
            return tProp.GetString() ?? "";

        if (root.TryGetProperty("instruction", out var instProp) && instProp.ValueKind == JsonValueKind.String)
        {
            var name = root.TryGetProperty("name", out var nP) ? nP.GetString() : null;
            var cat = root.TryGetProperty("category", out var catP) ? catP.GetString() : null;
            var bm = root.TryGetProperty("base_model", out var bmP) ? bmP.GetString() : null;
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(name)) sb.AppendLine($"Riset: {name}");
            sb.AppendLine($"Instruksi: {instProp.GetString()}");
            if (!string.IsNullOrEmpty(cat)) sb.AppendLine($"Kategori: {cat}");
            if (!string.IsNullOrEmpty(bm)) sb.AppendLine($"Model Basis: {bm}");
            return sb.ToString().Trim();
        }

        if (root.TryGetProperty("nama_usaha", out var nuProp))
        {
            var jenis = root.TryGetProperty("jenis_usaha", out var juP) ? juP.GetString() : "";
            var omset = root.TryGetProperty("omset", out var oP) ? oP.ToString() : "";
            var aset = root.TryGetProperty("aset", out var aP) ? aP.ToString() : "";
            var legal = root.TryGetProperty("status_legalitas", out var lP) ? lP.GetString() : "";
            return $"Nama Usaha: {nuProp.GetString()} | Bidang: {jenis} | Omset: Rp {omset} | Aset: Rp {aset} | Legalitas: {legal}";
        }

        if (root.TryGetProperty("prompt", out var pProp) && root.TryGetProperty("response", out var rProp))
        {
            return $"Tanya: {pProp.GetString()}\nJawab: {rProp.GetString()}";
        }

        if (root.TryGetProperty("question", out var qProp) && root.TryGetProperty("answer", out var aProp))
        {
            return $"Pertanyaan: {qProp.GetString()}\nJawaban: {aProp.GetString()}";
        }

        return root.ToString();
    }

    private async Task ProcessJsonDocumentAsync(
        Stream stream,
        string fileName,
        DatasetImportOptions options,
        DatasetImportResultDto result,
        CancellationToken cancellationToken)
    {
        using var jsonDoc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = jsonDoc.RootElement;

        if (root.ValueKind == JsonValueKind.Array)
        {
            var knowledgeChunksToInsert = new List<KnowledgeChunk>();
            KnowledgeDocument? doc = null;

            foreach (var item in root.EnumerateArray())
            {
                result.TotalRecordsParsed++;
                if (result.TotalRecordsParsed > options.MaxRowsToProcess)
                {
                    result.Messages.Add($"Batas maksimum {options.MaxRowsToProcess} baris tercapai.");
                    break;
                }

                if (doc == null)
                {
                    doc = new KnowledgeDocument
                    {
                        Title = Path.GetFileNameWithoutExtension(fileName),
                        Category = options.DefaultCategory,
                        Language = options.DefaultLanguage,
                        AccessLevel = options.AccessLevel,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _dbContext.KnowledgeDocuments.Add(doc);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }

                var content = ExtractContentFromJson(item);
                if (!string.IsNullOrWhiteSpace(content))
                {
                    var heading = item.TryGetProperty("nama_usaha", out var nuP) ? nuP.GetString() ?? "" :
                                  item.TryGetProperty("name", out var nP) ? nP.GetString() ?? "" : "";
                    knowledgeChunksToInsert.Add(new KnowledgeChunk
                    {
                        KnowledgeDocumentId = doc.Id,
                        Content = content,
                        SourceHeading = heading,
                        ChunkIndex = knowledgeChunksToInsert.Count,
                        TokenCount = EstimateTokenCount(content)
                    });
                    result.ImportedCount++;
                }
            }

            if (knowledgeChunksToInsert.Count > 0)
            {
                _dbContext.KnowledgeChunks.AddRange(knowledgeChunksToInsert);
                await _dbContext.SaveChangesAsync(cancellationToken);
                result.Messages.Add($"Berhasil mengimpor {knowledgeChunksToInsert.Count} record dari file JSON ke Knowledge Base RAG.");
            }
        }
        else
        {
            // Single JSON object
            var doc = new KnowledgeDocument
            {
                Title = Path.GetFileNameWithoutExtension(fileName),
                Category = options.DefaultCategory,
                Language = options.DefaultLanguage,
                AccessLevel = options.AccessLevel,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _dbContext.KnowledgeDocuments.Add(doc);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var content = root.ToString();
            _dbContext.KnowledgeChunks.Add(new KnowledgeChunk
            {
                KnowledgeDocumentId = doc.Id,
                Content = content,
                ChunkIndex = 0,
                TokenCount = EstimateTokenCount(content)
            });
            await _dbContext.SaveChangesAsync(cancellationToken);
            result.TotalRecordsParsed = 1;
            result.ImportedCount = 1;
            result.Messages.Add($"Berhasil mengimpor dokumen konfigurasi/baseline JSON ke Knowledge Base RAG.");
        }
    }

    private bool UpdateEconomicNodeFromJson(JsonElement root)
    {
        var id = root.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
        if (string.IsNullOrWhiteSpace(id)) return false;

        var label = root.TryGetProperty("label", out var lProp) ? lProp.GetString() ?? id : id;
        var unit = root.TryGetProperty("unit", out var uProp) ? uProp.GetString() ?? "" : "";
        double val = root.TryGetProperty("currentValue", out var vProp) && vProp.TryGetDouble(out var dVal) ? dVal : 0;
        double vol = root.TryGetProperty("volatility30d", out var volProp) && volProp.TryGetDouble(out var dVol) ? dVol : 0.1;
        double sent = root.TryGetProperty("sentimentScore", out var sProp) && sProp.TryGetDouble(out var dSent) ? dSent : 0;

        var cat = IndicatorCategory.Macroeconomic;
        if (root.TryGetProperty("category", out var cProp) &&
            Enum.TryParse<IndicatorCategory>(cProp.GetString(), true, out var parsedCat))
        {
            cat = parsedCat;
        }

        var existing = _graphEngine.GetNode(id);
        if (existing != null)
        {
            existing.Label = label;
            existing.CurrentValue = val;
            existing.Unit = string.IsNullOrWhiteSpace(unit) ? existing.Unit : unit;
            existing.Volatility30d = vol;
            existing.SentimentScore = sent;
            existing.LastUpdated = DateTime.UtcNow;
        }
        else
        {
            _graphEngine.AddNode(new EconomicGraphNode
            {
                Id = id,
                Label = label,
                Category = cat,
                CurrentValue = val,
                Unit = unit,
                Volatility30d = vol,
                SentimentScore = sent,
                LastUpdated = DateTime.UtcNow
            });
        }

        return true;
    }

    private static Product? ParseProductFromJson(JsonElement root, DatasetImportOptions options)
    {
        var name = root.TryGetProperty("name", out var nProp) ? nProp.GetString() :
                   root.TryGetProperty("productName", out var pnProp) ? pnProp.GetString() : null;

        if (string.IsNullOrWhiteSpace(name)) return null;

        decimal price = 0;
        if (root.TryGetProperty("price", out var prProp))
        {
            if (prProp.ValueKind == JsonValueKind.Number)
                price = prProp.GetDecimal();
            else if (decimal.TryParse(prProp.GetString(), out var parsedPrice))
                price = parsedPrice;
        }

        var desc = root.TryGetProperty("description", out var dProp) ? dProp.GetString() ?? "" : "";
        var catStr = root.TryGetProperty("category", out var catProp) ? catProp.GetString() : options.DefaultCategory;

        var category = ProductCategory.General;
        if (!string.IsNullOrWhiteSpace(catStr) && Enum.TryParse<ProductCategory>(catStr, true, out var parsedCategory))
        {
            category = parsedCategory;
        }

        var slug = name.ToLowerInvariant().Replace(" ", "-");

        return new Product
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = slug,
            Summary = desc.Length > 150 ? desc.Substring(0, 147) + "..." : desc,
            Description = desc,
            Price = price,
            Category = category,
            Status = PublicationStatus.Published,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    #endregion

    #region TXT Corpus Processor

    private async Task ProcessTextCorpusAsync(
        Stream stream,
        string fileName,
        DatasetImportOptions options,
        DatasetImportResultDto result,
        CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var fullText = await reader.ReadToEndAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(fullText))
        {
            result.Messages.Add("File teks kosong.");
            result.SkippedCount++;
            return;
        }

        var title = Path.GetFileNameWithoutExtension(fileName);
        // Normalize line breaks
        var normalized = fullText.Replace("\r\n", "\n").Replace("\r", "\n");

        // Extract title from first line if starts with # or title keyword
        var lines = normalized.Split('\n');
        if (lines.Length > 0 && lines[0].StartsWith("# "))
        {
            title = lines[0].TrimStart('#', ' ').Trim();
        }

        var doc = new KnowledgeDocument
        {
            Title = title,
            Category = options.DefaultCategory,
            Language = options.DefaultLanguage,
            AccessLevel = options.AccessLevel,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.KnowledgeDocuments.Add(doc);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Split by double newline (paragraphs) or markdown headings (##, ###)
        var paragraphs = Regex.Split(normalized, @"\n{2,}")
            .Select(p => p.Trim())
            .Where(p => !string.IsNullOrWhiteSpace(p) && p.Length > 15)
            .ToList();

        if (paragraphs.Count == 0)
        {
            // Fallback: split by line chunks if text doesn't have double newlines
            paragraphs = normalized.Split('\n')
                .Select(p => p.Trim())
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToList();
        }

        var chunksToInsert = new List<KnowledgeChunk>();
        string currentHeading = "";

        for (int i = 0; i < paragraphs.Count; i++)
        {
            if (i >= options.MaxRowsToProcess)
            {
                result.Messages.Add($"Batas maksimum {options.MaxRowsToProcess} paragraf/chunks tercapai.");
                break;
            }

            var para = paragraphs[i];
            result.TotalRecordsParsed++;

            // If paragraph starts with heading marker
            if (para.StartsWith("#") || para.StartsWith("BAB ") || para.StartsWith("PASAL "))
            {
                var firstLine = para.Split('\n')[0].TrimStart('#', ' ').Trim();
                if (firstLine.Length <= 100)
                {
                    currentHeading = firstLine;
                }
            }

            var chunk = new KnowledgeChunk
            {
                KnowledgeDocumentId = doc.Id,
                Content = para,
                SourceHeading = currentHeading,
                ChunkIndex = i,
                TokenCount = EstimateTokenCount(para)
            };

            chunksToInsert.Add(chunk);
            result.ImportedCount++;
        }

        if (chunksToInsert.Count > 0)
        {
            _dbContext.KnowledgeChunks.AddRange(chunksToInsert);
            await _dbContext.SaveChangesAsync(cancellationToken);
            result.Messages.Add($"Dokumen '{title}' berhasil di-chunking menjadi {chunksToInsert.Count} Knowledge Chunks untuk RAG.");
        }
    }

    #endregion

    #region CSV Processor

    private async Task ProcessCsvAsync(
        Stream stream,
        string fileName,
        DatasetImportOptions options,
        DatasetImportResultDto result,
        CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var headerLine = await reader.ReadLineAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(headerLine))
        {
            result.Messages.Add("File CSV kosong atau header tidak ditemukan.");
            return;
        }

        char separator = headerLine.Contains(';') ? ';' : ',';
        var headers = ParseCsvLine(headerLine, separator).Select(h => h.Trim().ToLowerInvariant()).ToList();

        var title = Path.GetFileNameWithoutExtension(fileName);
        KnowledgeDocument? doc = null;
        var chunksToInsert = new List<KnowledgeChunk>();
        var productsToInsert = new List<Product>();

        string? line;
        int lineNumber = 1;

        while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line)) continue;

            result.TotalRecordsParsed++;
            if (result.TotalRecordsParsed > options.MaxRowsToProcess)
            {
                result.Messages.Add($"Batas maksimum pemrosesan CSV {options.MaxRowsToProcess} baris tercapai.");
                break;
            }

            var values = ParseCsvLine(line, separator);
            if (values.Count == 0) continue;

            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < Math.Min(headers.Count, values.Count); i++)
            {
                row[headers[i]] = values[i];
            }

            // Detect or apply target type
            if (options.TargetType == DatasetTargetType.EconomicIndicator ||
                (options.TargetType == DatasetTargetType.AutoDetect && (row.ContainsKey("currentvalue") || row.ContainsKey("volatility30d"))))
            {
                if (row.TryGetValue("id", out var id) && !string.IsNullOrWhiteSpace(id))
                {
                    row.TryGetValue("label", out var label);
                    row.TryGetValue("unit", out var unit);
                    double.TryParse(row.GetValueOrDefault("currentvalue", "0"), out var val);
                    double.TryParse(row.GetValueOrDefault("volatility30d", "0.1"), out var vol);
                    double.TryParse(row.GetValueOrDefault("sentimentscore", "0"), out var sent);

                    _graphEngine.AddNode(new EconomicGraphNode
                    {
                        Id = id,
                        Label = string.IsNullOrWhiteSpace(label) ? id : label,
                        Unit = unit ?? "",
                        CurrentValue = val,
                        Volatility30d = vol,
                        SentimentScore = sent,
                        LastUpdated = DateTime.UtcNow
                    });
                    result.ImportedCount++;
                    continue;
                }
            }

            if (options.TargetType == DatasetTargetType.ProductCatalog ||
                (options.TargetType == DatasetTargetType.AutoDetect && row.ContainsKey("price") && (row.ContainsKey("name") || row.ContainsKey("productname"))))
            {
                var name = row.GetValueOrDefault("name") ?? row.GetValueOrDefault("productname") ?? "";
                if (!string.IsNullOrWhiteSpace(name))
                {
                    decimal.TryParse(row.GetValueOrDefault("price", "0"), out var price);
                    int.TryParse(row.GetValueOrDefault("stock", "10"), out var stock);

                    var desc = row.GetValueOrDefault("description", "");
                    productsToInsert.Add(new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = name,
                        Slug = name.ToLowerInvariant().Replace(" ", "-"),
                        Summary = desc.Length > 150 ? desc.Substring(0, 147) + "..." : desc,
                        Description = desc,
                        Price = price,
                        Category = ProductCategory.General,
                        Status = PublicationStatus.Published,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                    result.ImportedCount++;
                    continue;
                }
            }

            // Default: Ingest as Knowledge Chunk for RAG
            if (doc == null)
            {
                doc = new KnowledgeDocument
                {
                    Title = title,
                    Category = options.DefaultCategory,
                    Language = options.DefaultLanguage,
                    AccessLevel = options.AccessLevel
                };
                _dbContext.KnowledgeDocuments.Add(doc);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            var chunkContent = string.Join(" | ", row.Select(kv => $"{kv.Key}: {kv.Value}"));
            chunksToInsert.Add(new KnowledgeChunk
            {
                KnowledgeDocumentId = doc.Id,
                Content = chunkContent,
                ChunkIndex = chunksToInsert.Count,
                TokenCount = EstimateTokenCount(chunkContent)
            });
            result.ImportedCount++;
        }

        if (chunksToInsert.Count > 0)
        {
            _dbContext.KnowledgeChunks.AddRange(chunksToInsert);
            await _dbContext.SaveChangesAsync(cancellationToken);
            result.Messages.Add($"Berhasil mengimpor {chunksToInsert.Count} baris CSV ke Knowledge Base RAG.");
        }

        if (productsToInsert.Count > 0)
        {
            _dbContext.Products.AddRange(productsToInsert);
            await _dbContext.SaveChangesAsync(cancellationToken);
            result.Messages.Add($"Berhasil mengimpor {productsToInsert.Count} produk dari CSV.");
        }
    }

    private static List<string> ParseCsvLine(string line, char separator)
    {
        var result = new List<string>();
        bool inQuotes = false;
        var current = new StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == separator && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        result.Add(current.ToString());
        return result;
    }

    #endregion

    #region Excel Processor (.xlsx)

    private async Task ProcessExcelAsync(
        Stream stream,
        string fileName,
        DatasetImportOptions options,
        DatasetImportResultDto result,
        CancellationToken cancellationToken)
    {
        // Read OpenXML (.xlsx) using built-in ZipArchive and XDocument
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;

        using var archive = new ZipArchive(memoryStream, ZipArchiveMode.Read);

        // 1. Read shared strings
        var sharedStrings = new List<string>();
        var sharedStringsEntry = archive.GetEntry("xl/sharedStrings.xml");
        if (sharedStringsEntry != null)
        {
            using var sStream = sharedStringsEntry.Open();
            var sDoc = XDocument.Load(sStream);
            var ns = sDoc.Root?.Name.Namespace ?? XNamespace.None;
            foreach (var si in sDoc.Descendants(ns + "si"))
            {
                var text = string.Concat(si.Descendants(ns + "t").Select(t => t.Value));
                sharedStrings.Add(text);
            }
        }

        // 2. Read sheet1
        var sheetEntry = archive.GetEntry("xl/worksheets/sheet1.xml");
        if (sheetEntry == null)
        {
            result.Messages.Add("Sheet1 tidak ditemukan di dalam workbook Excel.");
            result.SkippedCount++;
            return;
        }

        using var sheetStream = sheetEntry.Open();
        var sheetDoc = XDocument.Load(sheetStream);
        var sNs = sheetDoc.Root?.Name.Namespace ?? XNamespace.None;

        var rows = sheetDoc.Descendants(sNs + "row").ToList();
        if (rows.Count == 0)
        {
            result.Messages.Add("File Excel tidak memiliki baris data.");
            return;
        }

        // Parse header row
        var headerRow = rows[0];
        var headers = ParseExcelRowCells(headerRow, sNs, sharedStrings);

        var title = Path.GetFileNameWithoutExtension(fileName);
        var doc = new KnowledgeDocument
        {
            Title = title,
            Category = options.DefaultCategory,
            Language = options.DefaultLanguage,
            AccessLevel = options.AccessLevel
        };
        _dbContext.KnowledgeDocuments.Add(doc);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var chunksToInsert = new List<KnowledgeChunk>();

        for (int r = 1; r < rows.Count; r++)
        {
            if (r > options.MaxRowsToProcess)
            {
                result.Messages.Add($"Batas maksimum pemrosesan Excel {options.MaxRowsToProcess} baris tercapai.");
                break;
            }

            result.TotalRecordsParsed++;
            var cells = ParseExcelRowCells(rows[r], sNs, sharedStrings);
            if (cells.Count == 0 || cells.All(string.IsNullOrWhiteSpace)) continue;

            var rowData = new List<string>();
            for (int c = 0; c < cells.Count; c++)
            {
                var colName = c < headers.Count ? headers[c] : $"Kolom_{c + 1}";
                rowData.Add($"{colName}: {cells[c]}");
            }

            var chunkContent = string.Join(" | ", rowData);
            chunksToInsert.Add(new KnowledgeChunk
            {
                KnowledgeDocumentId = doc.Id,
                Content = chunkContent,
                ChunkIndex = chunksToInsert.Count,
                TokenCount = EstimateTokenCount(chunkContent)
            });
            result.ImportedCount++;
        }

        if (chunksToInsert.Count > 0)
        {
            _dbContext.KnowledgeChunks.AddRange(chunksToInsert);
            await _dbContext.SaveChangesAsync(cancellationToken);
            result.Messages.Add($"Berhasil membaca spreadsheet Excel dan membuat {chunksToInsert.Count} Knowledge Chunks untuk RAG.");
        }
    }

    private static List<string> ParseExcelRowCells(XElement rowElem, XNamespace ns, List<string> sharedStrings)
    {
        var cells = new List<string>();
        foreach (var c in rowElem.Elements(ns + "c"))
        {
            var typeAttr = c.Attribute("t")?.Value;
            var valElem = c.Element(ns + "v");
            var valStr = valElem?.Value ?? "";

            if (typeAttr == "s" && int.TryParse(valStr, out var sIdx) && sIdx >= 0 && sIdx < sharedStrings.Count)
            {
                cells.Add(sharedStrings[sIdx]);
            }
            else
            {
                cells.Add(valStr);
            }
        }
        return cells;
    }

    #endregion

    private static int EstimateTokenCount(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;
        // Approximation: ~4 characters per token
        return Math.Max(1, (int)Math.Ceiling(text.Length / 4.0));
    }
}
