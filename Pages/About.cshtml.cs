using dagangOnline.Data;
using dagangOnline.Domain;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace dagangOnline.Pages;

public class AboutModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public AboutModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public ContentPage? AboutPage { get; set; }

    public async Task OnGetAsync()
    {
        AboutPage = await _context.ContentPages
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Slug == "about" && x.Status == PublicationStatus.Published);
    }
}
