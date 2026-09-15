using dagangOnline.Data;
using dagangOnline.Domain;
using dagangOnline.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace dagangOnline.Pages.Account;

[Authorize]
public class ProfileModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public ProfileModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ApplicationDbContext context,
        IWebHostEnvironment webHostEnvironment)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
        _webHostEnvironment = webHostEnvironment;
    }

    [BindProperty]
    public ProfileInput Input { get; set; } = new();

    public string CurrentEmail { get; private set; } = string.Empty;
    public string? AvatarUrl { get; private set; }
    public string RoleName { get; private set; } = "Pengguna";

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge();
        }

        await LoadProfileAsync(user);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            await LoadProfileAsync(user);
            return Page();
        }

        var newEmail = Input.Email.Trim().ToLowerInvariant();
        if (!string.Equals(user.Email, newEmail, StringComparison.OrdinalIgnoreCase))
        {
            var existingUserWithEmail = await _userManager.FindByEmailAsync(newEmail);
            if (existingUserWithEmail != null && existingUserWithEmail.Id != user.Id)
            {
                ModelState.AddModelError("Input.Email", "Alamat email ini sudah digunakan oleh akun lain.");
                await LoadProfileAsync(user);
                return Page();
            }

            user.Email = newEmail;
            user.NormalizedEmail = _userManager.NormalizeEmail(newEmail);
            user.UserName = newEmail;
            user.NormalizedUserName = _userManager.NormalizeName(newEmail);
        }

        user.DisplayName = Input.DisplayName.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(Input.PhoneNumber) ? null : Input.PhoneNumber.Trim();
        user.Bio = string.IsNullOrWhiteSpace(Input.Bio) ? null : Input.Bio.Trim();
        user.UpdatedAt = DateTime.UtcNow;

        // Handle Avatar File or Cropped Face Adjustment Data
        if (!string.IsNullOrWhiteSpace(Input.CroppedAvatarData))
        {
            var (success, error, newAvatarUrl) = await ProcessCroppedAvatarAsync(user, Input.CroppedAvatarData);
            if (!success)
            {
                ModelState.AddModelError("Input.AvatarFile", error ?? "Gagal memproses gambar avatar.");
                await LoadProfileAsync(user);
                return Page();
            }
            user.AvatarUrl = newAvatarUrl;
        }
        else if (Input.AvatarFile != null && Input.AvatarFile.Length > 0)
        {
            var (success, error, newAvatarUrl) = await ProcessAvatarFileAsync(user, Input.AvatarFile);
            if (!success)
            {
                ModelState.AddModelError("Input.AvatarFile", error ?? "Gagal mengunggah foto profil.");
                await LoadProfileAsync(user);
                return Page();
            }
            user.AvatarUrl = newAvatarUrl;
        }

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            await LoadProfileAsync(user);
            return Page();
        }

        // Sync or Create UserProfile in DB
        var userProfile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
        if (userProfile == null)
        {
            userProfile = new UserProfile
            {
                UserId = user.Id,
                DisplayName = user.DisplayName,
                Bio = user.Bio,
                Location = string.IsNullOrWhiteSpace(Input.Location) ? null : Input.Location.Trim(),
                WebsiteUrl = string.IsNullOrWhiteSpace(Input.WebsiteUrl) ? null : Input.WebsiteUrl.Trim(),
                AvatarUrl = user.AvatarUrl
            };
            _context.UserProfiles.Add(userProfile);
        }
        else
        {
            userProfile.DisplayName = user.DisplayName;
            userProfile.Bio = user.Bio;
            userProfile.Location = string.IsNullOrWhiteSpace(Input.Location) ? null : Input.Location.Trim();
            userProfile.WebsiteUrl = string.IsNullOrWhiteSpace(Input.WebsiteUrl) ? null : Input.WebsiteUrl.Trim();
            userProfile.AvatarUrl = user.AvatarUrl;
            userProfile.UpdatedAt = DateTime.UtcNow;
        }
        await _context.SaveChangesAsync();

        // Refresh sign-in cookie so any email or claims changes apply immediately
        await _signInManager.RefreshSignInAsync(user);

        TempData["ProfileMessage"] = "Profil dan foto berhasil diperbarui.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAvatarAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge();
        }

        if (!string.IsNullOrEmpty(user.AvatarUrl))
        {
            DeleteAvatarPhysicalFile(user.AvatarUrl);
            user.AvatarUrl = null;
            user.UpdatedAt = DateTime.UtcNow;

            await _userManager.UpdateAsync(user);

            var userProfile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (userProfile != null)
            {
                userProfile.AvatarUrl = null;
                userProfile.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            await _signInManager.RefreshSignInAsync(user);
            TempData["ProfileMessage"] = "Foto profil berhasil dihapus.";
        }

        return RedirectToPage();
    }

    private async Task LoadProfileAsync(ApplicationUser user)
    {
        CurrentEmail = user.Email ?? string.Empty;
        AvatarUrl = user.AvatarUrl;

        var userProfile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
        if (string.IsNullOrEmpty(AvatarUrl) && userProfile != null)
        {
            AvatarUrl = userProfile.AvatarUrl;
        }

        var roles = await _userManager.GetRolesAsync(user);
        RoleName = roles.FirstOrDefault() ?? "Pengguna";

        Input.DisplayName = user.DisplayName;
        Input.Email = user.Email ?? string.Empty;
        Input.PhoneNumber = user.PhoneNumber;
        Input.Bio = user.Bio;
        Input.Location = userProfile?.Location;
        Input.WebsiteUrl = userProfile?.WebsiteUrl;
    }

    private async Task<(bool Success, string? Error, string? AvatarUrl)> ProcessAvatarFileAsync(ApplicationUser user, IFormFile file)
    {
        const long maxFileSize = 5 * 1024 * 1024; // 5 MB
        if (file.Length > maxFileSize)
        {
            return (false, "Ukuran file foto maksimal 5 MB.", null);
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".jpg" && ext != ".jpeg" && ext != ".png")
        {
            return (false, "Hanya format .jpg, .jpeg, dan .png yang diperbolehkan.", null);
        }

        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);
        var bytes = memoryStream.ToArray();

        if (!IsValidImageHeader(bytes, ext))
        {
            return (false, "File yang diunggah bukan format gambar yang valid.", null);
        }

        return await SaveAvatarBytesAsync(user, bytes, ext);
    }

    private async Task<(bool Success, string? Error, string? AvatarUrl)> ProcessCroppedAvatarAsync(ApplicationUser user, string base64Data)
    {
        try
        {
            // Expected format: data:image/png;base64,.... or data:image/jpeg;base64,....
            var parts = base64Data.Split(',');
            if (parts.Length != 2)
            {
                return (false, "Format data gambar hasil penyesuaian tidak valid.", null);
            }

            var header = parts[0];
            string ext = ".png";
            if (header.Contains("image/jpeg") || header.Contains("image/jpg"))
            {
                ext = ".jpg";
            }
            else if (!header.Contains("image/png"))
            {
                return (false, "Format gambar penyesuaian harus jpg atau png.", null);
            }

            var bytes = Convert.FromBase64String(parts[1]);
            const long maxFileSize = 5 * 1024 * 1024; // 5 MB
            if (bytes.Length > maxFileSize)
            {
                return (false, "Ukuran gambar hasil penyesuaian melebihi batas 5 MB.", null);
            }

            if (!IsValidImageHeader(bytes, ext))
            {
                return (false, "Data biner gambar tidak valid.", null);
            }

            return await SaveAvatarBytesAsync(user, bytes, ext);
        }
        catch (Exception ex)
        {
            return (false, $"Gagal membaca gambar: {ex.Message}", null);
        }
    }

    private async Task<(bool Success, string? Error, string? AvatarUrl)> SaveAvatarBytesAsync(ApplicationUser user, byte[] bytes, string ext)
    {
        var webRoot = _webHostEnvironment?.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
        {
            webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        }

        var avatarFolder = Path.Combine(webRoot, "uploads", "avatars");
        if (!Directory.Exists(avatarFolder))
        {
            Directory.CreateDirectory(avatarFolder);
        }

        // Delete previous avatar file if exists
        if (!string.IsNullOrEmpty(user.AvatarUrl))
        {
            DeleteAvatarPhysicalFile(user.AvatarUrl);
        }

        var fileName = $"avatar_{user.Id}_{Guid.NewGuid():N}{ext}";
        var filePath = Path.Combine(avatarFolder, fileName);

        await System.IO.File.WriteAllBytesAsync(filePath, bytes);
        var publicUrl = $"/uploads/avatars/{fileName}";

        return (true, null, publicUrl);
    }

    private void DeleteAvatarPhysicalFile(string avatarUrl)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(avatarUrl) || !avatarUrl.StartsWith("/uploads/avatars/"))
            {
                return;
            }

            var webRoot = _webHostEnvironment?.WebRootPath;
            if (string.IsNullOrWhiteSpace(webRoot))
            {
                webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            }

            var fileName = Path.GetFileName(avatarUrl);
            var fullPath = Path.Combine(webRoot, "uploads", "avatars", fileName);

            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }
        catch
        {
            // Suppress IO errors during file cleanup to prevent interrupting the request
        }
    }

    public static bool IsValidImageHeader(byte[] bytes, string ext)
    {
        if (bytes == null || bytes.Length < 4)
        {
            return false;
        }

        ext = ext.ToLowerInvariant();
        if (ext == ".jpg" || ext == ".jpeg")
        {
            // JPEG starts with FF D8 FF
            return bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF;
        }
        else if (ext == ".png")
        {
            // PNG starts with 89 50 4E 47 0D 0A 1A 0A
            if (bytes.Length < 8) return false;
            return bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47 &&
                   bytes[4] == 0x0D && bytes[5] == 0x0A && bytes[6] == 0x1A && bytes[7] == 0x0A;
        }

        return false;
    }

    public sealed class ProfileInput
    {
        [Required(ErrorMessage = "Nama tampilan wajib diisi.")]
        [StringLength(100, ErrorMessage = "Nama tampilan maksimal 100 karakter.")]
        [Display(Name = "Nama Tampilan")]
        public string DisplayName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Alamat email wajib diisi.")]
        [EmailAddress(ErrorMessage = "Format email tidak valid.")]
        [StringLength(150, ErrorMessage = "Email maksimal 150 karakter.")]
        [Display(Name = "Alamat Email")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Format nomor telepon tidak valid.")]
        [StringLength(20, ErrorMessage = "Nomor telepon maksimal 20 karakter.")]
        [Display(Name = "Nomor Telepon")]
        public string? PhoneNumber { get; set; }

        [StringLength(100, ErrorMessage = "Lokasi / Kota maksimal 100 karakter.")]
        [Display(Name = "Lokasi / Kota")]
        public string? Location { get; set; }

        [Url(ErrorMessage = "Format URL website tidak valid. Gunakan format seperti http:// atau https://")]
        [StringLength(200, ErrorMessage = "URL Website maksimal 200 karakter.")]
        [Display(Name = "Website / Tautan")]
        public string? WebsiteUrl { get; set; }

        [StringLength(500, ErrorMessage = "Bio maksimal 500 karakter.")]
        [Display(Name = "Bio / Deskripsi Ringkas")]
        public string? Bio { get; set; }

        [Display(Name = "Unggah Foto Profil")]
        public IFormFile? AvatarFile { get; set; }

        public string? CroppedAvatarData { get; set; }
    }
}
