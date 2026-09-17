using System.Threading;
using System.Threading.Tasks;

namespace dagangOnline.Application.Interfaces;

public interface IEmbeddingService
{
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
}
