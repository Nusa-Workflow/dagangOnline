using System.ComponentModel.DataAnnotations;
using dagangOnline.Authorization;
using dagangOnline.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace dagangOnline.Pages.Account;

[AllowAnonymous]
public class LoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public LoginModel(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Email wajib diisi.")]
        [EmailAddress(ErrorMessage = "Format email tidak valid.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password wajib diisi.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Ingat Saya")]
        public bool RememberMe { get; set; }
    }

    public async Task OnGetAsync(string? returnUrl = null)
    {
        if (!string.IsNullOrEmpty(ErrorMessage))
        {
            ModelState.AddModelError(string.Empty, ErrorMessage);
        }

        returnUrl ??= Url.Content("~/");
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        ReturnUrl = returnUrl;
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");

        if (ModelState.IsValid)
        {
            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user != null && !user.IsActive)
            {
                ModelState.AddModelError(string.Empty, "Akun Anda sedang dinonaktifkan oleh administrator.");
                return Page();
            }

            var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                if (user != null)
                {
                    if (await _userManager.IsInRoleAsync(user, RoleConstants.Admin))
                    {
                        return RedirectToPage("/Admin/Dashboard/Index");
                    }
                    if (await _userManager.IsInRoleAsync(user, RoleConstants.Mitra))
                    {
                        return RedirectToPage("/Dashboard/Mitra/Index");
                    }
                    if (await _userManager.IsInRoleAsync(user, RoleConstants.User))
                    {
                        return RedirectToPage("/Dashboard/User/Index");
                    }
                }

                return LocalRedirect(returnUrl);
            }

            ModelState.AddModelError(string.Empty, "Kombinasi email atau password tidak valid.");
        }

        return Page();
    }
}
