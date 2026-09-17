using System.Threading;
using System.Threading.Tasks;

namespace dagangOnline.Application.Interfaces;

public interface IIndonesiaContextLayer
{
    Task<string> EnrichContextWithLocalKnowledgeAsync(string query, string detectedLanguage, CancellationToken cancellationToken = default);
    bool ContainsRegionalAdministrativeTerms(string query);
}
