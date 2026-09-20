using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using dagangOnline.Application.DTOs;
using dagangOnline.Application.Services.DataImport;
using dagangOnline.Application.Services.Economic;
using dagangOnline.Data;
using Xunit;

namespace dagangOnline.Tests;

public class DatasetImportTests
{
    private class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider WebRootFileProvider { get; set; } = null!;
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "dagangOnline";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
    }

    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private DatasetImportService CreateService(ApplicationDbContext dbContext, EconomicGraphEngine graphEngine)
    {
        var env = new TestWebHostEnvironment();
        var logger = NullLogger<DatasetImportService>.Instance;
        return new DatasetImportService(dbContext, graphEngine, env, logger);
    }

    [Fact]
    public void Supported_Formats_Should_Include_JsonL_And_Txt()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var graph = new EconomicGraphEngine();
        var service = CreateService(db, graph);

        // Act
        var formats = service.GetSupportedFormats();

        // Assert
        Assert.NotNull(formats);
        Assert.Contains(formats, f => f.Extension.Equals(".jsonl", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(formats, f => f.Extension.Equals(".txt", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(formats, f => f.Extension.Equals(".csv", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(formats, f => f.Extension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Can_Import_JsonLines_RAG_Knowledge_Dataset()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var graph = new EconomicGraphEngine();
        var service = CreateService(db, graph);

        var jsonlContent = new StringBuilder()
            .AppendLine("{\"title\": \"FAQ Ongkir\", \"content\": \"Subsidi ongkir berlaku untuk mitra Pulau Jawa.\", \"category\": \"Logistik\"}")
            .AppendLine("{\"title\": \"FAQ Escrow\", \"content\": \"Dana transaksi disimpan di rekening escrow aman.\", \"category\": \"Pembayaran\"}")
            .ToString();

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonlContent));

        // Act
        var result = await service.ImportStreamAsync(stream, "knowledge_faq.jsonl");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.ImportedCount);
        Assert.Equal(".jsonl", result.DetectedFormat);

        var docs = await db.KnowledgeDocuments.Include(d => d.Chunks).ToListAsync();
        Assert.NotEmpty(docs);
        var totalChunks = docs.Sum(d => d.Chunks.Count);
        Assert.Equal(2, totalChunks);
    }

    [Fact]
    public async Task Can_Import_JsonLines_Economic_Indicators()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var graph = new EconomicGraphEngine();
        var service = CreateService(db, graph);

        var jsonlContent = new StringBuilder()
            .AppendLine("{\"id\": \"RUBBER_PRICE\", \"label\": \"Harga Karet Alam Nasional\", \"category\": \"Commodity\", \"currentValue\": 24500.0, \"unit\": \"IDR/Kg\", \"volatility30d\": 0.22, \"sentimentScore\": 0.35}")
            .ToString();

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonlContent));

        // Act
        var result = await service.ImportStreamAsync(stream, "commodities.jsonl");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(1, result.ImportedCount);

        var node = graph.GetNode("RUBBER_PRICE");
        Assert.NotNull(node);
        Assert.Equal("Harga Karet Alam Nasional", node.Label);
        Assert.Equal(24500.0, node.CurrentValue);
        Assert.Equal("IDR/Kg", node.Unit);
    }

    [Fact]
    public async Task Can_Import_Txt_Corpus_Dataset_With_Paragraph_Chunking()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var graph = new EconomicGraphEngine();
        var service = CreateService(db, graph);

        var txtContent = new StringBuilder()
            .AppendLine("# Panduan Operasional UMKM 2026")
            .AppendLine()
            .AppendLine("BAB 1: KETENTUAN MITRA")
            .AppendLine("Mitra usaha mikro kecil dan menengah wajib memiliki identitas NIK dan rekening aktif untuk verifikasi kyc.")
            .AppendLine()
            .AppendLine("BAB 2: STANDAR PACKAGING DAN PENGIRIMAN")
            .AppendLine("Semua barang makanan basah harus menggunakan pengemasan vacuum seal dan es gel agar tetap higienis selama perjalanan.")
            .ToString();

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(txtContent));

        // Act
        var result = await service.ImportStreamAsync(stream, "panduan_umkm.txt");

        // Assert
        Assert.True(result.Success);
        Assert.True(result.ImportedCount >= 2, "Expected at least 2 chunks from paragraphs");
        Assert.Equal(".txt", result.DetectedFormat);

        var doc = await db.KnowledgeDocuments.Include(d => d.Chunks).FirstOrDefaultAsync();
        Assert.NotNull(doc);
        Assert.Equal("Panduan Operasional UMKM 2026", doc.Title);
        Assert.NotEmpty(doc.Chunks);
    }

    [Fact]
    public async Task Can_Import_Csv_Dataset()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var graph = new EconomicGraphEngine();
        var service = CreateService(db, graph);

        var csvContent = new StringBuilder()
            .AppendLine("Id,Label,Category,CurrentValue,Unit")
            .AppendLine("GOLD_PRICE,Harga Emas Logam Mulia,Commodity,1420000.0,IDR/Gram")
            .ToString();

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await service.ImportStreamAsync(stream, "prices.csv");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(1, result.ImportedCount);

        var node = graph.GetNode("GOLD_PRICE");
        Assert.NotNull(node);
        Assert.Equal(1420000.0, node.CurrentValue);
    }

    [Fact]
    public async Task Handles_Malformed_JsonLines_Gracefully()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var graph = new EconomicGraphEngine();
        var service = CreateService(db, graph);

        var jsonlContent = new StringBuilder()
            .AppendLine("{\"title\": \"Valid Row 1\", \"content\": \"Konten valid pertama.\"}")
            .AppendLine("{INI_BUKAN_JSON_VALID}")
            .AppendLine("{\"title\": \"Valid Row 2\", \"content\": \"Konten valid kedua.\"}")
            .ToString();

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonlContent));

        // Act
        var result = await service.ImportStreamAsync(stream, "partial_corrupt.jsonl");

        // Assert
        Assert.True(result.Success, "Operation should partially succeed");
        Assert.Equal(2, result.ImportedCount);
        Assert.Equal(1, result.ErrorCount);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task Can_Discover_And_Parse_Real_Imports_Directory_Files()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var graph = new EconomicGraphEngine();
        var env = new TestWebHostEnvironment();
        // Point ContentRootPath to project root by walking up until dagangOnline.sln is found
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current != null && !File.Exists(Path.Combine(current.FullName, "dagangOnline.sln")))
        {
            current = current.Parent;
        }
        var projectRoot = current?.FullName ?? Directory.GetCurrentDirectory();
        env.ContentRootPath = projectRoot;

        var service = new DatasetImportService(db, graph, env, NullLogger<DatasetImportService>.Instance);

        // Act
        var availableFiles = service.GetAvailableImportFiles();

        // Assert
        Assert.NotEmpty(availableFiles);
        Assert.Contains(availableFiles, f => f.Extension == ".jsonl");

        // Act 2: Import research_mid_training_id.jsonl
        var researchFile = availableFiles.FirstOrDefault(f => f.FileName == "research_mid_training_id.jsonl");
        if (researchFile != null)
        {
            var fullPath = Path.Combine(projectRoot, researchFile.RelativePath);
            var importResult = await service.ImportFromFileAsync(fullPath, new DatasetImportOptions { MaxRowsToProcess = 50 });
            Assert.True(importResult.Success);
            Assert.True(importResult.ImportedCount > 0);
        }
    }
}
