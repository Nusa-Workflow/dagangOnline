using dagangOnline.Authorization;
using dagangOnline.Models.Agents;
using dagangOnline.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace dagangOnline.Pages.Agent;

[Authorize(Policy = AuthorizationPolicies.RequireAgentOrAdmin)]
public class IndexModel : PageModel
{
    private readonly AgentReviewService _agentReviewService;

    public IndexModel(AgentReviewService agentReviewService)
    {
        _agentReviewService = agentReviewService;
    }

    public List<ReviewQueueItemDto> QueueItems { get; set; } = new();

    public async Task OnGetAsync()
    {
        QueueItems = await _agentReviewService.GetPendingTasksAsync();
    }
}
