using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using dagangOnline.Application.DTOs;
using dagangOnline.Application.Interfaces;
using dagangOnline.Domain;
using dagangOnline.Domain.Repositories;

namespace dagangOnline.Application.Services;

public class CatalogService : ICatalogService, IProductService, IServiceCatalogService, IPortfolioService, ISearchService
{
    private readonly IProductRepository _productRepository;
    private readonly IServiceRepository _serviceRepository;
    private readonly IPortfolioRepository _portfolioRepository;
    private readonly AuditLogService _auditLogService;

    public CatalogService(
        IProductRepository productRepository,
        IServiceRepository serviceRepository,
        IPortfolioRepository portfolioRepository,
        AuditLogService auditLogService)
    {
        _productRepository = productRepository;
        _serviceRepository = serviceRepository;
        _portfolioRepository = portfolioRepository;
        _auditLogService = auditLogService;
    }

    public IProductService Products => this;
    public IServiceCatalogService Services => this;
    public IPortfolioService Portfolio => this;
    public ISearchService Search => this;

    #region Queries
    public async Task<PagedResult<ProductDto>> GetPublishedProductsAsync(ProductCategory? category, int page, int pageSize, CancellationToken cancellationToken)
    {
        var (items, total) = await _productRepository.GetPublishedProductsAsync(category, page, pageSize, cancellationToken);
        
        var dtos = items.Select(p => new ProductDto {
            Id = p.Id, Name = p.Name, Slug = p.Slug, Summary = p.Summary,
            Description = p.Description, Price = p.Price, Currency = p.Currency ?? "IDR",
            Category = p.Category, Status = p.Status, IsFeatured = p.IsFeatured,
            ServiceId = p.ServiceId, CreatedAt = p.CreatedAt
        }).ToList();

        return new PagedResult<ProductDto> { Items = dtos, Page = page, PageSize = pageSize, TotalCount = total };
    }

