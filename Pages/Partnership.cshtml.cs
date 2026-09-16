using dagangOnline.Application.DTOs;
using dagangOnline.Data;
using dagangOnline.Domain;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace dagangOnline.Pages;

public class PartnershipModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public PartnershipModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public int ActiveMitraCount { get; set; }
    public List<MitraProfile> FeaturedMitra { get; set; } = new();

    public async Task OnGetAsync()
    {
        ActiveMitraCount = await _context.MitraProfiles.CountAsync(m => m.IsVerified);
        FeaturedMitra = await _context.MitraProfiles
            .AsNoTracking()
            .Where(m => m.IsVerified)
            .OrderByDescending(m => m.CreatedAt)
            .Take(6)
            .ToListAsync();
    }
}
