namespace dagangOnline.Application.Interfaces;

public interface IAiChatService
{
    Task<string> GetChatResponseAsync(string userMessage, string contextData, CancellationToken cancellationToken = default);
}
