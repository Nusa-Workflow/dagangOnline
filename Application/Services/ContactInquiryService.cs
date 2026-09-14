using dagangOnline.Data;
using dagangOnline.Domain;

namespace dagangOnline.Application.Services;

public class ContactInquiryService
{
    private readonly ApplicationDbContext _context;

    public ContactInquiryService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ContactInquiry> SubmitAsync(ContactInquirySubmission submission)
    {
        var inquiry = new ContactInquiry
        {
            Name = submission.Name,
            Email = submission.Email,
            Subject = submission.Subject,
            Message = submission.Message,
            Category = submission.Category,
            ConsentAccepted = submission.ConsentAccepted,
            Status = InquiryStatus.New,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.ContactInquiries.Add(inquiry);
        await _context.SaveChangesAsync();

        return inquiry;
    }
}
