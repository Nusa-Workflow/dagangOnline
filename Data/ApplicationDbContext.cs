using dagangOnline.Domain;
using dagangOnline.Domain.Agents;
using dagangOnline.Domain.Chat;
using dagangOnline.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace dagangOnline.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public DbSet<PortfolioProject> PortfolioProjects => Set<PortfolioProject>();
    public DbSet<PortfolioTechnology> PortfolioTechnologies => Set<PortfolioTechnology>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ContactInquiry> ContactInquiries => Set<ContactInquiry>();
    public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<CarouselItem> CarouselItems => Set<CarouselItem>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ContentPage> ContentPages => Set<ContentPage>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<MitraProfile> MitraProfiles => Set<MitraProfile>();
    public DbSet<ReviewTask> ReviewTasks => Set<ReviewTask>();
    public DbSet<ModerationDecision> ModerationDecisions => Set<ModerationDecision>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ConversationMessage> ConversationMessages => Set<ConversationMessage>();
    public DbSet<KnowledgeDocument> KnowledgeDocuments => Set<KnowledgeDocument>();
    public DbSet<KnowledgeChunk> KnowledgeChunks => Set<KnowledgeChunk>();
    public DbSet<ConversationFeedback> ConversationFeedbacks => Set<ConversationFeedback>();

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(x => x.DisplayName).HasMaxLength(200);
            entity.Property(x => x.Bio).HasMaxLength(1000);
            entity.Property(x => x.AvatarUrl).HasMaxLength(500);
            entity.Property(x => x.IsActive).HasDefaultValue(true);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(x => x.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        builder.Entity<PortfolioProject>(entity =>
        {
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Summary).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>();
            entity.HasMany(x => x.Technologies)
                .WithOne(x => x.PortfolioProject)
                .HasForeignKey(x => x.PortfolioProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<PortfolioTechnology>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.HasIndex(x => new { x.PortfolioProjectId, x.Name }).IsUnique();
        });

        builder.Entity<Service>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Slug).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>();
            entity.HasMany(x => x.Products)
                .WithOne(x => x.Service)
                .HasForeignKey(x => x.ServiceId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Product>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Slug).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Currency).HasMaxLength(10).HasDefaultValue("IDR");
            entity.Property(x => x.Status).HasConversion<string>();
            entity.HasIndex(x => x.Slug).IsUnique();
        });

        builder.Entity<ContactInquiry>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Subject).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Category).HasMaxLength(100);
            entity.Property(x => x.Status).HasConversion<string>();
        });

        builder.Entity<Announcement>(entity =>
        {
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>();
        });

        builder.Entity<CarouselItem>(entity =>
        {
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.ImageUrl).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>();
        });

        builder.Entity<ContentPage>(entity =>
        {
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Slug).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>();
            entity.HasIndex(x => x.Slug).IsUnique();
        });

        builder.Entity<Notification>(entity =>
        {
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Message).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.Type).HasMaxLength(50).HasDefaultValue("Info");
        });

        builder.Entity<UserProfile>(entity =>
        {
            entity.HasIndex(x => x.UserId).IsUnique();
            entity.Property(x => x.DisplayName).HasMaxLength(200);
        });

        builder.Entity<MitraProfile>(entity =>
        {
            entity.HasIndex(x => x.UserId).IsUnique();
            entity.Property(x => x.BusinessName).HasMaxLength(200).IsRequired();
        });

        builder.Entity<AuditLog>(entity =>
        {
            entity.Property(x => x.Action).HasMaxLength(200).IsRequired();
            entity.Property(x => x.EntityName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Severity).HasMaxLength(50).HasDefaultValue("Info");
        });

        builder.Entity<ServiceRequest>(entity =>
        {
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.ClientEmail).HasMaxLength(200);
            entity.Property(x => x.Status).HasMaxLength(50).HasDefaultValue("New");
        });

        builder.Entity<ReviewTask>(entity =>
        {
            entity.Property(x => x.Status).HasConversion<string>();
            entity.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ModerationDecision>(entity =>
        {
            entity.Property(x => x.Decision).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Reason).HasMaxLength(500);
            entity.Property(x => x.Notes).HasMaxLength(1000);
            entity.HasOne(x => x.ReviewTask)
                .WithMany()
                .HasForeignKey(x => x.ReviewTaskId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Conversation>(entity =>
        {
            entity.Property(x => x.Status).HasConversion<string>();
            entity.Property(x => x.Priority).HasConversion<string>();
            entity.Property(x => x.Intent).HasConversion<string>();
            entity.HasOne(x => x.Customer)
                .WithMany()
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.AssignedAgent)
                .WithMany()
                .HasForeignKey(x => x.AssignedAgentId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<ConversationMessage>(entity =>
        {
            entity.Property(x => x.Content).IsRequired();
            entity.Property(x => x.SenderType).HasConversion<string>();
            entity.HasOne(x => x.Conversation)
                .WithMany(s => s.Messages)
                .HasForeignKey(x => x.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
