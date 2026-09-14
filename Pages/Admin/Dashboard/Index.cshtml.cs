using System.ComponentModel.DataAnnotations;
using dagangOnline.Application.Services;
using dagangOnline.Authorization;
using dagangOnline.Data;
using dagangOnline.Domain;
using dagangOnline.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace dagangOnline.Pages.Admin.Dashboard;

[Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
public class IndexModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly AuditLogService _auditLogService;

    public IndexModel(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        AuditLogService auditLogService)
    {
        _userManager = userManager;
        _context = context;
        _auditLogService = auditLogService;
    }

    public List<UserViewModel> Users { get; set; } = new();
    public List<AuditLog> AuditLogs { get; set; } = new();
    public List<ContactInquiry> Inquiries { get; set; } = new();
    public List<ServiceRequest> ServiceRequests { get; set; } = new();
    public List<Announcement> Announcements { get; set; } = new();

    public int TotalUsersCount { get; set; }
    public int TotalMitraCount { get; set; }
    public int TotalInquiriesCount { get; set; }
    public int TotalAuditLogsCount { get; set; }

    [BindProperty]
    public CreateAnnouncementInput NewAnnouncement { get; set; } = new();

    [BindProperty]
    public AboutPageInput AboutInput { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync()
    {
        await LoadAdminDataAsync();
    }

    public async Task<IActionResult> OnPostUpdateInquiryStatusAsync(Guid id, InquiryStatus status)
    {
        var inquiry = await _context.ContactInquiries.FindAsync(id);
        if (inquiry != null)
        {
            var oldStatus = inquiry.Status;
            inquiry.Status = status;
            inquiry.UpdatedAt = DateTime.UtcNow;

            await _auditLogService.LogAsync(
                action: "UpdateInquiryStatus",
                entityName: "ContactInquiry",
                entityId: id.ToString(),
                performedByUserId: _userManager.GetUserId(User),
                performedByUserName: User.Identity?.Name,
                details: $"Changed status from {oldStatus} to {status}"
            );

            await _context.SaveChangesAsync();
            StatusMessage = $"Status inquiri '{inquiry.Subject}' berhasil diubah ke {status}.";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUpdateServiceRequestStatusAsync(Guid id, string status)
    {
        var req = await _context.ServiceRequests.FindAsync(id);
        if (req != null)
        {
            var oldStatus = req.Status;
            req.Status = status;
            req.UpdatedAt = DateTime.UtcNow;

            await _auditLogService.LogAsync(
                action: "UpdateServiceRequestStatus",
                entityName: "ServiceRequest",
                entityId: id.ToString(),
                performedByUserId: _userManager.GetUserId(User),
                performedByUserName: User.Identity?.Name,
                details: $"Changed service request status from {oldStatus} to {status}"
            );

            await _context.SaveChangesAsync();
            StatusMessage = $"Status permintaan layanan '{req.Title}' berhasil diubah ke {status}.";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleUserActiveAsync(string userId)
    {
        var targetUser = await _userManager.FindByIdAsync(userId);
        if (targetUser != null)
        {
            targetUser.IsActive = !targetUser.IsActive;
            targetUser.UpdatedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(targetUser);

            await _auditLogService.LogWarningAsync(
                action: "ToggleUserStatus",
                entityName: "ApplicationUser",
                entityId: userId,
                performedByUserId: _userManager.GetUserId(User),
                performedByUserName: User.Identity?.Name,
                details: $"Set user {targetUser.Email} IsActive = {targetUser.IsActive}"
            );

            StatusMessage = $"Status user {targetUser.Email} berhasil diubah menjadi {(targetUser.IsActive ? "Aktif" : "Non-aktif")}.";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCreateAnnouncementAsync()
    {
        if (string.IsNullOrWhiteSpace(NewAnnouncement.Title))
        {
            StatusMessage = "Judul pengumuman wajib diisi.";
            return RedirectToPage();
        }

        var announcement = new Announcement
        {
            Title = NewAnnouncement.Title,
            Content = NewAnnouncement.Content ?? string.Empty,
            Status = PublicationStatus.Published,
            PublishedAt = DateTime.UtcNow,
            IsPinned = NewAnnouncement.IsPinned,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Announcements.Add(announcement);

        await _auditLogService.LogAsync(
            action: "CreateAnnouncement",
            entityName: "Announcement",
            performedByUserId: _userManager.GetUserId(User),
            performedByUserName: User.Identity?.Name,
            details: $"Published announcement '{announcement.Title}'"
        );

        await _context.SaveChangesAsync();
        StatusMessage = "Pengumuman baru berhasil diterbitkan!";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSaveAboutPageAsync()
    {
        if (string.IsNullOrWhiteSpace(AboutInput.Title))
        {
            StatusMessage = "Judul halaman About wajib diisi.";
            return RedirectToPage();
        }

        var about = await _context.ContentPages.FirstOrDefaultAsync(x => x.Slug == "about");
        if (about == null)
        {
            about = new ContentPage
            {
                Slug = "about",
                Title = AboutInput.Title,
                MetaDescription = AboutInput.MetaDescription ?? string.Empty,
                Content = AboutInput.Content ?? string.Empty,
                Status = PublicationStatus.Published,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.ContentPages.Add(about);
        }
        else
        {
            about.Title = AboutInput.Title;
            about.MetaDescription = AboutInput.MetaDescription ?? string.Empty;
            about.Content = AboutInput.Content ?? string.Empty;
            about.Status = PublicationStatus.Published;
            about.UpdatedAt = DateTime.UtcNow;
        }

        await _auditLogService.LogAsync(
            action: "UpdateAboutPage",
            entityName: "ContentPage",
            entityId: about.Id.ToString(),
            performedByUserId: _userManager.GetUserId(User),
            performedByUserName: User.Identity?.Name,
            details: $"Updated About page content (Title: {about.Title})"
        );

        await _context.SaveChangesAsync();
        StatusMessage = "Halaman Tentang Kami (About) berhasil diperbarui dan dipublikasikan!";
        return RedirectToPage();
    }

    private async Task LoadAdminDataAsync()
    {
        TotalUsersCount = await _userManager.Users.CountAsync();
        TotalMitraCount = await _context.MitraProfiles.CountAsync();
        TotalInquiriesCount = await _context.ContactInquiries.CountAsync();
        TotalAuditLogsCount = await _context.AuditLogs.CountAsync();

        var usersList = await _userManager.Users.OrderByDescending(u => u.CreatedAt).Take(20).ToListAsync();
        Users = new List<UserViewModel>();
        foreach (var u in usersList)
        {
            var roles = await _userManager.GetRolesAsync(u);
            Users.Add(new UserViewModel
            {
                Id = u.Id,
                Email = u.Email ?? u.UserName ?? "-",
                DisplayName = u.DisplayName ?? u.UserName ?? "-",
                Roles = string.Join(", ", roles),
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt
            });
        }

        AuditLogs = await _context.AuditLogs
            .AsNoTracking()
            .OrderByDescending(a => a.CreatedAt)
            .Take(30)
            .ToListAsync();

        Inquiries = await _context.ContactInquiries
            .AsNoTracking()
            .OrderByDescending(i => i.CreatedAt)
            .Take(25)
            .ToListAsync();

        ServiceRequests = await _context.ServiceRequests
            .Include(s => s.Service)
            .AsNoTracking()
            .OrderByDescending(s => s.CreatedAt)
            .Take(25)
            .ToListAsync();

        Announcements = await _context.Announcements
            .AsNoTracking()
            .OrderByDescending(a => a.CreatedAt)
            .Take(10)
            .ToListAsync();

        var aboutPage = await _context.ContentPages.AsNoTracking().FirstOrDefaultAsync(x => x.Slug == "about");
        if (aboutPage != null)
        {
            AboutInput = new AboutPageInput
            {
                Title = aboutPage.Title,
                MetaDescription = aboutPage.MetaDescription,
                Content = aboutPage.Content
            };
        }
        else
        {
            AboutInput = new AboutPageInput
            {
                Title = "Transformasi Digital untuk Masa Depan Bisnis & UMKM",
                MetaDescription = "dagangOnline adalah platform ekosistem teknologi yang menghubungkan pelaku usaha, mitra profesional, dan solusi perangkat lunak handal dalam satu jaringan terintegrasi.",
                Content = "<p>Selamat datang di platform dagangOnline. Kami hadir untuk memberdayakan bisnis dan UMKM di seluruh Indonesia melalui transformasi digital, infrastruktur cloud modern, dan kolaborasi mitra terpercaya.</p>"
            };
        }
    }

    public class UserViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Roles { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateAnnouncementInput
    {
        [Required]
        public string Title { get; set; } = string.Empty;
        public string? Content { get; set; }
        public bool IsPinned { get; set; }
    }

    public class AboutPageInput
    {
        [Required(ErrorMessage = "Judul halaman wajib diisi")]
        public string Title { get; set; } = string.Empty;

        public string? MetaDescription { get; set; }

        [Required(ErrorMessage = "Konten halaman wajib diisi")]
        public string Content { get; set; } = string.Empty;
    }
}
