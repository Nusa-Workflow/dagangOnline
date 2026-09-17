using System;
using System.Collections.Generic;
using System.Linq;
using dagangOnline.Application.DTOs;
using dagangOnline.Application.Interfaces;

namespace dagangOnline.Application.Services.RAG;

public class RetrievalEvaluationService : IRetrievalEvaluationService
{
    public RetrievalMetricsDto EvaluateRetrieval(List<int> retrievedChunkIds, List<int> relevantChunkIds, int k = 5)
    {
        var topKRetrieved = retrievedChunkIds.Take(k).ToList();
        var relevantSet = new HashSet<int>(relevantChunkIds);

        if (relevantSet.Count == 0 || topKRetrieved.Count == 0)
        {
            return new RetrievalMetricsDto();
        }

        // Precision@K & Recall@K
        int hits = topKRetrieved.Count(id => relevantSet.Contains(id));
        double precisionAtK = (double)hits / topKRetrieved.Count;
        double recallAtK = (double)hits / relevantSet.Count;
        double hitAtK = hits > 0 ? 1.0 : 0.0;

        // MRR (Mean Reciprocal Rank)
        double mrr = 0.0;
        for (int i = 0; i < topKRetrieved.Count; i++)
        {
            if (relevantSet.Contains(topKRetrieved[i]))
            {
                mrr = 1.0 / (i + 1);
                break;
            }
        }

        // DCG & IDCG for nDCG
        double dcg = 0.0;
        for (int i = 0; i < topKRetrieved.Count; i++)
        {
            if (relevantSet.Contains(topKRetrieved[i]))
            {
                dcg += 1.0 / (Math.Log2(i + 2)); // log2(rank + 1) where rank is 1-indexed
            }
        }

        double idcg = 0.0;
        int maxHits = Math.Min(k, relevantSet.Count);
        for (int i = 0; i < maxHits; i++)
        {
            idcg += 1.0 / (Math.Log2(i + 2));
        }

        double ndcg = idcg > 0 ? dcg / idcg : 0.0;

        return new RetrievalMetricsDto
        {
            PrecisionAtK = Math.Round(precisionAtK, 4),
            RecallAtK = Math.Round(recallAtK, 4),
            HitAtK = hitAtK,
            MeanReciprocalRank = Math.Round(mrr, 4),
            NormalizedDcg = Math.Round(ndcg, 4),
            ContextRelevance = Math.Round(precisionAtK, 4),
            ContextCoverage = Math.Round(recallAtK, 4)
        };
    }
}
