using dagangOnline.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace dagangOnline.Pages.Account;

[Authorize]
public class ProfileModel : PageModel
{
    private readonly UserManager<ApplicationUser> userManager;

    public ProfileModel(UserManager<ApplicationUser> userManager)
    {
        this.userManager = userManager;
    }

    [BindProperty]
    public ProfileInput Input { get; set; } = new();

    public string Email { get; private set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge();
        }

        LoadProfile(user);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            Email = user.Email ?? string.Empty;
            return Page();
        }

        user.DisplayName = Input.DisplayName.Trim();
        user.Bio = string.IsNullOrWhiteSpace(Input.Bio) ? null : Input.Bio.Trim();
        user.UpdatedAt = DateTime.UtcNow;

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            Email = user.Email ?? string.Empty;
            return Page();
        }

        TempData["ProfileMessage"] = "Profile berhasil diperbarui.";
        return RedirectToPage();
    }

    private void LoadProfile(ApplicationUser user)
    {
        Email = user.Email ?? string.Empty;
        Input = new ProfileInput
        {
            DisplayName = user.DisplayName,
            Bio = user.Bio
        };
    }

    public sealed class ProfileInput
    {
        [Required(ErrorMessage = "Nama tampilan wajib diisi.")]
        [StringLength(100, ErrorMessage = "Nama tampilan maksimal 100 karakter.")]
        [Display(Name = "Nama tampilan")]
        public string DisplayName { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Bio maksimal 500 karakter.")]
        [Display(Name = "Bio")]
        public string? Bio { get; set; }
    }
}
