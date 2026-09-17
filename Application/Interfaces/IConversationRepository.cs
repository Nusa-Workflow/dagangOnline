using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using dagangOnline.Domain.Chat;

namespace dagangOnline.Application.Interfaces;

public interface IConversationRepository
{
    Task<Conversation?> GetByIdAsync(Guid id, bool trackChanges = false, CancellationToken cancellationToken = default);
    Task<List<Conversation>> GetActiveConversationsAsync(CancellationToken cancellationToken = default);
    Task<List<Conversation>> GetConversationsByCustomerAsync(string customerId, CancellationToken cancellationToken = default);
    Task<List<Conversation>> GetConversationsByAgentAsync(string agentId, CancellationToken cancellationToken = default);
    Task AddAsync(Conversation conversation, CancellationToken cancellationToken = default);
    Task UpdateAsync(Conversation conversation, CancellationToken cancellationToken = default);
    Task AddMessageAsync(ConversationMessage message, CancellationToken cancellationToken = default);
}
