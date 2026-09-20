using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using dagangOnline.Application.DTOs;
using dagangOnline.Application.Interfaces;
using dagangOnline.Authorization;

namespace dagangOnline.Controllers.Api.v1;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class DatasetController : ControllerBase
{
    private readonly IDatasetImportService _importService;

    public DatasetController(IDatasetImportService importService)
    {
        _importService = importService;
    }

    /// <summary>
    /// Daftar format file dataset yang didukung (.jsonl, .txt, .csv, .xlsx).
    /// </summary>
    [HttpGet("supported-formats")]
    [ProducesResponseType(typeof(ApiResponse<List<SupportedFormatInfoDto>>), StatusCodes.Status200OK)]
    public IActionResult GetSupportedFormats()
    {
        var formats = _importService.GetSupportedFormats();
        return Ok(ApiResponse<List<SupportedFormatInfoDto>>.Ok(formats, "Daftar format dataset yang didukung."));
    }

    /// <summary>
    /// Daftar file dataset yang ada di folder Data/Imports.
    /// </summary>
    [HttpGet("pending-files")]
    [Authorize(Policy = AuthorizationPolicies.RequireAgentOrAdmin)]
    [ProducesResponseType(typeof(ApiResponse<List<ImportFileInfo>>), StatusCodes.Status200OK)]
    public IActionResult GetPendingFiles()
    {
        var files = _importService.GetAvailableImportFiles();
        return Ok(ApiResponse<List<ImportFileInfo>>.Ok(files, "Daftar file dataset pada folder Data/Imports."));
    }

    /// <summary>
    /// Unggah dan impor file dataset (.jsonl, .txt, .csv, .xlsx) secara langsung.
    /// </summary>
    [HttpPost("upload")]
    [Authorize(Policy = AuthorizationPolicies.RequireAgentOrAdmin)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<DatasetImportResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<DatasetImportResultDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadDataset(
        IFormFile file,
        [FromForm] DatasetTargetType targetType = DatasetTargetType.AutoDetect,
        [FromForm] string defaultCategory = "General",
        [FromForm] string defaultLanguage = "id-ID",
        [FromForm] string accessLevel = "Public",
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(ApiResponse<DatasetImportResultDto>.Fail("File dataset tidak boleh kosong."));
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowed = new[] { ".jsonl", ".jsonlines", ".txt", ".text", ".csv", ".xlsx", ".xls" };
        if (Array.IndexOf(allowed, ext) < 0)
        {
            return BadRequest(ApiResponse<DatasetImportResultDto>.Fail(
                $"Ekstensi '{ext}' tidak didukung. Format yang didukung: .jsonl, .txt, .csv, .xlsx."));
        }

        var options = new DatasetImportOptions
        {
            TargetType = targetType,
            DefaultCategory = defaultCategory,
            DefaultLanguage = defaultLanguage,
            AccessLevel = accessLevel
        };

        await using var stream = file.OpenReadStream();
        var result = await _importService.ImportStreamAsync(stream, file.FileName, options, cancellationToken);

        if (!result.Success && result.ImportedCount == 0)
        {
            return BadRequest(ApiResponse<DatasetImportResultDto>.Fail(
                $"Import gagal: {string.Join(", ", result.Errors)}", result.Errors));
        }

        return Ok(ApiResponse<DatasetImportResultDto>.Ok(result,
            $"File dataset '{file.FileName}' berhasil diproses. {result.ImportedCount} entri berhasil diimpor."));
    }

    /// <summary>
    /// Memproses dan mengimpor file yang diletakkan di folder lokal Data/Imports.
    /// </summary>
    [HttpPost("import-local")]
    [Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
    [ProducesResponseType(typeof(ApiResponse<List<DatasetImportResultDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ImportLocalFiles(
        [FromQuery] string? fileName = null,
        [FromQuery] DatasetTargetType targetType = DatasetTargetType.AutoDetect,
        CancellationToken cancellationToken = default)
    {
        var options = new DatasetImportOptions { TargetType = targetType };

        if (!string.IsNullOrWhiteSpace(fileName))
        {
            var files = _importService.GetAvailableImportFiles();
            var target = files.Find(f => f.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase));
            if (target == null)
            {
                return NotFound(ApiResponse<List<DatasetImportResultDto>>.Fail($"File '{fileName}' tidak ditemukan di Data/Imports."));
            }

            var singleResult = await _importService.ImportFromFileAsync(target.RelativePath, options, cancellationToken);
            return Ok(ApiResponse<List<DatasetImportResultDto>>.Ok(new List<DatasetImportResultDto> { singleResult },
                $"File '{fileName}' berhasil diproses."));
        }

        var results = await _importService.ScanAndImportDirectoryAsync(null, options, cancellationToken);
        return Ok(ApiResponse<List<DatasetImportResultDto>>.Ok(results,
            $"{results.Count} file dari folder Data/Imports berhasil diproses."));
    }
}
