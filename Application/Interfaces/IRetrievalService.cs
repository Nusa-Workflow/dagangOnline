using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using dagangOnline.Application.DTOs;

namespace dagangOnline.Application.Interfaces;

public interface IRetrievalService
{
    Task<List<RetrievalResultDto>> RetrieveDenseAsync(float[] queryEmbedding, int topK = 5, CancellationToken cancellationToken = default);
    Task<List<RetrievalResultDto>> RetrieveSparseAsync(string query, int topK = 5, CancellationToken cancellationToken = default);
    Task<List<RetrievalResultDto>> RetrieveHybridAsync(string query, float[] queryEmbedding, int topK = 5, CancellationToken cancellationToken = default);
}
