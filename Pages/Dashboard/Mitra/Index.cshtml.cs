using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using dagangOnline.Authorization;
using dagangOnline.Data;
using dagangOnline.Domain;
using dagangOnline.Models;
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

    public IndexModel(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    public string DisplayName { get; private set; } = string.Empty;
    public MitraProfile? Profile { get; set; }
    public List<Product> Products { get; set; } = new();
    public List<ServiceRequest> AvailableRequests { get; set; } = new();

    [BindProperty]
    public MitraProfileInput ProfileInput { get; set; } = new();

    [BindProperty]
    public CreateProductInput NewProduct { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

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
            StatusMessage = "Nama produk wajib diisi.";
            return RedirectToPage();
        }

        var slug = Regex.Replace(NewProduct.Name.ToLowerInvariant(), @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-").Trim('-');
        var uniqueSlug = $"{slug}-{Guid.NewGuid().ToString().Substring(0, 6)}";

        var product = new Product
        {
            Name = NewProduct.Name,
            Slug = uniqueSlug,
            Summary = NewProduct.Summary ?? string.Empty,
            Description = NewProduct.Description ?? string.Empty,
            Price = NewProduct.Price,
            Category = NewProduct.Category,
            Status = PublicationStatus.Published,
            IsFeatured = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        StatusMessage = $"Produk/Layanan '{product.Name}' berhasil ditambahkan ke katalog platform!";
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

        Products = await _context.Products
            .AsNoTracking()
            .OrderByDescending(p => p.CreatedAt)
            .Take(25)
            .ToListAsync();

        AvailableRequests = await _context.ServiceRequests
            .Include(r => r.Service)
            .AsNoTracking()
            .Where(r => r.Status == "New" || r.Status == "In Progress")
            .OrderByDescending(r => r.CreatedAt)
            .Take(15)
            .ToListAsync();
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
}
