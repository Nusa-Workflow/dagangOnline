using System.Collections.Generic;
using dagangOnline.Application.DTOs;

namespace dagangOnline.Application.Interfaces;

public interface IGuardrailService
{
    GuardrailResultDto ValidateInput(string userInput);
    GuardrailResultDto ValidateRetrievalAccess(string userId, string userRole, List<RetrievalResultDto> retrievedChunks);
    GuardrailResultDto ValidateOutput(string generatedResponse, List<RetrievalResultDto> retrievedEvidence);
}
