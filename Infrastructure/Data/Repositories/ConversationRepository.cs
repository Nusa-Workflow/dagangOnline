using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using dagangOnline.Application.Interfaces;
using dagangOnline.Data;
using dagangOnline.Domain.Chat;
using Microsoft.EntityFrameworkCore;

namespace dagangOnline.Infrastructure.Data.Repositories;

public class ConversationRepository : IConversationRepository
{
    private readonly ApplicationDbContext _context;

    public ConversationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Conversation?> GetByIdAsync(Guid id, bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        var query = trackChanges ? _context.Conversations : _context.Conversations.AsNoTracking();
        return await query
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<List<Conversation>> GetActiveConversationsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Conversations.AsNoTracking()
            .Where(c => c.Status != ConversationStatus.Closed && c.Status != ConversationStatus.Resolved)
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Conversation>> GetConversationsByCustomerAsync(string customerId, CancellationToken cancellationToken = default)
    {
        return await _context.Conversations.AsNoTracking()
            .Where(c => c.CustomerId == customerId)
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Conversation>> GetConversationsByAgentAsync(string agentId, CancellationToken cancellationToken = default)
    {
        return await _context.Conversations.AsNoTracking()
            .Where(c => c.AssignedAgentId == agentId)
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Conversation conversation, CancellationToken cancellationToken = default)
    {
        await _context.Conversations.AddAsync(conversation, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Conversation conversation, CancellationToken cancellationToken = default)
    {
        _context.Conversations.Update(conversation);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddMessageAsync(ConversationMessage message, CancellationToken cancellationToken = default)
    {
        await _context.ConversationMessages.AddAsync(message, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
