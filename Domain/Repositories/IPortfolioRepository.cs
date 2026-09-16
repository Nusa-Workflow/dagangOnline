using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace dagangOnline.Domain.Repositories;

public interface IPortfolioRepository
{
    Task<(List<PortfolioProject> Items, int TotalCount)> GetFeaturedPortfolioAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<List<PortfolioProject>> GetPublishedPortfolioAsync(bool? isFeatured, CancellationToken cancellationToken = default);
    Task<PortfolioProject?> GetByIdAsync(Guid id, bool trackChanges = false, CancellationToken cancellationToken = default);
    Task<List<PortfolioProject>> SearchPublishedAsync(string query, int limit, CancellationToken cancellationToken = default);
    
    Task AddAsync(PortfolioProject project, CancellationToken cancellationToken = default);
    Task UpdateAsync(PortfolioProject project, CancellationToken cancellationToken = default);
    Task DeleteAsync(PortfolioProject project, CancellationToken cancellationToken = default);
}
