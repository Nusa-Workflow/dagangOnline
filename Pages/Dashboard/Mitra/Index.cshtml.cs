using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using dagangOnline.Authorization;
using dagangOnline.Data;
using dagangOnline.Domain;
using dagangOnline.Models;
using dagangOnline.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace dagangOnline.Pages.Dashboard.Mitra;

[Authorize(Policy = AuthorizationPolicies.RequireMitra)]
public class IndexModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly AgentReviewService _agentReviewService;

    public IndexModel(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        AgentReviewService agentReviewService)
    {
        _userManager = userManager;
        _context = context;
        _agentReviewService = agentReviewService;
    }

    public string DisplayName { get; private set; } = string.Empty;
    public MitraProfile? Profile { get; set; }
    public List<Product> Products { get; set; } = new();
    public List<ServiceRequest> AvailableRequests { get; set; } = new();
    public List<Notification> Notifications { get; set; } = new();

    [BindProperty]
    public MitraProfileInput ProfileInput { get; set; } = new();

    [BindProperty]
    public CreateProductInput NewProduct { get; set; } = new();

    [BindProperty]
    public UpdateProductInput EditProduct { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        await LoadMitraDataAsync();
    }

    public async Task<IActionResult> OnPostUpdateProfileAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var profile = await _context.MitraProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
        if (profile == null)
        {
            profile = new MitraProfile
            {
                UserId = user.Id,
                BusinessName = ProfileInput.BusinessName,
                BusinessType = ProfileInput.BusinessType,
                Description = ProfileInput.Description,
                WebsiteUrl = ProfileInput.WebsiteUrl,
                Address = ProfileInput.Address,
                City = ProfileInput.City,
                Region = ProfileInput.Region,
                IsVerified = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.MitraProfiles.Add(profile);
        }
        else
        {
            profile.BusinessName = ProfileInput.BusinessName;
            profile.BusinessType = ProfileInput.BusinessType;
            profile.Description = ProfileInput.Description;
            profile.WebsiteUrl = ProfileInput.WebsiteUrl;
            profile.Address = ProfileInput.Address;
            profile.City = ProfileInput.City;
            profile.Region = ProfileInput.Region;
            profile.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        StatusMessage = "Profil bisnis mitra berhasil diperbarui!";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCreateProductAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        if (string.IsNullOrWhiteSpace(NewProduct.Name))
        {
            ErrorMessage = "Nama produk wajib diisi.";
            return RedirectToPage();
        }

        var slug = Regex.Replace(NewProduct.Name.ToLowerInvariant(), @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-").Trim('-');
        var uniqueSlug = $"{slug}-{Guid.NewGuid().ToString().Substring(0, 6)}";

        var profile = await _context.MitraProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
        var ownerName = profile?.BusinessName ?? user.DisplayName ?? user.UserName ?? "Mitra";

        var product = new Product
        {
            Name = NewProduct.Name,
            Slug = uniqueSlug,
            Summary = NewProduct.Summary ?? string.Empty,
            Description = NewProduct.Description ?? string.Empty,
            Price = NewProduct.Price,
            Category = NewProduct.Category,
            Status = PublicationStatus.PendingReview, // Produk mitra masuk status menunggu review (Task 1.8)
            OwnerId = user.Id,
            OwnerName = ownerName,
            IsFeatured = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Enqueue to Human Agent Review queue
        await _agentReviewService.CreateReviewTaskForProductAsync(product);

        StatusMessage = $"Produk/Layanan '{product.Name}' berhasil didaftarkan dan menunggu review kurator internal!";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUpdateProductAsync(Guid id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (product == null)
        {
            ErrorMessage = "Produk tidak ditemukan.";
            return RedirectToPage();
        }

        // Strict backend ownership verification (Task 1.8)
        if (product.OwnerId != user.Id && !User.IsInRole(RoleConstants.Admin))
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(EditProduct.Name))
        {
            ErrorMessage = "Nama produk wajib diisi.";
            return RedirectToPage();
        }

        product.Name = EditProduct.Name;
        product.Summary = EditProduct.Summary ?? string.Empty;
        product.Description = EditProduct.Description ?? string.Empty;
        product.Price = EditProduct.Price;
        product.Category = EditProduct.Category;
        product.Status = PublicationStatus.PendingReview; // Re-enters review queue after edits
        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await _agentReviewService.CreateReviewTaskForProductAsync(product);

        StatusMessage = $"Produk '{product.Name}' berhasil diperbarui dan diajukan ulang ke antrean review!";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteProductAsync(Guid id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (product == null)
        {
            ErrorMessage = "Produk tidak ditemukan.";
            return RedirectToPage();
        }

        // Strict backend ownership verification (Task 1.8)
        if (product.OwnerId != user.Id && !User.IsInRole(RoleConstants.Admin))
        {
            return Forbid();
        }

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        StatusMessage = $"Produk '{product.Name}' berhasil dihapus.";
        return RedirectToPage();
    }

    private async Task LoadMitraDataAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return;

        DisplayName = user.DisplayName ?? user.UserName ?? "Mitra";
        Profile = await _context.MitraProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);

        if (Profile != null)
        {
            ProfileInput.BusinessName = Profile.BusinessName;
            ProfileInput.BusinessType = Profile.BusinessType;
            ProfileInput.Description = Profile.Description;
            ProfileInput.WebsiteUrl = Profile.WebsiteUrl;
            ProfileInput.Address = Profile.Address;
            ProfileInput.City = Profile.City;
            ProfileInput.Region = Profile.Region;
        }
        else
        {
            ProfileInput.BusinessName = DisplayName;
        }

        // Query only products owned by this Mitra (Task 1.8)
        Products = await _context.Products
            .AsNoTracking()
            .Where(p => p.OwnerId == user.Id)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        AvailableRequests = await _context.ServiceRequests
            .Include(r => r.Service)
            .AsNoTracking()
            .Where(r => r.Status == "New" || r.Status == "In Progress")
            .OrderByDescending(r => r.CreatedAt)
            .Take(15)
            .ToListAsync();

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            Notifications = await _context.Notifications
                .AsNoTracking()
                .Where(n => n.RecipientEmail == user.Email)
                .OrderByDescending(n => n.CreatedAt)
                .Take(10)
                .ToListAsync();
        }
    }

    public class MitraProfileInput
    {
        [Required(ErrorMessage = "Nama bisnis/organisasi wajib diisi.")]
        public string BusinessName { get; set; } = string.Empty;

        public string? BusinessType { get; set; }
        public string? Description { get; set; }
        public string? WebsiteUrl { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Region { get; set; }
    }

    public class CreateProductInput
    {
        [Required(ErrorMessage = "Nama produk wajib diisi.")]
        public string Name { get; set; } = string.Empty;

        public string? Summary { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public ProductCategory Category { get; set; } = ProductCategory.General;
    }

    public class UpdateProductInput
    {
        [Required(ErrorMessage = "Nama produk wajib diisi.")]
        public string Name { get; set; } = string.Empty;

        public string? Summary { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public ProductCategory Category { get; set; } = ProductCategory.General;
    }
}
