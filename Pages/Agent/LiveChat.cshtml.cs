using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using dagangOnline.Authorization;
using dagangOnline.Data;
using dagangOnline.Domain.Chat;
using dagangOnline.Application.Interfaces;

namespace dagangOnline.Pages.Agent;

[Authorize(Policy = AuthorizationPolicies.RequireAgentOrAdmin)]
public class LiveChatModel : PageModel
{
    private readonly IConversationRepository _conversationRepository;
    private readonly dagangOnline.Application.Services.Economic.EconomicGraphEngine _graphEngine;

    public LiveChatModel(
        IConversationRepository conversationRepository,
        dagangOnline.Application.Services.Economic.EconomicGraphEngine graphEngine)
    {
        _conversationRepository = conversationRepository;
        _graphEngine = graphEngine;
    }

    public List<Conversation> EscalatedSessions { get; set; } = new();
    public List<Conversation> MyActiveSessions { get; set; } = new();
    public List<dagangOnline.Domain.Economic.EconomicGraphNode> EconomicIndicators { get; set; } = new();
    
    [BindProperty(SupportsGet = true)]
    public Guid? ActiveSessionId { get; set; }
    
    public Conversation? CurrentSession { get; set; }

    public async Task OnGetAsync()
    {
        var currentCustomerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Get sessions waiting for any agent
        EscalatedSessions = (await _conversationRepository.GetActiveConversationsAsync())
            .Where(s => s.Status == ConversationStatus.Escalated)
            .OrderBy(s => s.UpdatedAt)
            .ToList(); // Note: Repository doesn't load Customer natively in GetActive... wait, let's just use the repo

        // Get sessions currently handled by this agent
        MyActiveSessions = (await _conversationRepository.GetConversationsByAgentAsync(currentCustomerId ?? string.Empty))
            .Where(s => s.Status == ConversationStatus.WaitingForCustomer || s.Status == ConversationStatus.Open)
            .OrderByDescending(s => s.UpdatedAt)
            .ToList();

        if (ActiveSessionId.HasValue)
        {
            CurrentSession = await _conversationRepository.GetByIdAsync(ActiveSessionId.Value, trackChanges: false);
        }

        EconomicIndicators = _graphEngine.GetAllNodes();
    }

    public async Task<IActionResult> OnPostAcceptEscalationAsync(Guid sessionId)
    {
        var currentCustomerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var session = await _conversationRepository.GetByIdAsync(sessionId, trackChanges: true);
        
        if (session != null && session.Status == ConversationStatus.Escalated)
        {
            session.Status = ConversationStatus.WaitingForCustomer;
            session.AssignedAgentId = currentCustomerId;
            session.UpdatedAt = DateTime.UtcNow;
            await _conversationRepository.UpdateAsync(session);
        }

        return RedirectToPage(new { ActiveSessionId = sessionId });
    }

    public async Task<IActionResult> OnPostUpdateStatusAsync(Guid sessionId, ConversationStatus newStatus)
    {
        var session = await _conversationRepository.GetByIdAsync(sessionId, trackChanges: true);
        if (session != null)
        {
            session.Status = newStatus;
            session.UpdatedAt = DateTime.UtcNow;
            if (newStatus == ConversationStatus.Closed || newStatus == ConversationStatus.Resolved)
            {
                session.ClosedAt = DateTime.UtcNow;
            }
            await _conversationRepository.UpdateAsync(session);
        }
        return RedirectToPage(new { ActiveSessionId = sessionId });
    }

    public async Task<IActionResult> OnPostUpdatePriorityAsync(Guid sessionId, ConversationPriority newPriority)
    {
        var session = await _conversationRepository.GetByIdAsync(sessionId, trackChanges: true);
        if (session != null)
        {
            session.Priority = newPriority;
            session.UpdatedAt = DateTime.UtcNow;
            await _conversationRepository.UpdateAsync(session);
        }
        return RedirectToPage(new { ActiveSessionId = sessionId });
    }
}
