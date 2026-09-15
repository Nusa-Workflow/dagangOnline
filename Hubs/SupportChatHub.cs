using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using dagangOnline.Data;
using dagangOnline.Domain.Chat;
using dagangOnline.Services;
using System.Security.Claims;
using dagangOnline.Authorization;

namespace dagangOnline.Hubs;

[Authorize]
public class SupportChatHub : Hub
{
    private readonly ApplicationDbContext _db;
    private readonly ChatBotService _chatBotService;

    public SupportChatHub(ApplicationDbContext db, ChatBotService chatBotService)
    {
        _db = db;
        _chatBotService = chatBotService;
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

        var session = await _db.ChatSessions.FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);
        if (session == null)
        {
            // Create a new session if not found
            session = new ChatSession
            {
                UserId = userId,
                Status = ChatSessionStatus.ActiveWithBot
            };
            _db.ChatSessions.Add(session);
            await _db.SaveChangesAsync();
        }

        if (session.Status != ChatSessionStatus.ActiveWithBot) return;

        // Save user message
        var userMessage = new ChatMessage
        {
            ChatSessionId = session.Id,
            Content = message,
            SenderRole = ChatMessageSenderRole.User
        };
        _db.ChatMessages.Add(userMessage);
        await _db.SaveChangesAsync();

        // Broadcast back to user UI
        await Clients.Group($"User_{userId}").SendAsync("ReceiveMessage", session.Id, userMessage.Id, "User", message, userMessage.CreatedAt);

        // Get bot response
        var botResponse = await _chatBotService.GetReplyAsync(session, message);

        // Save bot message
        var botMessage = new ChatMessage
        {
            ChatSessionId = session.Id,
            Content = botResponse,
            SenderRole = ChatMessageSenderRole.Bot
        };
        _db.ChatMessages.Add(botMessage);
        await _db.SaveChangesAsync();

        // Broadcast bot reply to user UI
        await Clients.Group($"User_{userId}").SendAsync("ReceiveMessage", session.Id, botMessage.Id, "Bot", botResponse, botMessage.CreatedAt);
    }

    public async Task RequestEscalation(Guid sessionId)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return;

        var session = await _db.ChatSessions.FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);
        if (session != null && session.Status == ChatSessionStatus.ActiveWithBot)
        {
            session.Status = ChatSessionStatus.Escalated;
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

        var session = await _db.ChatSessions.FirstOrDefaultAsync(s => s.Id == sessionId);
        if (session != null && session.Status == ChatSessionStatus.Escalated)
        {
            session.Status = ChatSessionStatus.ActiveWithAgent;
            session.AssignedAgentId = agentId;
            await _db.SaveChangesAsync();

            // Add agent to session group
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Session_{session.Id}");

            // Notify user
            await Clients.Group($"User_{session.UserId}").SendAsync("AgentJoined", session.Id, agentId);
            await Clients.Group($"User_{session.UserId}").SendAsync("ReceiveMessage", session.Id, Guid.NewGuid(), "System", "Seorang agen telah bergabung ke sesi obrolan Anda.", DateTime.UtcNow);
            
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

        var session = await _db.ChatSessions.FirstOrDefaultAsync(s => s.Id == sessionId && s.AssignedAgentId == agentId);
        if (session != null && session.Status == ChatSessionStatus.ActiveWithAgent)
        {
            var agentMessage = new ChatMessage
            {
                ChatSessionId = session.Id,
                Content = message,
                SenderRole = ChatMessageSenderRole.Agent
            };
            _db.ChatMessages.Add(agentMessage);
            await _db.SaveChangesAsync();

            // Send to user
            await Clients.Group($"User_{session.UserId}").SendAsync("ReceiveMessage", session.Id, agentMessage.Id, agentName, message, agentMessage.CreatedAt);
            
            // Send to agent (self)
            await Clients.Caller.SendAsync("ReceiveMessage", session.Id, agentMessage.Id, "You", message, agentMessage.CreatedAt);
        }
    }

    public async Task SendMessageToAgent(Guid sessionId, string message)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return;

        var session = await _db.ChatSessions.FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);
        if (session != null && session.Status == ChatSessionStatus.ActiveWithAgent)
        {
            var userMessage = new ChatMessage
            {
                ChatSessionId = session.Id,
                Content = message,
                SenderRole = ChatMessageSenderRole.User
            };
            _db.ChatMessages.Add(userMessage);
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

        var session = await _db.ChatSessions.FirstOrDefaultAsync(s => s.Id == sessionId);
        if (session == null) return;

        // Either the owner user or the assigned agent can end it
        if (session.UserId == userId || session.AssignedAgentId == userId)
        {
            session.Status = ChatSessionStatus.Resolved;
            await _db.SaveChangesAsync();

            await Clients.Group($"User_{session.UserId}").SendAsync("SessionEnded", session.Id);
            await Clients.Group($"Session_{session.Id}").SendAsync("SessionEnded", session.Id);
        }
    }
}
