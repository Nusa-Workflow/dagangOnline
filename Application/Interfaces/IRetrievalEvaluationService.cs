using System.Collections.Generic;
using dagangOnline.Application.DTOs;

namespace dagangOnline.Application.Interfaces;

public interface IRetrievalEvaluationService
{
    RetrievalMetricsDto EvaluateRetrieval(List<int> retrievedChunkIds, List<int> relevantChunkIds, int k = 5);
}
