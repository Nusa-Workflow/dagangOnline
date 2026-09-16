using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace dagangOnline.Domain.Repositories;

public interface IServiceRepository
{
    Task<(List<Service> Items, int TotalCount)> GetPublishedServicesAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<List<Service>> GetPublishedServicesAsync(bool? isFeatured, CancellationToken cancellationToken = default);
    Task<Service?> GetByIdAsync(Guid id, bool trackChanges = false, CancellationToken cancellationToken = default);
    Task<Service?> GetBySlugAsync(string slug, bool trackChanges = false, CancellationToken cancellationToken = default);
    Task<List<Service>> SearchPublishedAsync(string query, int limit, CancellationToken cancellationToken = default);
    
    Task AddAsync(Service service, CancellationToken cancellationToken = default);
    Task UpdateAsync(Service service, CancellationToken cancellationToken = default);
    Task DeleteAsync(Service service, CancellationToken cancellationToken = default);
}
