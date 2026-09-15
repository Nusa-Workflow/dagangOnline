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

namespace dagangOnline.Pages.Agent;

[Authorize(Policy = AuthorizationPolicies.RequireAgentOrAdmin)]
public class LiveChatModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public LiveChatModel(ApplicationDbContext db)
    {
        _db = db;
    }

    public List<ChatSession> EscalatedSessions { get; set; } = new();
    public List<ChatSession> MyActiveSessions { get; set; } = new();
    
    [BindProperty(SupportsGet = true)]
    public Guid? ActiveSessionId { get; set; }
    
    public ChatSession? CurrentSession { get; set; }

    public async Task OnGetAsync()
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Get sessions waiting for any agent
        EscalatedSessions = await _db.ChatSessions
            .Include(s => s.User)
            .Where(s => s.Status == ChatSessionStatus.Escalated)
            .OrderBy(s => s.UpdatedAt)
            .ToListAsync();

        // Get sessions currently handled by this agent
        MyActiveSessions = await _db.ChatSessions
            .Include(s => s.User)
            .Where(s => s.Status == ChatSessionStatus.ActiveWithAgent && s.AssignedAgentId == currentUserId)
            .OrderByDescending(s => s.UpdatedAt)
            .ToListAsync();

        if (ActiveSessionId.HasValue)
        {
            CurrentSession = await _db.ChatSessions
                .Include(s => s.User)
                .Include(s => s.Messages)
                .FirstOrDefaultAsync(s => s.Id == ActiveSessionId.Value && (s.AssignedAgentId == currentUserId || s.Status == ChatSessionStatus.Escalated));
        }
    }

    public async Task<IActionResult> OnPostAcceptEscalationAsync(Guid sessionId)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var session = await _db.ChatSessions.FirstOrDefaultAsync(s => s.Id == sessionId && s.Status == ChatSessionStatus.Escalated);
        
        if (session != null)
        {
            session.Status = ChatSessionStatus.ActiveWithAgent;
            session.AssignedAgentId = currentUserId;
            session.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            
            // Note: SignalR Hub AcceptEscalation is typically called from client, but we can also just redirect.
            // But we already have a hub method for it. The client will call the hub method. 
            // We'll let the UI call the hub method directly via JS to ensure groups are updated.
        }

        return RedirectToPage(new { ActiveSessionId = sessionId });
    }
}
