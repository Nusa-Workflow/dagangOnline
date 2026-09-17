using System.Threading;
using System.Threading.Tasks;
using dagangOnline.Application.DTOs;

namespace dagangOnline.Application.Interfaces;

public interface IAiChatService
{
    Task<AiSuggestionDto> GetChatResponseAsync(string userMessage, string contextData, CancellationToken cancellationToken = default);
}
