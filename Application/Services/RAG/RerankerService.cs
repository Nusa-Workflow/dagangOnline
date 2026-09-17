using System.Collections.Generic;
using System.Linq;
using dagangOnline.Application.DTOs;
using dagangOnline.Application.Interfaces;

namespace dagangOnline.Application.Services.RAG;

public class RerankerService : IRerankerService
{
    private const double RrfK = 60.0;

    public List<RetrievalResultDto> Rerank(List<RetrievalResultDto> denseResults, List<RetrievalResultDto> sparseResults, int finalTopK = 5)
    {
        var scores = new Dictionary<int, (RetrievalResultDto Item, double Score)>();

        // Dense scoring
        for (int rank = 0; rank < denseResults.Count; rank++)
        {
            var item = denseResults[rank];
            double rrfScore = 1.0 / (RrfK + rank + 1);

            if (scores.TryGetValue(item.ChunkId, out var existing))
            {
                scores[item.ChunkId] = (existing.Item, existing.Score + rrfScore);
            }
            else
            {
                scores[item.ChunkId] = (item, rrfScore);
            }
        }

        // Sparse scoring
        for (int rank = 0; rank < sparseResults.Count; rank++)
        {
            var item = sparseResults[rank];
            double rrfScore = 1.0 / (RrfK + rank + 1);

            if (scores.TryGetValue(item.ChunkId, out var existing))
            {
                scores[item.ChunkId] = (existing.Item, existing.Score + rrfScore);
            }
            else
            {
                scores[item.ChunkId] = (item, rrfScore);
            }
        }

        return scores.Values
            .OrderByDescending(x => x.Score)
            .Take(finalTopK)
            .Select(x =>
            {
                var copy = x.Item;
                copy.Score = x.Score;
                copy.RetrievalType = "Hybrid (RRF)";
                return copy;
            })
            .ToList();
    }
}
