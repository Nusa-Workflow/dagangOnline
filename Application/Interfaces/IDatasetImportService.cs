using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using dagangOnline.Application.DTOs;

namespace dagangOnline.Application.Interfaces;

public interface IDatasetImportService
{
    Task<DatasetImportResultDto> ImportStreamAsync(
        Stream stream,
        string fileName,
        DatasetImportOptions? options = null,
        CancellationToken cancellationToken = default);

    Task<DatasetImportResultDto> ImportFromFileAsync(
        string filePath,
        DatasetImportOptions? options = null,
        CancellationToken cancellationToken = default);

    Task<List<DatasetImportResultDto>> ScanAndImportDirectoryAsync(
        string? directoryPath = null,
        DatasetImportOptions? options = null,
        CancellationToken cancellationToken = default);

    List<ImportFileInfo> GetAvailableImportFiles(string? directoryPath = null);

    List<SupportedFormatInfoDto> GetSupportedFormats();
}
