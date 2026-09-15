using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using dagangOnline.Authorization;
using dagangOnline.Domain.Agents;
using dagangOnline.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace dagangOnline.Pages.Agent;

[Authorize(Policy = AuthorizationPolicies.RequireAgentOrAdmin)]
public class ReviewModel : PageModel
{
    private readonly AgentReviewService _agentReviewService;

    public ReviewModel(AgentReviewService agentReviewService)
    {
        _agentReviewService = agentReviewService;
    }

    public ReviewTask? TaskItem { get; set; }

    [BindProperty]
    public Guid TaskId { get; set; }

    [BindProperty]
    public string? ApprovalNotes { get; set; }

    [BindProperty]
    [Required(ErrorMessage = "Alasan penolakan wajib diisi agar Mitra dapat melakukan revisi.")]
    public string RejectReason { get; set; } = string.Empty;

    [BindProperty]
    public string? RejectNotes { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        TaskId = id;
        TaskItem = await _agentReviewService.GetTaskByIdAsync(id);
        if (TaskItem == null)
        {
            return NotFound();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostApproveAsync()
    {
        var reviewerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "agent-system";
        var reviewerName = User.Identity?.Name ?? "Human Agent";

        var success = await _agentReviewService.ApproveProductAsync(TaskId, reviewerId, reviewerName, ApprovalNotes);
        if (!success)
        {
            TempData["ErrorMessage"] = "Gagal memproses persetujuan produk. Data tidak ditemukan.";
            return RedirectToPage("/Agent/Index");
        }

        TempData["StatusMessage"] = "Produk berhasil disetujui dan kini telah tayang di katalog platform.";
        return RedirectToPage("/Agent/Index");
    }

    public async Task<IActionResult> OnPostRejectAsync()
    {
        if (string.IsNullOrWhiteSpace(RejectReason))
        {
            ModelState.AddModelError(nameof(RejectReason), "Alasan penolakan wajib diisi.");
            TaskItem = await _agentReviewService.GetTaskByIdAsync(TaskId);
            return Page();
        }

        var reviewerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "agent-system";
        var reviewerName = User.Identity?.Name ?? "Human Agent";

        var success = await _agentReviewService.RejectProductAsync(TaskId, reviewerId, reviewerName, RejectReason, RejectNotes);
        if (!success)
        {
            TempData["ErrorMessage"] = "Gagal memproses penolakan produk. Data tidak ditemukan.";
            return RedirectToPage("/Agent/Index");
        }

        TempData["StatusMessage"] = "Pengajuan produk telah ditolak dan notifikasi perbaikan telah dikirim ke Mitra.";
        return RedirectToPage("/Agent/Index");
    }
}
