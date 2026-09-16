using dagangOnline.Application.DTOs;
using System.ComponentModel.DataAnnotations;
using dagangOnline.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;

namespace dagangOnline.Pages;

[EnableRateLimiting("contact")]
public class ContactModel : PageModel
{
    private readonly ContactInquiryService _inquiryService;

    public ContactModel(ContactInquiryService inquiryService)
    {
        _inquiryService = inquiryService;
    }

    [BindProperty]
    public ContactInputModel Input { get; set; } = new();

    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (!Input.ConsentAccepted)
        {
            ModelState.AddModelError("Input.ConsentAccepted", "Anda harus menyetujui kebijakan privasi untuk mengirim pesan.");
            return Page();
        }

        try
        {
            var submission = new ContactInquirySubmission
            {
                Name = Input.Name,
                Email = Input.Email,
                Subject = Input.Subject,
                Message = Input.Message,
                Category = Input.Category,
                ConsentAccepted = Input.ConsentAccepted
            };

            await _inquiryService.SubmitAsync(submission);
            SuccessMessage = "Terima kasih! Pesan dan inquiri Anda telah berhasil dikirim. Tim kami akan segera menghubungi Anda.";
            Input = new();
            ModelState.Clear();
        }
        catch (Exception)
        {
            ErrorMessage = "Terjadi kesalahan saat memproses permintaan Anda. Silakan coba lagi nanti.";
        }

        return Page();
    }

    public class ContactInputModel
    {
        [Required(ErrorMessage = "Nama lengkap wajib diisi.")]
        [StringLength(100, ErrorMessage = "Nama maksimal 100 karakter.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email wajib diisi.")]
        [EmailAddress(ErrorMessage = "Format email tidak valid.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Subjek wajib diisi.")]
        [StringLength(200, ErrorMessage = "Subjek maksimal 200 karakter.")]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Pesan wajib diisi.")]
        [StringLength(2000, ErrorMessage = "Pesan maksimal 2000 karakter.")]
        public string Message { get; set; } = string.Empty;

        public string Category { get; set; } = "General";

        [Range(typeof(bool), "true", "true", ErrorMessage = "Persetujuan privasi wajib dicentang.")]
        public bool ConsentAccepted { get; set; }
    }
}
