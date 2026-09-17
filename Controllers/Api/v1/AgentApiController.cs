using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using dagangOnline.Application.DTOs;
using dagangOnline.Application.Services;
using dagangOnline.Data;
using dagangOnline.Domain.Chat;
using dagangOnline.Models;

namespace dagangOnline.Controllers.Api.v1;

[ApiController]
[Route("api/v1/agent")]
[Produces("application/json")]
public class AgentApiController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly AgentAssistService _agentAssist;

    public AgentApiController(ApplicationDbContext db, AgentAssistService agentAssist)
    {
        _db = db;
        _agentAssist = agentAssist;
    }

    public class ChatRequest
    {
        public Guid? SessionId { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    [HttpPost("chat")]
    [ProducesResponseType(typeof(ApiResponse<AiSuggestionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(ApiResponse<AiSuggestionDto>.Fail("Pesan tidak boleh kosong."));
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            var guestUser = await _db.Users.FirstOrDefaultAsync(u => u.Email == "guest@dagangonline.local", cancellationToken)
                         ?? await _db.Users.FirstOrDefaultAsync(cancellationToken);
            if (guestUser == null)
            {
                guestUser = new ApplicationUser
                {
                    Id = Guid.NewGuid().ToString(),
                    UserName = "guest@dagangonline.local",
                    NormalizedUserName = "GUEST@DAGANGONLINE.LOCAL",
                    Email = "guest@dagangonline.local",
                    NormalizedEmail = "GUEST@DAGANGONLINE.LOCAL",
                    DisplayName = "Tamu / Guest Visitor",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _db.Users.Add(guestUser);
                await _db.SaveChangesAsync(cancellationToken);
            }
            userId = guestUser.Id;
        }

        Conversation session;
        if (request.SessionId.HasValue && request.SessionId.Value != Guid.Empty)
        {
            var existing = await _db.Conversations.FirstOrDefaultAsync(c => c.Id == request.SessionId.Value, cancellationToken);
            if (existing != null)
            {
                session = existing;
            }
            else
            {
                session = new Conversation { Id = request.SessionId.Value, CustomerId = userId, Status = ConversationStatus.Open };
                _db.Conversations.Add(session);
                await _db.SaveChangesAsync(cancellationToken);
            }
        }
        else
        {
            session = new Conversation { CustomerId = userId, Status = ConversationStatus.Open };
            _db.Conversations.Add(session);
            await _db.SaveChangesAsync(cancellationToken);
        }

        // Save Customer message
        var customerMsg = new ConversationMessage
        {
            ConversationId = session.Id,
            Content = request.Message,
            SenderType = SenderType.Customer,
            CreatedAt = DateTime.UtcNow
        };
        _db.ConversationMessages.Add(customerMsg);

        // Process through RAG + CLM + Grounding pipeline
        var suggestion = await _agentAssist.ProcessCustomerMessageAsync(session, request.Message, cancellationToken);

        // Save Bot message with confidence metadata
        var botMsg = new ConversationMessage
        {
            ConversationId = session.Id,
            Content = suggestion.SuggestedResponse,
            SenderType = SenderType.AI,
            Confidence = (float)suggestion.Confidence,
            Metadata = suggestion.GroundingState,
            CreatedAt = DateTime.UtcNow
        };
        _db.ConversationMessages.Add(botMsg);

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<AiSuggestionDto>.Ok(suggestion));
    }

    [HttpGet("session/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSession(Guid id, CancellationToken cancellationToken)
    {
        var session = await _db.Conversations
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (session == null)
        {
            return NotFound(ApiResponse<object>.Fail("Sesi percakapan tidak ditemukan."));
        }

        return Ok(ApiResponse<object>.Ok(new
        {
            session.Id,
            session.Status,
            session.Priority,
            session.Intent,
            Messages = session.Messages.Select(m => new
            {
                m.Id,
                m.Content,
                Sender = m.SenderType.ToString(),
                m.Confidence,
                m.CreatedAt
            })
        }));
    }
}
