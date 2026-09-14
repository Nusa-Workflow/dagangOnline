using System.ComponentModel.DataAnnotations;
using dagangOnline.Authorization;
using dagangOnline.Data;
using dagangOnline.Domain;
using dagangOnline.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace dagangOnline.Pages.Dashboard.User;

[Authorize(Policy = AuthorizationPolicies.RequireUser)]
public class IndexModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;

    public IndexModel(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    public string DisplayName { get; private set; } = string.Empty;
    public string UserEmail { get; private set; } = string.Empty;
    public DateTime MemberSince { get; private set; }

    public List<ServiceRequest> ServiceRequests { get; set; } = new();
    public List<Notification> Notifications { get; set; } = new();
    public List<ContactInquiry> ContactInquiries { get; set; } = new();
    public List<Service> AvailableServices { get; set; } = new();

    public int ActiveRequestsCount => ServiceRequests.Count(x => x.Status != "Completed" && x.Status != "Cancelled");
    public int UnreadNotificationsCount => Notifications.Count(x => !x.IsRead);

    [BindProperty]
    public CreateServiceRequestInput NewRequest { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync()
    {
        await LoadUserDataAsync();
    }

    public async Task<IActionResult> OnPostCreateRequestAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            await LoadUserDataAsync();
            return Page();
        }

        Guid? parsedUserId = Guid.TryParse(user.Id, out var uid) ? uid : null;

        var request = new ServiceRequest
        {
            Title = NewRequest.Title,
            Description = NewRequest.Description,
            ClientName = user.DisplayName ?? user.UserName,
            ClientEmail = user.Email,
            ClientPhone = NewRequest.Phone,
            BudgetRange = NewRequest.BudgetRange,
            ServiceId = NewRequest.ServiceId,
            UserId = parsedUserId,
            UserName = user.UserName,
            Status = "New",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.ServiceRequests.Add(request);

        // Also create a confirmation notification for the user
        _context.Notifications.Add(new Notification
        {
            Title = "Permintaan Layanan Diajukan",
            Message = $"Permintaan '{request.Title}' telah berhasil diterima dan sedang ditinjau tim kami.",
            Type = "Success",
            IsRead = false,
            RecipientUserId = user.Id,
            RecipientEmail = user.Email,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        StatusMessage = "Permintaan layanan baru berhasil diajukan! Tim kami akan segera meninjau detailnya.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostMarkNotificationReadAsync(Guid notificationId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge();
        }

        var notif = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && (n.RecipientUserId == user.Id || n.RecipientEmail == user.Email));

        if (notif != null && !notif.IsRead)
        {
            notif.IsRead = true;
            notif.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return RedirectToPage();
    }

    private async Task LoadUserDataAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return;

        DisplayName = user.DisplayName ?? user.UserName ?? "Pengguna";
        UserEmail = user.Email ?? string.Empty;
        MemberSince = user.CreatedAt;

        Guid? parsedUserId = Guid.TryParse(user.Id, out var uid) ? uid : null;

        ServiceRequests = await _context.ServiceRequests
            .Include(x => x.Service)
            .AsNoTracking()
            .Where(x => (parsedUserId.HasValue && x.UserId == parsedUserId) || x.ClientEmail == user.Email)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        Notifications = await _context.Notifications
            .AsNoTracking()
            .Where(x => x.RecipientUserId == user.Id || x.RecipientEmail == user.Email)
            .OrderByDescending(x => x.CreatedAt)
            .Take(20)
            .ToListAsync();

        ContactInquiries = await _context.ContactInquiries
            .AsNoTracking()
            .Where(x => x.Email == user.Email)
            .OrderByDescending(x => x.CreatedAt)
            .Take(15)
            .ToListAsync();

        AvailableServices = await _context.Services
            .AsNoTracking()
            .Where(x => x.Status == PublicationStatus.Published)
            .OrderBy(x => x.Name)
            .ToListAsync();
    }

    public class CreateServiceRequestInput
    {
        [Required(ErrorMessage = "Judul permintaan wajib diisi.")]
        [StringLength(200, ErrorMessage = "Maksimal 200 karakter.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Deskripsi kebutuhan wajib diisi.")]
        [StringLength(2000, ErrorMessage = "Maksimal 2000 karakter.")]
        public string Description { get; set; } = string.Empty;

        public Guid? ServiceId { get; set; }

        public string? BudgetRange { get; set; }

        [Phone(ErrorMessage = "Format nomor telepon tidak valid.")]
        public string? Phone { get; set; }
    }
}
