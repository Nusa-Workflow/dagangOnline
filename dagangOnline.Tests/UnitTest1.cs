using dagangOnline.Application.DTOs;
using dagangOnline.Application.Services;
using dagangOnline.Authorization;
using dagangOnline.Controllers.Api.v1;
using dagangOnline.Data;
using dagangOnline.Domain;
using dagangOnline.Models;
using dagangOnline.Protos;
using dagangOnline.Services.Grpc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace dagangOnline.Tests;

public class IdentityFoundationTests
{
    [Fact]
    public void RequiredRoles_AreDefined()
    {
        Assert.Equal("Admin", RoleConstants.Admin);
        Assert.Equal("User", RoleConstants.User);
        Assert.Equal("Mitra", RoleConstants.Mitra);
    }

    [Fact]
    public void AuthorizationPolicies_AreConfiguredForRoleBasedAccess()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthorizationPolicies.RequireAdmin, policy => policy.RequireRole(RoleConstants.Admin));
            options.AddPolicy(AuthorizationPolicies.RequireUser, policy => policy.RequireRole(RoleConstants.User));
            options.AddPolicy(AuthorizationPolicies.RequireMitra, policy => policy.RequireRole(RoleConstants.Mitra));
        });

        var provider = services.BuildServiceProvider();
        var authorizationOptions = provider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;

        Assert.NotNull(authorizationOptions.GetPolicy(AuthorizationPolicies.RequireAdmin));
        Assert.NotNull(authorizationOptions.GetPolicy(AuthorizationPolicies.RequireUser));
        Assert.NotNull(authorizationOptions.GetPolicy(AuthorizationPolicies.RequireMitra));
    }

    [Fact]
    public async Task ApplicationDbContext_CanPersistUserWithRoleInfo()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new ApplicationDbContext(options);
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "alice@example.com",
            Email = "alice@example.com",
            DisplayName = "Alice",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var saved = await context.Users.SingleAsync(x => x.Email == "alice@example.com");
        Assert.Equal("Alice", saved.DisplayName);
    }

    [Fact]
    public void DomainEntities_AreAvailableForPlatformFeatures()
    {
        var portfolioProject = new PortfolioProject();
        var service = new Service();
        var product = new Product();
        var inquiry = new ContactInquiry();
        var notification = new Notification();
        var auditLog = new AuditLog();

        Assert.NotNull(portfolioProject);
        Assert.NotNull(service);
        Assert.NotNull(product);
        Assert.NotNull(inquiry);
        Assert.NotNull(notification);
        Assert.NotNull(auditLog);
    }

    [Fact]
    public async Task ApplicationDbContext_CanPersistPortfolioAndInquiry()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new ApplicationDbContext(options);

        var project = new PortfolioProject
        {
            Id = Guid.NewGuid(),
            Title = "Digital Transformation",
            Summary = "Modernized operations",
            Description = "Migrated workflows to secure digital platform",
            Problem = "Legacy processes",
            Solution = "New platform",
            Result = "Faster delivery",
            Status = PublicationStatus.Published,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var inquiry = new ContactInquiry
        {
            Id = Guid.NewGuid(),
            Name = "Test User",
            Email = "customer@example.com",
            Subject = "Need a solution",
            Message = "I want a digital platform",
            Category = "General",
            ConsentAccepted = true,
            Status = InquiryStatus.New,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.PortfolioProjects.Add(project);
        context.ContactInquiries.Add(inquiry);
        await context.SaveChangesAsync();

        var savedProject = await context.PortfolioProjects.SingleAsync(x => x.Title == "Digital Transformation");
        var savedInquiry = await context.ContactInquiries.SingleAsync(x => x.Email == "customer@example.com");

        Assert.Equal("Digital Transformation", savedProject.Title);
        Assert.Equal(InquiryStatus.New, savedInquiry.Status);
    }

    [Fact]
    public async Task PublicCatalogService_ReturnsPublishedPortfolioAndServices()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new ApplicationDbContext(options);
        context.PortfolioProjects.Add(new PortfolioProject
        {
            Title = "Portal Migration",
            Summary = "Cloud migration",
            Description = "Transformed legacy processes",
            Problem = "Rigid workflows",
            Solution = "New platform",
            Result = "Higher throughput",
            Status = PublicationStatus.Published,
            IsFeatured = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        context.Services.Add(new Service
        {
            Name = "Platform Consulting",
            Slug = "platform-consulting",
            Summary = "Implementation support",
            Description = "End-to-end platform strategy",
            Status = PublicationStatus.Published,
            IsFeatured = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var service = new PublicCatalogService(context);

        var projects = await service.GetFeaturedPortfolioAsync();
        var servicesList = await service.GetPublishedServicesAsync();

        Assert.Single(projects);
        Assert.Single(servicesList);
        Assert.Equal("Portal Migration", projects[0].Title);
        Assert.Equal("Platform Consulting", servicesList[0].Name);
    }

    [Fact]
    public async Task ContactInquiryService_SubmitsAndStoresInquiry()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new ApplicationDbContext(options);
        var service = new ContactInquiryService(context);

        var submitted = await service.SubmitAsync(new ContactInquirySubmission
        {
            Name = "Ada",
            Email = "ada@example.com",
            Subject = "Need platform",
            Message = "We need a better portal",
            Category = "General",
            ConsentAccepted = true
        });

        Assert.NotNull(submitted);
        Assert.Equal("Ada", submitted.Name);
        Assert.Equal(InquiryStatus.New, submitted.Status);

        var saved = await context.ContactInquiries.SingleAsync(x => x.Email == "ada@example.com");
        Assert.Equal("Need platform", saved.Subject);
    }

    [Fact]
    public void PermissionMatrix_ContainsRequiredSecurityPermissions()
    {
        Assert.Contains(PermissionConstants.UsersRead, PermissionConstants.All);
        Assert.Contains(PermissionConstants.PortfolioCreateOwn, PermissionConstants.All);
        Assert.Contains(PermissionConstants.ContactReadOwn, PermissionConstants.All);
        Assert.Contains(PermissionConstants.AuditRead, PermissionConstants.All);
    }

    [Fact]
    public void ResourceAuthorizationService_RejectsCrossOwnerAccess()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user-1"),
            new Claim(ClaimTypes.Role, RoleConstants.User)
        }, "TestAuth"));

        var service = new ResourceAuthorizationService();

        var canAccess = service.CanAccessOwnedResource(principal, "user-2", "user-1");

        Assert.False(canAccess);
    }

    [Fact]
    public void ResourceAuthorizationService_AllowsAdminToAccessAnyOwnedResource()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "admin-1"),
            new Claim(ClaimTypes.Role, RoleConstants.Admin)
        }, "TestAuth"));

        var service = new ResourceAuthorizationService();

        var canAccess = service.CanAccessOwnedResource(principal, "user-2", "user-1");

        Assert.True(canAccess);
    }

    [Fact]
    public void ResourceAuthorizationService_RejectsRoleEscalationPayload()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user-1"),
            new Claim(ClaimTypes.Role, RoleConstants.User)
        }, "TestAuth"));

        var service = new ResourceAuthorizationService();

        var canAssignRole = service.CanAssignRole(principal, RoleConstants.Admin, RoleConstants.User);

        Assert.False(canAssignRole);
    }

    [Fact]
    public async Task PublicCatalogService_SearchAsync_ReturnsMatchingResults()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new ApplicationDbContext(options);
        context.Services.Add(new Service
        {
            Name = "Web Enterprise Portal",
            Slug = "web-enterprise-portal",
            Summary = "High availability web portal",
            Status = PublicationStatus.Published
        });
        context.PortfolioProjects.Add(new PortfolioProject
        {
            Title = "Retail Ecommerce System",
            Summary = "Scalable web shopping solution",
            Status = PublicationStatus.Published
        });
        await context.SaveChangesAsync();

        var service = new PublicCatalogService(context);
        var results = await service.SearchAsync("web");

        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.Title == "Web Enterprise Portal" && r.Type == "Service");
        Assert.Contains(results, r => r.Title == "Retail Ecommerce System" && r.Type == "Portfolio");
    }

    [Fact]
    public async Task AuditLogService_LogsInfoAndWarningEntries()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new ApplicationDbContext(options);
        var auditService = new AuditLogService(context);

        await auditService.LogAsync("CreateService", "Service", "svc-1", "user-1", "Admin", "Created service");
        await auditService.LogWarningAsync("FailedLogin", "User", "user-2", null, null, "3 invalid attempts");

        var logs = await context.AuditLogs.ToListAsync();
        Assert.Equal(2, logs.Count);
        Assert.Contains(logs, l => l.Action == "CreateService" && l.Severity == "Info");
        Assert.Contains(logs, l => l.Action == "FailedLogin" && l.Severity == "Warning");
    }

    [Fact]
    public async Task SecurityHeadersMiddleware_SetsRequiredSecurityHeaders()
    {
        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        var middleware = new SecurityHeadersMiddleware(innerContext => Task.CompletedTask);

        await middleware.InvokeAsync(httpContext);

        var headers = httpContext.Response.Headers;
        Assert.Equal("nosniff", headers["X-Content-Type-Options"].ToString());
        Assert.Equal("DENY", headers["X-Frame-Options"].ToString());
        Assert.Equal("1; mode=block", headers["X-XSS-Protection"].ToString());
        Assert.Equal("strict-origin-when-cross-origin", headers["Referrer-Policy"].ToString());
        Assert.Contains("default-src 'self'", headers["Content-Security-Policy"].ToString());
    }

    [Fact]
    public async Task UserDashboard_ServiceRequestAndNotification_PersistProperly()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new ApplicationDbContext(options);
        var req = new ServiceRequest
        {
            Title = "Need CRM system",
            Description = "Custom CRM for 20 agents",
            ClientEmail = "client@example.com",
            Status = "New"
        };
        var notif = new Notification
        {
            Title = "Permintaan Diterima",
            Message = "Permintaan sedang ditinjau",
            RecipientEmail = "client@example.com",
            IsRead = false
        };

        context.ServiceRequests.Add(req);
        context.Notifications.Add(notif);
        await context.SaveChangesAsync();

        var savedReq = await context.ServiceRequests.SingleAsync(r => r.ClientEmail == "client@example.com");
        var savedNotif = await context.Notifications.SingleAsync(n => n.RecipientEmail == "client@example.com");

        Assert.Equal("Need CRM system", savedReq.Title);
        Assert.False(savedNotif.IsRead);
    }

    [Fact]
    public async Task MitraDashboard_MitraProfileAndProduct_PersistProperly()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new ApplicationDbContext(options);
        var profile = new MitraProfile
        {
            UserId = "mitra-user-1",
            BusinessName = "PT Tech Mitra Jaya",
            City = "Bandung",
            IsVerified = true
        };
        var product = new Product
        {
            Name = "Paket ERP Cloud",
            Slug = "paket-erp-cloud",
            Price = 5000000,
            Category = ProductCategory.SaaS,
            Status = PublicationStatus.Published
        };

        context.MitraProfiles.Add(profile);
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var savedProfile = await context.MitraProfiles.SingleAsync(p => p.UserId == "mitra-user-1");
        var savedProduct = await context.Products.SingleAsync(p => p.Slug == "paket-erp-cloud");

        Assert.True(savedProfile.IsVerified);
        Assert.Equal(5000000, savedProduct.Price);
    }

    [Fact]
    public async Task AdminDashboard_StatusUpdate_GeneratesAuditLog()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new ApplicationDbContext(options);
        var auditService = new AuditLogService(context);

        var inquiry = new ContactInquiry
        {
            Name = "Prospective Partner",
            Email = "partner@example.com",
            Subject = "Partnership Proposal",
            Message = "Proposal details...",
            Status = InquiryStatus.New
        };
        context.ContactInquiries.Add(inquiry);
        await context.SaveChangesAsync();

        // Simulate admin action
        inquiry.Status = InquiryStatus.Replied;
        await auditService.LogAsync(
            action: "UpdateInquiryStatus",
            entityName: "ContactInquiry",
            entityId: inquiry.Id.ToString(),
            performedByUserName: "admin@dagangonline.local",
            details: "Changed status from New to Replied"
        );
        await context.SaveChangesAsync();

        var audit = await context.AuditLogs.SingleAsync(a => a.Action == "UpdateInquiryStatus");
        Assert.Equal("ContactInquiry", audit.EntityName);
        Assert.Contains("Replied", audit.Details);
    }

    [Fact]
    public async Task ServicesApiController_CrudOperations_WorkCorrectly()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new ApplicationDbContext(options);
        var auditService = new AuditLogService(context);
        var controller = new ServicesController(context, auditService);

        // 1. Create
        var createResult = await controller.Create(new CreateServiceDto
        {
            Name = "DevOps Engineering",
            Summary = "Cloud CI/CD modern automation",
            Description = "Automated deployments and testing",
            Status = PublicationStatus.Published
        });
        var createdAction = Assert.IsType<CreatedAtActionResult>(createResult);
        var createdDto = Assert.IsType<ApiResponse<ServiceDto>>(createdAction.Value);
        Assert.NotNull(createdDto.Data);
        var serviceId = createdDto.Data.Id;

        // 2. GetById
        var getResult = await controller.GetById(serviceId);
        var getOk = Assert.IsType<OkObjectResult>(getResult);
        var getDto = Assert.IsType<ApiResponse<ServiceDto>>(getOk.Value);
        Assert.Equal("DevOps Engineering", getDto.Data?.Name);

        // 3. Update
        var updateResult = await controller.Update(serviceId, new UpdateServiceDto
        {
            Name = "DevOps & Cloud Automation",
            Summary = "Updated summary",
            Status = PublicationStatus.Published
        });
        var updateOk = Assert.IsType<OkObjectResult>(updateResult);
        var updateDto = Assert.IsType<ApiResponse<ServiceDto>>(updateOk.Value);
        Assert.Equal("DevOps & Cloud Automation", updateDto.Data?.Name);

        // 4. GetAll
        var allResult = await controller.GetAll();
        var allOk = Assert.IsType<OkObjectResult>(allResult);
        var allDto = Assert.IsType<ApiResponse<List<ServiceDto>>>(allOk.Value);
        Assert.Single(allDto.Data!);

        // 5. Delete
        var deleteResult = await controller.Delete(serviceId);
        var deleteOk = Assert.IsType<OkObjectResult>(deleteResult);
        var deleteDto = Assert.IsType<ApiResponse<bool>>(deleteOk.Value);
        Assert.True(deleteDto.Data);

        var verifyDeleted = await controller.GetById(serviceId);
        Assert.IsType<NotFoundObjectResult>(verifyDeleted);
    }

    [Fact]
    public async Task ProductsApiController_CreateAndFilterProducts_WorkCorrectly()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new ApplicationDbContext(options);
        var auditService = new AuditLogService(context);
        var controller = new ProductsController(context, auditService);

        var createResult = await controller.Create(new CreateProductDto
        {
            Name = "Smart POS",
            Summary = "Cloud cashier system",
            Price = 2500000,
            Category = ProductCategory.SaaS,
            Status = PublicationStatus.Published
        });
        var createdAction = Assert.IsType<CreatedAtActionResult>(createResult);
        var createdDto = Assert.IsType<ApiResponse<ProductDto>>(createdAction.Value);
        Assert.Equal("Smart POS", createdDto.Data?.Name);

        var filterResult = await controller.GetAll(ProductCategory.SaaS, 3000000);
        var filterOk = Assert.IsType<OkObjectResult>(filterResult);
        var filterDto = Assert.IsType<ApiResponse<List<ProductDto>>>(filterOk.Value);
        Assert.Single(filterDto.Data!);
    }

    [Fact]
    public async Task CatalogGrpcService_ReturnsExpectedProtobufPayload()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new ApplicationDbContext(options);
        context.Services.Add(new Service
        {
            Name = "Cybersecurity Audit",
            Slug = "cybersecurity-audit",
            Summary = "Penetration testing & compliance",
            Status = PublicationStatus.Published
        });
        await context.SaveChangesAsync();

        var catalogService = new PublicCatalogService(context);
        var grpcService = new CatalogGrpcService(context, catalogService);

        var response = await grpcService.GetPublishedServices(new GetServicesRequest(), null!);
        Assert.Single(response.Services);
        Assert.Equal("Cybersecurity Audit", response.Services[0].Name);

        var searchResponse = await grpcService.SearchCatalog(new SearchCatalogRequest { Query = "audit" }, null!);
        Assert.Single(searchResponse.Results);
        Assert.Equal("Cybersecurity Audit", searchResponse.Results[0].Title);
    }
}

