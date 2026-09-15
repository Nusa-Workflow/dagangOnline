using System.Threading.RateLimiting;
using dagangOnline.Application.Services;
using dagangOnline.Authorization;
using dagangOnline.Data;
using dagangOnline.Models;
using dagangOnline.Services;
using dagangOnline.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (!string.IsNullOrWhiteSpace(connectionString))
    {
        options.UseNpgsql(connectionString);
    }
    else
    {
        options.UseInMemoryDatabase("dagangOnline");
    }
});

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicies.RequireAdmin, policy => policy.RequireRole(RoleConstants.Admin));
    options.AddPolicy(AuthorizationPolicies.RequireUser, policy => policy.RequireRole(RoleConstants.User));
    options.AddPolicy(AuthorizationPolicies.RequireMitra, policy => policy.RequireRole(RoleConstants.Mitra));
    options.AddPolicy(AuthorizationPolicies.RequireAgent, policy => policy.RequireRole(RoleConstants.Agent));
    options.AddPolicy(AuthorizationPolicies.RequireUserOrAdmin, policy => policy.RequireAssertion(context =>
        context.User.IsInRole(RoleConstants.User) || context.User.IsInRole(RoleConstants.Admin)));
    options.AddPolicy(AuthorizationPolicies.RequireMitraOrAdmin, policy => policy.RequireAssertion(context =>
        context.User.IsInRole(RoleConstants.Mitra) || context.User.IsInRole(RoleConstants.Admin)));
    options.AddPolicy(AuthorizationPolicies.RequireAgentOrAdmin, policy => policy.RequireAssertion(context =>
        context.User.IsInRole(RoleConstants.Agent) || context.User.IsInRole(RoleConstants.Admin)));
});

builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddGrpc();
builder.Services.AddSignalR();

// Application services
builder.Services.AddScoped<AuditLogService>();
builder.Services.AddScoped<ContactInquiryService>();
builder.Services.AddScoped<PublicCatalogService>();
builder.Services.AddScoped<AgentReviewService>();
builder.Services.AddScoped<ChatBotService>();
builder.Services.AddSingleton<ResourceAuthorizationService>();

// Rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("fixed", limiter =>
    {
        limiter.PermitLimit = 60;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiter.QueueLimit = 5;
    });
    options.AddFixedWindowLimiter("contact", limiter =>
    {
        limiter.PermitLimit = 5;
        limiter.Window = TimeSpan.FromMinutes(10);
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiter.QueueLimit = 0;
    });
});

// Anti-forgery
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "dagangOnline.Antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<ApplicationDbContext>();
    db.Database.EnsureCreated();

    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

    await SeedRolesAsync(roleManager);
    await SeedAdminUserAsync(userManager, roleManager);
    await SeedAgentUserAsync(userManager, roleManager);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseSecurityHeaders();

var contentTypeProvider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
contentTypeProvider.Mappings[".webmanifest"] = "application/manifest+json";

app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = contentTypeProvider
});

app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi();
app.MapControllers();
app.MapDefaultControllerRoute();
app.MapGrpcService<dagangOnline.Services.Grpc.CatalogGrpcService>();
app.MapHub<SupportChatHub>("/supportChatHub");
app.MapRazorPages();

app.Run();

static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
{
    foreach (var roleName in new[] { RoleConstants.Admin, RoleConstants.User, RoleConstants.Mitra, RoleConstants.Agent })
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }
}

static async Task SeedAdminUserAsync(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
{
    var email = "admin@dagangonline.local";
    var admin = await userManager.FindByEmailAsync(email);

    if (admin == null)
    {
        admin = new ApplicationUser
        {
            UserName = email,
            Email = email,
            DisplayName = "Platform Administrator",
            EmailConfirmed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(admin, "Admin@123");
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
        }
    }

    if (!await userManager.IsInRoleAsync(admin, RoleConstants.Admin))
    {
        await userManager.AddToRoleAsync(admin, RoleConstants.Admin);
    }
}

static async Task SeedAgentUserAsync(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
{
    var email = "agent@dagangonline.local";
    var agent = await userManager.FindByEmailAsync(email);

    if (agent == null)
    {
        agent = new ApplicationUser
        {
            UserName = email,
            Email = email,
            DisplayName = "Moderator Internal",
            EmailConfirmed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(agent, "Admin@123");
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
        }
    }

    if (!await userManager.IsInRoleAsync(agent, RoleConstants.Agent))
    {
        await userManager.AddToRoleAsync(agent, RoleConstants.Agent);
    }
}
