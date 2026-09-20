using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using dagangOnline.Application.DTOs;

namespace dagangOnline.Application.Interfaces;

public interface IDatasetFineTuningEngine
{
    Task<DatasetFineTuningReportDto> TrainModelFromDatasetsAsync(CancellationToken cancellationToken = default);
    DatasetFineTuningReportDto GetCurrentTrainingStatus();
    SectorEmpiricalSummaryDto? GetSectorEmpiricalMetrics(string sector);
}
