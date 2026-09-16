using System.Threading;
using System.Threading.Tasks;
using dagangOnline.Application.DTOs;

namespace dagangOnline.Application.Interfaces;

public interface IServiceCatalogService
{
    Task<PagedResult<ServiceDto>> GetPublishedServicesAsync(int page = 1, int pageSize = 100, CancellationToken cancellationToken = default);
    Task<ServiceDto?> GetServiceBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<ServiceDto?> GetServiceByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ServiceDto> CreateServiceAsync(CreateServiceDto dto, string? userName, CancellationToken cancellationToken = default);
    Task<ServiceDto> UpdateServiceAsync(Guid id, UpdateServiceDto dto, string? userName, CancellationToken cancellationToken = default);
    Task DeleteServiceAsync(Guid id, string? userName, CancellationToken cancellationToken = default);
}