    public async Task<ProductDto?> GetProductByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var p = await _productRepository.GetByIdAsync(id, false, cancellationToken);
        if (p == null) return null;
        return new ProductDto {
            Id = p.Id, Name = p.Name, Slug = p.Slug, Summary = p.Summary,
            Description = p.Description, Price = p.Price, Currency = p.Currency ?? "IDR",
            Category = p.Category, Status = p.Status, IsFeatured = p.IsFeatured,
            ServiceId = p.ServiceId, CreatedAt = p.CreatedAt
        };
    }

    public async Task<ProductDto?> GetProductBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        var p = await _productRepository.GetBySlugAsync(slug, false, cancellationToken);
        if (p == null) return null;
        return new ProductDto {
            Id = p.Id, Name = p.Name, Slug = p.Slug, Summary = p.Summary,
            Description = p.Description, Price = p.Price, Currency = p.Currency ?? "IDR",
            Category = p.Category, Status = p.Status, IsFeatured = p.IsFeatured,
            ServiceId = p.ServiceId, CreatedAt = p.CreatedAt
        };
    }

    public async Task<PagedResult<ServiceDto>> GetPublishedServicesAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var (items, total) = await _serviceRepository.GetPublishedServicesAsync(page, pageSize, cancellationToken);
        
        var dtos = items.Select(s => new ServiceDto {
            Id = s.Id, Name = s.Name, Slug = s.Slug, Summary = s.Summary,
            Description = s.Description, Status = s.Status, IsFeatured = s.IsFeatured,
            CreatedAt = s.CreatedAt
        }).ToList();

        return new PagedResult<ServiceDto> { Items = dtos, Page = page, PageSize = pageSize, TotalCount = total };
    }

    public async Task<ServiceDto?> GetServiceByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var s = await _serviceRepository.GetByIdAsync(id, false, cancellationToken);
        if (s == null) return null;
        return new ServiceDto {
            Id = s.Id, Name = s.Name, Slug = s.Slug, Summary = s.Summary,
            Description = s.Description, Status = s.Status, IsFeatured = s.IsFeatured,
            CreatedAt = s.CreatedAt
        };
    }

    public async Task<ServiceDto?> GetServiceBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        var s = await _serviceRepository.GetBySlugAsync(slug, false, cancellationToken);
        if (s == null) return null;
        return new ServiceDto {
            Id = s.Id, Name = s.Name, Slug = s.Slug, Summary = s.Summary,
            Description = s.Description, Status = s.Status, IsFeatured = s.IsFeatured,
            CreatedAt = s.CreatedAt
        };
    }

    public async Task<PagedResult<PortfolioDto>> GetFeaturedPortfolioAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var (items, total) = await _portfolioRepository.GetFeaturedPortfolioAsync(page, pageSize, cancellationToken);
        
        var dtos = items.Select(p => new PortfolioDto {
            Id = p.Id, Title = p.Title, Summary = p.Summary, Description = p.Description,
            Problem = p.Problem, Solution = p.Solution, Result = p.Result,
            Status = p.Status, IsFeatured = p.IsFeatured, CreatedAt = p.CreatedAt
        }).ToList();

        return new PagedResult<PortfolioDto> { Items = dtos, Page = page, PageSize = pageSize, TotalCount = total };
    }

    public async Task<PortfolioDto?> GetPortfolioByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var p = await _portfolioRepository.GetByIdAsync(id, false, cancellationToken);
        if (p == null) return null;
        return new PortfolioDto {
            Id = p.Id, Title = p.Title, Summary = p.Summary, Description = p.Description,
            Problem = p.Problem, Solution = p.Solution, Result = p.Result,
            Status = p.Status, IsFeatured = p.IsFeatured, CreatedAt = p.CreatedAt
        };
    }

    public async Task<PagedResult<SearchResultItem>> SearchAsync(string query, int page, int pageSize, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new PagedResult<SearchResultItem> { Page = page, PageSize = pageSize, TotalCount = 0 };

        var results = new List<SearchResultItem>();
        int limit = 100;

        var services = await _serviceRepository.SearchPublishedAsync(query, limit, cancellationToken);
        results.AddRange(services.Select(s => new SearchResultItem { Title = s.Name, Description = s.Summary, Type = "Service", Url = $"/Services" }));

        var projects = await _portfolioRepository.SearchPublishedAsync(query, limit, cancellationToken);
        results.AddRange(projects.Select(p => new SearchResultItem { Title = p.Title, Description = p.Summary, Type = "Portfolio", Url = "/Portfolio" }));

        var products = await _productRepository.SearchPublishedAsync(query, limit, cancellationToken);
        results.AddRange(products.Select(pr => new SearchResultItem { Title = pr.Name, Description = pr.Summary, Type = "Product", Url = $"/Services" }));

        var total = results.Count;
        var pagedResults = results.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return new PagedResult<SearchResultItem> { Items = pagedResults, Page = page, PageSize = pageSize, TotalCount = total };
    }
    #endregion

    #region Product Mutations
    public async Task<ProductDto> CreateProductAsync(CreateProductDto dto, string? userId, string? userName, bool isMitra, CancellationToken cancellationToken)
    {
        var slug = string.IsNullOrWhiteSpace(dto.Slug)
            ? Regex.Replace(dto.Name.ToLowerInvariant(), @"[^a-z0-9\s-]", "")
            : dto.Slug;
        slug = Regex.Replace(slug, @"\s+", "-").Trim('-');
        var uniqueSlug = $"{slug}-{Guid.NewGuid().ToString().Substring(0, 6)}";

        var entity = new Product
        {
            Name = dto.Name,
            Slug = uniqueSlug,
            Summary = dto.Summary,
            Description = dto.Description ?? string.Empty,
            Price = dto.Price,
            Currency = dto.Currency,
            Category = dto.Category,
            Status = isMitra ? PublicationStatus.PendingReview : dto.Status,
            OwnerId = isMitra ? userId : null,
            OwnerName = isMitra ? userName : null,
            IsFeatured = dto.IsFeatured,
            ServiceId = dto.ServiceId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _productRepository.AddAsync(entity, cancellationToken);

        await _auditLogService.LogAsync(
            action: "CreateProduct",
            entityName: "Product",
            entityId: entity.Id.ToString(),
            performedByUserName: userName,
            details: $"Product '{entity.Name}' created"
        );

        return new ProductDto
        {
            Id = entity.Id, Name = entity.Name, Slug = entity.Slug, Summary = entity.Summary,
            Description = entity.Description, Price = entity.Price, Currency = entity.Currency ?? "IDR",
            Category = entity.Category, Status = entity.Status, IsFeatured = entity.IsFeatured,
            ServiceId = entity.ServiceId, CreatedAt = entity.CreatedAt
        };
    }

    public async Task<ProductDto> UpdateProductAsync(Guid id, UpdateProductDto dto, string? userId, string? userName, bool isMitra, CancellationToken cancellationToken)
    {
        var entity = await _productRepository.GetByIdAsync(id, true, cancellationToken);
        if (entity == null)
            throw new KeyNotFoundException("Product not found.");

        if (isMitra && entity.OwnerId != userId)
            throw new UnauthorizedAccessException("You don't have permission to update this product.");

        entity.Name = dto.Name;
        if (!string.IsNullOrWhiteSpace(dto.Slug)) entity.Slug = dto.Slug;
        entity.Summary = dto.Summary;
        entity.Description = dto.Description ?? string.Empty;
        entity.Price = dto.Price;
        entity.Currency = dto.Currency;
        entity.Category = dto.Category;
        entity.Status = dto.Status;
        entity.IsFeatured = dto.IsFeatured;
        entity.ServiceId = dto.ServiceId;
        entity.UpdatedAt = DateTime.UtcNow;

        await _productRepository.UpdateAsync(entity, cancellationToken);

        await _auditLogService.LogAsync(
            action: "UpdateProduct",
            entityName: "Product",
            entityId: id.ToString(),
            performedByUserName: userName,
            details: $"Product '{entity.Name}' updated"
        );

        return new ProductDto
        {
            Id = entity.Id, Name = entity.Name, Slug = entity.Slug, Summary = entity.Summary,
            Description = entity.Description, Price = entity.Price, Currency = entity.Currency ?? "IDR",
            Category = entity.Category, Status = entity.Status, IsFeatured = entity.IsFeatured,
            ServiceId = entity.ServiceId, CreatedAt = entity.CreatedAt
        };
    }

    public async Task DeleteProductAsync(Guid id, string? userId, string? userName, bool isMitra, CancellationToken cancellationToken)
    {
        var entity = await _productRepository.GetByIdAsync(id, true, cancellationToken);
        if (entity == null)
            throw new KeyNotFoundException("Product not found.");

        if (isMitra && entity.OwnerId != userId)
            throw new UnauthorizedAccessException("You don't have permission to delete this product.");

        await _productRepository.DeleteAsync(entity, cancellationToken);

        await _auditLogService.LogWarningAsync(
            action: "DeleteProduct",
            entityName: "Product",
            entityId: id.ToString(),
            performedByUserName: userName,
            details: $"Product '{entity.Name}' deleted"
        );
    }
    #endregion

    #region Service Mutations
    public async Task<ServiceDto> CreateServiceAsync(CreateServiceDto dto, string? userName, CancellationToken cancellationToken)
    {
        var slug = string.IsNullOrWhiteSpace(dto.Slug)
            ? Regex.Replace(dto.Name.ToLowerInvariant(), @"[^a-z0-9\s-]", "")
            : dto.Slug;
        slug = Regex.Replace(slug, @"\s+", "-").Trim('-');
        var uniqueSlug = $"{slug}-{Guid.NewGuid().ToString().Substring(0, 6)}";

        var entity = new Service
        {
            Name = dto.Name,
            Slug = uniqueSlug,
            Summary = dto.Summary,
            Description = dto.Description ?? string.Empty,
            Status = dto.Status,
            IsFeatured = dto.IsFeatured,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _serviceRepository.AddAsync(entity, cancellationToken);

        await _auditLogService.LogAsync(
            action: "CreateService",
            entityName: "Service",
            entityId: entity.Id.ToString(),
            performedByUserName: userName,
            details: $"Service '{entity.Name}' created"
        );

        return new ServiceDto
        {
            Id = entity.Id, Name = entity.Name, Slug = entity.Slug, Summary = entity.Summary,
            Description = entity.Description, Status = entity.Status, IsFeatured = entity.IsFeatured,
            CreatedAt = entity.CreatedAt
        };
    }

    public async Task<ServiceDto> UpdateServiceAsync(Guid id, UpdateServiceDto dto, string? userName, CancellationToken cancellationToken)
    {
        var entity = await _serviceRepository.GetByIdAsync(id, true, cancellationToken);
        if (entity == null)
            throw new KeyNotFoundException("Service not found.");

        entity.Name = dto.Name;
        if (!string.IsNullOrWhiteSpace(dto.Slug)) entity.Slug = dto.Slug;
        entity.Summary = dto.Summary;
        entity.Description = dto.Description ?? string.Empty;
        entity.Status = dto.Status;
        entity.IsFeatured = dto.IsFeatured;
        entity.UpdatedAt = DateTime.UtcNow;

        await _serviceRepository.UpdateAsync(entity, cancellationToken);

        await _auditLogService.LogAsync(
            action: "UpdateService",
            entityName: "Service",
            entityId: id.ToString(),
            performedByUserName: userName,
            details: $"Service '{entity.Name}' updated"
        );

        return new ServiceDto
        {
            Id = entity.Id, Name = entity.Name, Slug = entity.Slug, Summary = entity.Summary,
            Description = entity.Description, Status = entity.Status, IsFeatured = entity.IsFeatured,
            CreatedAt = entity.CreatedAt
        };
    }

    public async Task DeleteServiceAsync(Guid id, string? userName, CancellationToken cancellationToken)
    {
        var entity = await _serviceRepository.GetByIdAsync(id, true, cancellationToken);
        if (entity == null)
            throw new KeyNotFoundException("Service not found.");

        await _serviceRepository.DeleteAsync(entity, cancellationToken);

        await _auditLogService.LogWarningAsync(
            action: "DeleteService",
            entityName: "Service",
            entityId: id.ToString(),
            performedByUserName: userName,
            details: $"Service '{entity.Name}' deleted"
        );
    }
    #endregion

    #region Portfolio Mutations
    public async Task<PortfolioDto> CreatePortfolioAsync(CreatePortfolioDto dto, string? userName, CancellationToken cancellationToken)
    {
        var entity = new PortfolioProject
        {
            Title = dto.Title,
            Summary = dto.Summary,
            Description = dto.Description ?? string.Empty,
            Problem = dto.Problem ?? string.Empty,
            Solution = dto.Solution ?? string.Empty,
            Result = dto.Result ?? string.Empty,
            Status = dto.Status,
            IsFeatured = dto.IsFeatured,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _portfolioRepository.AddAsync(entity, cancellationToken);

        await _auditLogService.LogAsync(
            action: "CreatePortfolio",
            entityName: "PortfolioProject",
            entityId: entity.Id.ToString(),
            performedByUserName: userName,
            details: $"Portfolio '{entity.Title}' created"
        );

        return new PortfolioDto
        {
            Id = entity.Id, Title = entity.Title, Summary = entity.Summary, Description = entity.Description,
            Problem = entity.Problem, Solution = entity.Solution, Result = entity.Result,
            Status = entity.Status, IsFeatured = entity.IsFeatured, CreatedAt = entity.CreatedAt
        };
    }

    public async Task<PortfolioDto> UpdatePortfolioAsync(Guid id, UpdatePortfolioDto dto, string? userName, CancellationToken cancellationToken)
    {
        var entity = await _portfolioRepository.GetByIdAsync(id, true, cancellationToken);
        if (entity == null)
            throw new KeyNotFoundException("Portfolio not found.");

        entity.Title = dto.Title;
        entity.Summary = dto.Summary;
        entity.Description = dto.Description ?? string.Empty;
        entity.Problem = dto.Problem ?? string.Empty;
        entity.Solution = dto.Solution ?? string.Empty;
        entity.Result = dto.Result ?? string.Empty;
        entity.Status = dto.Status;
        entity.IsFeatured = dto.IsFeatured;
        entity.UpdatedAt = DateTime.UtcNow;

        await _portfolioRepository.UpdateAsync(entity, cancellationToken);

        await _auditLogService.LogAsync(
            action: "UpdatePortfolio",
            entityName: "PortfolioProject",
            entityId: id.ToString(),
            performedByUserName: userName,
            details: $"Portfolio '{entity.Title}' updated"
        );

        return new PortfolioDto
        {
            Id = entity.Id, Title = entity.Title, Summary = entity.Summary, Description = entity.Description,
            Problem = entity.Problem, Solution = entity.Solution, Result = entity.Result,
            Status = entity.Status, IsFeatured = entity.IsFeatured, CreatedAt = entity.CreatedAt
        };
    }

    public async Task DeletePortfolioAsync(Guid id, string? userName, CancellationToken cancellationToken)
    {
        var entity = await _portfolioRepository.GetByIdAsync(id, true, cancellationToken);
        if (entity == null)
            throw new KeyNotFoundException("Portfolio not found.");

        await _portfolioRepository.DeleteAsync(entity, cancellationToken);

        await _auditLogService.LogWarningAsync(
            action: "DeletePortfolio",
            entityName: "PortfolioProject",
            entityId: id.ToString(),
            performedByUserName: userName,
            details: $"Portfolio '{entity.Title}' deleted"
        );
    }
    #endregion
}
