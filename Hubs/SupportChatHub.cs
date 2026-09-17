using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using dagangOnline.Data;
using dagangOnline.Domain.Chat;
using dagangOnline.Services;
using dagangOnline.Application.Services;
using System.Security.Claims;
using dagangOnline.Authorization;

namespace dagangOnline.Hubs;

[Authorize]
public class SupportChatHub : Hub
{
    private readonly ApplicationDbContext _db;
    private readonly AgentAssistService _agentAssistService;

    public SupportChatHub(ApplicationDbContext db, AgentAssistService agentAssistService)
    {
        _db = db;
        _agentAssistService = agentAssistService;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userRole = Context.User?.FindFirst(ClaimTypes.Role)?.Value;

        if (!string.IsNullOrEmpty(userId))
        {
            if (userRole == RoleConstants.Agent || userRole == RoleConstants.Admin)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "Agents");
            }
            else
            {
                // Join personal user group
                await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
            }
        }
        await base.OnConnectedAsync();
    }

    public async Task SendMessageToBot(Guid sessionId, string message)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return;

        var session = await _db.Conversations.FirstOrDefaultAsync(s => s.Id == sessionId && s.CustomerId == userId);
        if (session == null)
        {
            // Create a new session if not found
            session = new Conversation
            {
                CustomerId = userId,
                Status = ConversationStatus.Open
            };
            _db.Conversations.Add(session);
            await _db.SaveChangesAsync();
        }

        if (session.Status != ConversationStatus.Open) return;

        // Save user message
        var userMessage = new ConversationMessage
        {
            ConversationId = session.Id,
            Content = message,
            SenderType = SenderType.Customer
        };
        _db.ConversationMessages.Add(userMessage);
        await _db.SaveChangesAsync();

        // Broadcast back to user UI
        await Clients.Group($"User_{userId}").SendAsync("ReceiveMessage", session.Id, userMessage.Id, "User", message, userMessage.CreatedAt);

        // Get bot response
        var botResponse = await _agentAssistService.ProcessCustomerMessageAsync(session, message);

        // Save bot message
        var botMessage = new ConversationMessage
        {
            ConversationId = session.Id,
            Content = botResponse.SuggestedResponse,
            SenderType = SenderType.AI
        };
        _db.ConversationMessages.Add(botMessage);
        await _db.SaveChangesAsync(); // This also saves the updated session properties from ProcessCustomerMessageAsync

        // Broadcast bot reply to user UI
        await Clients.Group($"User_{userId}").SendAsync("ReceiveMessage", session.Id, botMessage.Id, "Bot", botResponse.SuggestedResponse, botMessage.CreatedAt);
        
        if (session.Status == ConversationStatus.Escalated)
        {
            // Notify agents if it escalated
            await Clients.Group("Agents").SendAsync("SessionEscalated", session.Id, userId);
        }
    }

    public async Task RequestEscalation(Guid sessionId)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return;

        var session = await _db.Conversations.FirstOrDefaultAsync(s => s.Id == sessionId && s.CustomerId == userId);
        if (session != null && session.Status == ConversationStatus.Open)
        {
            session.Status = ConversationStatus.Escalated;
            await _db.SaveChangesAsync();

            // Notify all agents about the new escalated session
            await Clients.Group("Agents").SendAsync("SessionEscalated", session.Id, userId);
            
            // Send system message to user
            await Clients.Group($"User_{userId}").SendAsync("ReceiveMessage", session.Id, Guid.NewGuid(), "System", "Sesi Anda sedang dialihkan ke agen kami. Mohon tunggu...", DateTime.UtcNow);
        }
    }

    [Authorize(Policy = AuthorizationPolicies.RequireAgentOrAdmin)]
    public async Task AcceptEscalation(Guid sessionId)
    {
        var agentId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(agentId)) return;

        var session = await _db.Conversations.FirstOrDefaultAsync(s => s.Id == sessionId);
        if (session != null && session.Status == ConversationStatus.Escalated)
        {
            session.Status = ConversationStatus.WaitingForCustomer;
            session.AssignedAgentId = agentId;
            await _db.SaveChangesAsync();

            // Add agent to session group
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Session_{session.Id}");

            // Notify user
            await Clients.Group($"User_{session.CustomerId}").SendAsync("AgentJoined", session.Id, agentId);
            await Clients.Group($"User_{session.CustomerId}").SendAsync("ReceiveMessage", session.Id, Guid.NewGuid(), "System", "Seorang agen telah bergabung ke sesi obrolan Anda.", DateTime.UtcNow);
            
            // Notify agents UI
            await Clients.Group("Agents").SendAsync("SessionAccepted", session.Id, agentId);
        }
    }

    [Authorize(Policy = AuthorizationPolicies.RequireAgentOrAdmin)]
    public async Task SendMessageToUser(Guid sessionId, string message)
    {
        var agentId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var agentName = Context.User?.Identity?.Name ?? "Agent";
        if (string.IsNullOrEmpty(agentId)) return;

        var session = await _db.Conversations.FirstOrDefaultAsync(s => s.Id == sessionId && s.AssignedAgentId == agentId);
        if (session != null && session.Status == ConversationStatus.WaitingForCustomer)
        {
            var agentMessage = new ConversationMessage
            {
                ConversationId = session.Id,
                Content = message,
                SenderType = SenderType.HumanAgent
            };
            _db.ConversationMessages.Add(agentMessage);
            await _db.SaveChangesAsync();

            // Send to user
            await Clients.Group($"User_{session.CustomerId}").SendAsync("ReceiveMessage", session.Id, agentMessage.Id, agentName, message, agentMessage.CreatedAt);
            
            // Send to agent (self)
            await Clients.Caller.SendAsync("ReceiveMessage", session.Id, agentMessage.Id, "You", message, agentMessage.CreatedAt);
        }
    }

    public async Task SendMessageToAgent(Guid sessionId, string message)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return;

        var session = await _db.Conversations.FirstOrDefaultAsync(s => s.Id == sessionId && s.CustomerId == userId);
        if (session != null && session.Status == ConversationStatus.WaitingForCustomer)
        {
            var userMessage = new ConversationMessage
            {
                ConversationId = session.Id,
                Content = message,
                SenderType = SenderType.Customer
            };
            _db.ConversationMessages.Add(userMessage);
            await _db.SaveChangesAsync();

            // Broadcast back to user UI
            await Clients.Group($"User_{userId}").SendAsync("ReceiveMessage", session.Id, userMessage.Id, "User", message, userMessage.CreatedAt);
            
            // Broadcast to agent
            await Clients.Group($"Session_{session.Id}").SendAsync("ReceiveMessage", session.Id, userMessage.Id, "User", message, userMessage.CreatedAt);
        }
    }

    public async Task EndSession(Guid sessionId)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userRole = Context.User?.FindFirst(ClaimTypes.Role)?.Value;

        var session = await _db.Conversations.FirstOrDefaultAsync(s => s.Id == sessionId);
        if (session == null) return;

        // Either the owner user or the assigned agent can end it
        if (session.CustomerId == userId || session.AssignedAgentId == userId)
        {
            session.Status = ConversationStatus.Resolved;
            await _db.SaveChangesAsync();

            await Clients.Group($"User_{session.CustomerId}").SendAsync("SessionEnded", session.Id);
            await Clients.Group($"Session_{session.Id}").SendAsync("SessionEnded", session.Id);
        }
    }
}
