using System.Threading;
using System.Threading.Tasks;
using dagangOnline.Application.DTOs;

namespace dagangOnline.Application.Interfaces;

public interface IPortfolioService
{
    Task<PagedResult<PortfolioDto>> GetFeaturedPortfolioAsync(int page = 1, int pageSize = 100, CancellationToken cancellationToken = default);
    Task<PortfolioDto?> GetPortfolioByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PortfolioDto> CreatePortfolioAsync(CreatePortfolioDto dto, string? userName, CancellationToken cancellationToken = default);
    Task<PortfolioDto> UpdatePortfolioAsync(Guid id, UpdatePortfolioDto dto, string? userName, CancellationToken cancellationToken = default);
    Task DeletePortfolioAsync(Guid id, string? userName, CancellationToken cancellationToken = default);
}
