using System.ComponentModel.DataAnnotations;
using dagangOnline.Authorization;
using dagangOnline.Data;
using dagangOnline.Domain;
using dagangOnline.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace dagangOnline.Pages.Account;

[AllowAnonymous]
public class RegisterModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;

    public RegisterModel(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _context = context;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Role { get; set; } = "user";

    public class InputModel
    {
        [Required(ErrorMessage = "Nama lengkap wajib diisi.")]
        [StringLength(100, ErrorMessage = "Maksimal 100 karakter.")]
        public string DisplayName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Alamat email wajib diisi.")]
        [EmailAddress(ErrorMessage = "Format email tidak valid.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password wajib diisi.")]
        [StringLength(100, ErrorMessage = "Password minimal {2} karakter.", MinimumLength = 8)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Password dan konfirmasi password tidak cocok.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        public string AccountType { get; set; } = "user"; // "user" or "mitra"

        // Mitra / Partnership specific fields
        public string? BusinessName { get; set; }
        public string? BusinessType { get; set; }
        public string? City { get; set; }
        public string? Description { get; set; }
    }

    public void OnGet(string? role = null)
    {
        if (!string.IsNullOrWhiteSpace(role) && role.ToLowerInvariant() == "mitra")
        {
            Role = "mitra";
            Input.AccountType = "mitra";
        }
        else
        {
            Role = "user";
            Input.AccountType = "user";
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Input.AccountType == "mitra" && string.IsNullOrWhiteSpace(Input.BusinessName))
        {
            ModelState.AddModelError("Input.BusinessName", "Nama bisnis/perusahaan wajib diisi untuk pendaftaran mitra.");
        }

        if (ModelState.IsValid)
        {
            var user = new ApplicationUser
            {
                UserName = Input.Email,
                Email = Input.Email,
                DisplayName = Input.DisplayName,
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, Input.Password);
            if (result.Succeeded)
            {
                if (Input.AccountType == "mitra")
                {
                    await _userManager.AddToRoleAsync(user, RoleConstants.Mitra);

                    var mitraProfile = new MitraProfile
                    {
                        UserId = user.Id,
                        BusinessName = Input.BusinessName ?? Input.DisplayName,
                        BusinessType = Input.BusinessType,
                        City = Input.City,
                        Description = Input.Description,
                        IsVerified = false,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.MitraProfiles.Add(mitraProfile);
                    await _context.SaveChangesAsync();

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToPage("/Dashboard/Mitra/Index");
                }
                else
                {
                    await _userManager.AddToRoleAsync(user, RoleConstants.User);

                    var userProfile = new UserProfile
                    {
                        UserId = user.Id,
                        DisplayName = Input.DisplayName,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.UserProfiles.Add(userProfile);
                    await _context.SaveChangesAsync();

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToPage("/Dashboard/User/Index");
                }
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        return Page();
    }
}
