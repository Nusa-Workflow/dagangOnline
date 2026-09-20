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
    private readonly dagangOnline.Application.Services.Economic.EconomicMultiAgentSystem _multiAgentSystem;
    private readonly dagangOnline.Application.Interfaces.INemotronVoiceAgentService? _nemotronService;
    private readonly dagangOnline.Application.Interfaces.ICollaborativeAgentOrchestrator? _orchestrator;
    private readonly dagangOnline.Application.Interfaces.INemotronStrategicVoiceAgent? _strategicVoiceAgent;

    public SupportChatHub(
        ApplicationDbContext db,
        AgentAssistService agentAssistService,
        dagangOnline.Application.Services.Economic.EconomicMultiAgentSystem multiAgentSystem,
        dagangOnline.Application.Interfaces.INemotronVoiceAgentService? nemotronService = null,
        dagangOnline.Application.Interfaces.ICollaborativeAgentOrchestrator? orchestrator = null,
        dagangOnline.Application.Interfaces.INemotronStrategicVoiceAgent? strategicVoiceAgent = null)
    {
        _db = db;
        _agentAssistService = agentAssistService;
        _multiAgentSystem = multiAgentSystem;
        _nemotronService = nemotronService;
        _orchestrator = orchestrator;
        _strategicVoiceAgent = strategicVoiceAgent;
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

    public async Task RequestEconomicIntelligence(Guid sessionId, string targetTopic, int horizonMonths = 3)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return;

        var report = await _multiAgentSystem.ExecuteAutonomousWorkflowAsync(new dagangOnline.Domain.Economic.MultiAgentTaskRequestDto
        {
            Query = targetTopic,
            TargetSectorOrCommodity = targetTopic,
            HorizonMonths = horizonMonths
        });

        // Broadcast to session and agents
        var insightMessage = $"📊 [Economic Intelligence]: Proyeksi {report.Forecast.TargetEntityName} berarah {report.Forecast.OverallTrend} ({report.Forecast.ExpectedPercentageChange:+0.0;-0.0}%). Pendorong utama: {report.GraphFeatures.KeyDrivers.FirstOrDefault()?.SourceNodeName ?? "Makroekonomi"}. Rekomendasi: {report.RecommendedActions.FirstOrDefault()}";

        await Clients.Group($"User_{userId}").SendAsync("ReceiveEconomicInsight", sessionId, report);
        await Clients.Group($"Session_{sessionId}").SendAsync("ReceiveEconomicInsight", sessionId, report);
        await Clients.Group("Agents").SendAsync("ReceiveEconomicInsight", sessionId, report);
    }

    /// <summary>
    /// Receives real-time voice audio chunk from client (User or Agent),
    /// performs Nemotron streaming ASR, acoustic telemetry calculation,
    /// and triggers collaborative dual-agent turn when speech completes.
    /// </summary>
    public async Task SendVoiceChunk(Guid sessionId, string audioBase64, string mimeType = "audio/webm", bool isFinal = false)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(audioBase64)) return;

        if (_nemotronService == null) return;

        // Perform fast transcription
        var transcription = await _nemotronService.TranscribeAudioBase64Async(audioBase64, mimeType);

        // Analyze acoustic prosody and emotional telemetry
        var telemetry = await _nemotronService.AnalyzeAcousticStreamAsync(audioBase64, transcription.Transcript);

        // Broadcast live acoustic telemetry to Session and Agents room
        await Clients.Group($"Session_{sessionId}").SendAsync("ReceiveAcousticTelemetry", sessionId, telemetry);
        await Clients.Group("Agents").SendAsync("ReceiveAcousticTelemetry", sessionId, telemetry);

        // Broadcast partial transcript
        await Clients.Group($"Session_{sessionId}").SendAsync("ReceiveVoiceStream", sessionId, transcription.Transcript, isFinal);

        // If turn has ended (silence detected / user finished speaking)
        if (isFinal && !string.IsNullOrWhiteSpace(transcription.Transcript) && _orchestrator != null)
        {
            var collabResult = await _orchestrator.ProcessCollaborativeTurnAsync(new dagangOnline.Application.DTOs.CollaborativeChatRequestDto
            {
                SessionId = sessionId,
                TextPrompt = transcription.Transcript,
                AudioBase64 = audioBase64,
                MimeType = mimeType,
                ReturnAudio = true
            });

            // Save user message to database
            var userMsg = new ConversationMessage
            {
                ConversationId = sessionId,
                Content = $"🎤 {transcription.Transcript}",
                SenderType = SenderType.Customer
            };
            _db.ConversationMessages.Add(userMsg);

            // Save bot response to database
            var botMsg = new ConversationMessage
            {
                ConversationId = sessionId,
                Content = collabResult.SpokenResponse ?? collabResult.GroundedTextResponse,
                SenderType = SenderType.AI
            };
            _db.ConversationMessages.Add(botMsg);
            await _db.SaveChangesAsync();

            // Broadcast back to customer and agent
            await Clients.Group($"User_{userId}").SendAsync("ReceiveCollaborativeVoice", sessionId, collabResult);
            await Clients.Group($"Session_{sessionId}").SendAsync("ReceiveCollaborativeVoice", sessionId, collabResult);
            await Clients.Group("Agents").SendAsync("ReceiveCollaborativeVoice", sessionId, collabResult);
        }
    }

    /// <summary>
    /// Triggers immediate audio cut-off when barge-in is detected or user interrupts AI.
    /// </summary>
    public async Task TriggerBargeIn(Guid sessionId)
    {
        await Clients.Group($"Session_{sessionId}").SendAsync("BargeInInterruption", sessionId);
        await Clients.Group("Agents").SendAsync("BargeInInterruption", sessionId);
    }

    /// <summary>
    /// Allows a human agent to send an AI-synthesized prosodic voice note to customer.
    /// </summary>
    public async Task SendSpokenVoiceNote(Guid sessionId, string text, string? targetLanguage = "id")
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || string.IsNullOrWhiteSpace(text) || _nemotronService == null) return;

        var synth = await _nemotronService.SynthesizeSpokenVoiceAsync(new dagangOnline.Application.DTOs.VoiceSynthesisRequestDto
        {
            Text = text,
            TargetLanguage = targetLanguage ?? "id-ID",
            ReturnAudioStream = true
        });

        var session = await _db.Conversations.FirstOrDefaultAsync(s => s.Id == sessionId);
        if (session == null) return;

        var message = new ConversationMessage
        {
            ConversationId = sessionId,
            Content = $"🗣️ [Voice Note]: {synth.SpokenScript}",
            SenderType = SenderType.HumanAgent
        };
        _db.ConversationMessages.Add(message);
        await _db.SaveChangesAsync();

        await Clients.Group($"User_{session.CustomerId}").SendAsync("ReceiveVoiceNote", sessionId, synth.AudioBase64, synth.SpokenScript, "Agent");
        await Clients.Group($"Session_{sessionId}").SendAsync("ReceiveVoiceNote", sessionId, synth.AudioBase64, synth.SpokenScript, "Agent");
    }

    /// <summary>
    /// Invokes NVIDIA Nemotron Strategic Voice Agent to produce 5-10 year (2026-2036) future prediction
    /// and streams the result with voice briefing audio to agent & customer sessions.
    /// </summary>
    public async Task RequestLongHorizonForecast(Guid sessionId, dagangOnline.Application.DTOs.LongHorizonForecastRequestDto request)
    {
        if (_strategicVoiceAgent == null || request == null) return;

        var forecast = await _strategicVoiceAgent.GenerateStrategicForecastWithVoiceAsync(request);

        // Broadcast to both session group and agents group
        await Clients.Group($"Session_{sessionId}").SendAsync("ReceiveLongHorizonForecast", sessionId, forecast);
        await Clients.Group("Agents").SendAsync("ReceiveLongHorizonForecast", sessionId, forecast);
        await Clients.Caller.SendAsync("ReceiveLongHorizonForecast", sessionId, forecast);
    }
}

