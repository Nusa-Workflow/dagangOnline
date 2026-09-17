using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using dagangOnline.Application.DTOs;

namespace dagangOnline.Application.Interfaces;

public interface IGroundingService
{
    Task<GroundingAssessmentDto> AssessGroundingAsync(string generatedResponse, List<RetrievalResultDto> retrievedEvidence, CancellationToken cancellationToken = default);
}
