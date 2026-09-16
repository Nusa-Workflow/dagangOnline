using System.Threading;
using System.Threading.Tasks;
using dagangOnline.Application.DTOs;

namespace dagangOnline.Application.Interfaces;

public interface ISearchService
{
    Task<PagedResult<SearchResultItem>> SearchAsync(string query, int page = 1, int pageSize = 100, CancellationToken cancellationToken = default);
}
