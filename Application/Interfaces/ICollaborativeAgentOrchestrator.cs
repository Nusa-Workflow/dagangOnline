using System.Threading;
using System.Threading.Tasks;
using dagangOnline.Application.DTOs;
using dagangOnline.Domain.Chat;

namespace dagangOnline.Application.Interfaces;

public interface ICollaborativeAgentOrchestrator
{
    Task<CollaborativeChatResponseDto> ExecuteCollaborativeChatAsync(
        CollaborativeChatRequestDto request,
        Conversation? session = null,
        CancellationToken cancellationToken = default);

    Task<CollaborativeChatResponseDto> ProcessCollaborativeTurnAsync(
        CollaborativeChatRequestDto request,
        CancellationToken cancellationToken = default);

    Task<CollaborativeChatResponseDto> GenerateDualAgentDraftsAsync(
        string customerMessage,
        Conversation session,
        CancellationToken cancellationToken = default);
}
