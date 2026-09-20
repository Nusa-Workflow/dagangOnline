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

        var metaTags = new List<string>();
        if (!string.IsNullOrWhiteSpace(NewRequest.BpsSector)) metaTags.Add($"Sektor BPS: {NewRequest.BpsSector}");
        if (!string.IsNullOrWhiteSpace(NewRequest.DesilCategory)) metaTags.Add($"Desil 2026: {NewRequest.DesilCategory}");

        var finalDescription = metaTags.Any()
            ? $"[{string.Join(" | ", metaTags)}]\n\n{NewRequest.Description}"
            : NewRequest.Description;

        var request = new ServiceRequest
        {
            Title = NewRequest.Title,
            Description = finalDescription,
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

        await EnsureBpsServicesSeededAsync();

        AvailableServices = await _context.Services
            .AsNoTracking()
            .Where(x => x.Status == PublicationStatus.Published)
            .OrderBy(x => x.Name)
            .ToListAsync();
    }

    private async Task EnsureBpsServicesSeededAsync()
    {
        if (await _context.Services.AnyAsync()) return;

        var bpsServices = new List<Service>
        {
            new Service
            {
                Name = "Perdagangan Eceran & Grosir Modern (KBLI Kategori G)",
                Slug = "perdagangan-eceran-grosir-kbli-g",
                Summary = "Solusi POS kasir pintar, toko online, dan manajemen stok retail/kelontong.",
                Description = "Standar Klasifikasi Baku Lapangan Usaha Indonesia (BPS KBLI Kategori G) untuk perdagangan besar dan eceran. Dilengkapi integrasi QRIS, katalog digital, dan pencatatan kas harian.",
                StartingPrice = 2500000m,
                Status = PublicationStatus.Published,
                IsFeatured = true
            },
            new Service
            {
                Name = "Kuliner, Warung Makan & Minum (KBLI Kategori I)",
                Slug = "kuliner-makan-minum-kbli-i",
                Summary = "Aplikasi pemesanan QR meja, order delivery, dan manajemen bahan baku F&B.",
                Description = "Standar Klasifikasi BPS Kategori I untuk penyediaan akomodasi dan makan minum. Menghubungkan pesanan pelanggan langsung ke dapur dan pembukuan omset harian.",
                StartingPrice = 1500000m,
                Status = PublicationStatus.Published,
                IsFeatured = true
            },
            new Service
            {
                Name = "Industri Pengolahan & Manufaktur Kreatif (KBLI Kategori C)",
                Slug = "industri-pengolahan-manufaktur-kbli-c",
                Summary = "Standarisasi BPOM/Halal, kemasan produk, dan digitalisasi rantai pasok industri olahan.",
                Description = "Standar Klasifikasi BPS Kategori C untuk industri pengolahan makanan kemasan, konveksi/busana, kriya, dan herbal tradisional.",
                StartingPrice = 3500000m,
                Status = PublicationStatus.Published
            },
            new Service
            {
                Name = "Pertanian Presisi, Peternakan & Perikanan (KBLI Kategori A)",
                Slug = "pertanian-perikanan-kbli-a",
                Summary = "Digitalisasi agribisnis, sensor IoT tanah/cuaca, dan akses pasar langsung (D2C).",
                Description = "Standar Klasifikasi BPS Kategori A untuk sektor pertanian, perkebunan, peternakan, dan perikanan nusantara.",
                StartingPrice = 4000000m,
                Status = PublicationStatus.Published
            },
            new Service
            {
                Name = "Transportasi, Pergudangan & Logistik (KBLI Kategori H)",
                Slug = "logistik-pergudangan-kbli-h",
                Summary = "Manajemen armada, tracking kurir, dan pergudangan mikro antar-pulau & koridor IKN.",
                Description = "Standar Klasifikasi BPS Kategori H untuk transportasi dan pergudangan distribusi logistik rendah emisi.",
                StartingPrice = 5000000m,
                Status = PublicationStatus.Published
            },
            new Service
            {
                Name = "Teknologi Informasi & Agen AI Digital (KBLI Kategori J)",
                Slug = "teknologi-informasi-ai-kbli-j",
                Summary = "Pembuatan website profil, aplikasi kasir cloud, integrasi Voice Agent Nemotron, dan otomasi bisnis.",
                Description = "Standar Klasifikasi BPS Kategori J untuk penyedia jasa teknologi informasi, telekomunikasi, dan transformasi digital UMKM.",
                StartingPrice = 3000000m,
                Status = PublicationStatus.Published,
                IsFeatured = true
            },
            new Service
            {
                Name = "Jasa Bisnis, Legalitas NIB & Konsultasi Finansial (KBLI Kategori M & S)",
                Slug = "jasa-bisnis-legalitas-kbli-m-s",
                Summary = "Pengurusan NIB OSS, sertifikasi halal, pencatatan akuntansi keuangan UMKM, dan perizinan edar.",
                Description = "Standar Klasifikasi BPS Kategori M & S untuk jasa profesional, ilmiah, teknis, dan konsultasi manajemen usaha UMKM.",
                StartingPrice = 1000000m,
                Status = PublicationStatus.Published
            }
        };

        _context.Services.AddRange(bpsServices);
        await _context.SaveChangesAsync();
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

        public string? BpsSector { get; set; }

        public string? DesilCategory { get; set; }

        public string? BudgetRange { get; set; }

        [Phone(ErrorMessage = "Format nomor telepon tidak valid.")]
        public string? Phone { get; set; }
    }
}
