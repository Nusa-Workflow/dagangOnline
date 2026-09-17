using System.Collections.Generic;
using dagangOnline.Application.DTOs;

namespace dagangOnline.Application.Interfaces;

public interface IRerankerService
{
    List<RetrievalResultDto> Rerank(List<RetrievalResultDto> denseResults, List<RetrievalResultDto> sparseResults, int finalTopK = 5);
}
