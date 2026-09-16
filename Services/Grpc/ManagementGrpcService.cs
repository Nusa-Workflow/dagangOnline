using System;
using System.Threading.Tasks;
using dagangOnline.Application.DTOs;
using dagangOnline.Application.Interfaces;
using dagangOnline.Authorization;
using dagangOnline.Domain;
using dagangOnline.Protos;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace dagangOnline.Services.Grpc;

[Authorize(Policy = AuthorizationPolicies.RequireMitraOrAdmin)]
public class ManagementGrpcService : Management.ManagementBase
{
    private readonly ICatalogService _catalogService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ManagementGrpcService(ICatalogService catalogService, IHttpContextAccessor httpContextAccessor)
    {
        _catalogService = catalogService;
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public override async Task<ProductMutationResponse> CreateProduct(CreateProductRequest request, ServerCallContext context)
    {
        try
        {
            var isMitra = User != null && User.IsInRole(RoleConstants.Mitra) && !User.IsInRole(RoleConstants.Admin);
            var currentUserId = User?.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUserName = User?.Identity?.Name;

            var dto = new CreateProductDto
            {
                Name = request.Name,
                Slug = request.Slug,
                Summary = request.Summary,
                Description = request.Description,
                Price = (decimal)request.Price,
                Currency = request.Currency,
                Category = (ProductCategory)request.Category,
                Status = (PublicationStatus)request.Status,
                IsFeatured = request.IsFeatured,
                ServiceId = string.IsNullOrWhiteSpace(request.ServiceId) ? null : Guid.Parse(request.ServiceId)
            };

            var result = await _catalogService.Products.CreateProductAsync(dto, currentUserId, currentUserName, isMitra, context.CancellationToken);
            
            return new ProductMutationResponse
            {
                Success = true,
                Message = "Product created successfully",
                ProductId = result.Id.ToString()
            };
        }
        catch (Exception ex)
        {
            throw new RpcException(new Status(StatusCode.Internal, $"Failed to create product: {ex.Message}"));
        }
    }

    public override async Task<ProductMutationResponse> UpdateProduct(UpdateProductRequest request, ServerCallContext context)
    {
        try
        {
            var isMitra = User != null && User.IsInRole(RoleConstants.Mitra) && !User.IsInRole(RoleConstants.Admin);
            var currentUserId = User?.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUserName = User?.Identity?.Name;

            var dto = new UpdateProductDto
            {
                Name = request.Name,
                Slug = request.Slug,
                Summary = request.Summary,
                Description = request.Description,
                Price = (decimal)request.Price,
                Currency = request.Currency,
                Category = (ProductCategory)request.Category,
                Status = (PublicationStatus)request.Status,
                IsFeatured = request.IsFeatured,
                ServiceId = string.IsNullOrWhiteSpace(request.ServiceId) ? null : Guid.Parse(request.ServiceId)
            };

            var result = await _catalogService.Products.UpdateProductAsync(Guid.Parse(request.Id), dto, currentUserId, currentUserName, isMitra, context.CancellationToken);
            
            return new ProductMutationResponse
            {
                Success = true,
                Message = "Product updated successfully",
                ProductId = result.Id.ToString()
            };
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message));
        }
        catch (Exception ex)
        {
            throw new RpcException(new Status(StatusCode.Internal, $"Failed to update product: {ex.Message}"));
        }
    }

    public override async Task<StatusResponse> DeleteProduct(DeleteRequest request, ServerCallContext context)
    {
        try
        {
            var isMitra = User != null && User.IsInRole(RoleConstants.Mitra) && !User.IsInRole(RoleConstants.Admin);
            var currentUserId = User?.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUserName = User?.Identity?.Name;

            await _catalogService.Products.DeleteProductAsync(Guid.Parse(request.Id), currentUserId, currentUserName, isMitra, context.CancellationToken);
            
            return new StatusResponse { Success = true, Message = "Product deleted successfully" };
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message));
        }
        catch (Exception ex)
        {
            throw new RpcException(new Status(StatusCode.Internal, $"Failed to delete product: {ex.Message}"));
        }
    }

    // Admins only for Services and Portfolios, but we annotated class with RequireMitraOrAdmin, 
    // so we must do manual check if it's Mitra trying to do Admin stuff.
    private void EnsureAdmin()
    {
        if (User == null || !User.IsInRole(RoleConstants.Admin))
        {
            throw new UnauthorizedAccessException("Only admins can perform this action.");
        }
    }

    public override async Task<ServiceMutationResponse> CreateService(CreateServiceRequest request, ServerCallContext context)
    {
        try
        {
            EnsureAdmin();
            var currentUserName = User?.Identity?.Name;

            var dto = new CreateServiceDto
            {
                Name = request.Name,
                Slug = request.Slug,
                Summary = request.Summary,
                Description = request.Description,
                Status = (PublicationStatus)request.Status,
                IsFeatured = request.IsFeatured
            };

            var result = await _catalogService.Services.CreateServiceAsync(dto, currentUserName, context.CancellationToken);
            
            return new ServiceMutationResponse { Success = true, Message = "Service created successfully", ServiceId = result.Id.ToString() };
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message));
        }
        catch (Exception ex)
        {
            throw new RpcException(new Status(StatusCode.Internal, $"Failed to create service: {ex.Message}"));
        }
    }

    public override async Task<ServiceMutationResponse> UpdateService(UpdateServiceRequest request, ServerCallContext context)
    {
        try
        {
            EnsureAdmin();
            var currentUserName = User?.Identity?.Name;

            var dto = new UpdateServiceDto
            {
                Name = request.Name,
                Slug = request.Slug,
                Summary = request.Summary,
                Description = request.Description,
                Status = (PublicationStatus)request.Status,
                IsFeatured = request.IsFeatured
            };

            var result = await _catalogService.Services.UpdateServiceAsync(Guid.Parse(request.Id), dto, currentUserName, context.CancellationToken);
            
            return new ServiceMutationResponse { Success = true, Message = "Service updated successfully", ServiceId = result.Id.ToString() };
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message));
        }
        catch (Exception ex)
        {
            throw new RpcException(new Status(StatusCode.Internal, $"Failed to update service: {ex.Message}"));
        }
    }

    public override async Task<StatusResponse> DeleteService(DeleteRequest request, ServerCallContext context)
    {
        try
        {
            EnsureAdmin();
            var currentUserName = User?.Identity?.Name;

            await _catalogService.Services.DeleteServiceAsync(Guid.Parse(request.Id), currentUserName, context.CancellationToken);
            return new StatusResponse { Success = true, Message = "Service deleted successfully" };
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message));
        }
        catch (Exception ex)
        {
            throw new RpcException(new Status(StatusCode.Internal, $"Failed to delete service: {ex.Message}"));
        }
    }

    public override async Task<PortfolioMutationResponse> CreatePortfolio(CreatePortfolioRequest request, ServerCallContext context)
    {
        try
        {
            EnsureAdmin();
            var currentUserName = User?.Identity?.Name;

            var dto = new CreatePortfolioDto
            {
                Title = request.Title,
                Summary = request.Summary,
                Description = request.Description,
                Problem = request.Problem,
                Solution = request.Solution,
                Result = request.Result,
                Status = (PublicationStatus)request.Status,
                IsFeatured = request.IsFeatured
            };

            var result = await _catalogService.Portfolio.CreatePortfolioAsync(dto, currentUserName, context.CancellationToken);
            
            return new PortfolioMutationResponse { Success = true, Message = "Portfolio created successfully", PortfolioId = result.Id.ToString() };
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message));
        }
        catch (Exception ex)
        {
            throw new RpcException(new Status(StatusCode.Internal, $"Failed to create portfolio: {ex.Message}"));
        }
    }

    public override async Task<PortfolioMutationResponse> UpdatePortfolio(UpdatePortfolioRequest request, ServerCallContext context)
    {
        try
        {
            EnsureAdmin();
            var currentUserName = User?.Identity?.Name;

            var dto = new UpdatePortfolioDto
            {
                Title = request.Title,
                Summary = request.Summary,
                Description = request.Description,
                Problem = request.Problem,
                Solution = request.Solution,
                Result = request.Result,
                Status = (PublicationStatus)request.Status,
                IsFeatured = request.IsFeatured
            };

            var result = await _catalogService.Portfolio.UpdatePortfolioAsync(Guid.Parse(request.Id), dto, currentUserName, context.CancellationToken);
            
            return new PortfolioMutationResponse { Success = true, Message = "Portfolio updated successfully", PortfolioId = result.Id.ToString() };
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message));
        }
        catch (Exception ex)
        {
            throw new RpcException(new Status(StatusCode.Internal, $"Failed to update portfolio: {ex.Message}"));
        }
    }

    public override async Task<StatusResponse> DeletePortfolio(DeleteRequest request, ServerCallContext context)
    {
        try
        {
            EnsureAdmin();
            var currentUserName = User?.Identity?.Name;

            await _catalogService.Portfolio.DeletePortfolioAsync(Guid.Parse(request.Id), currentUserName, context.CancellationToken);
            return new StatusResponse { Success = true, Message = "Portfolio deleted successfully" };
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message));
        }
        catch (Exception ex)
        {
            throw new RpcException(new Status(StatusCode.Internal, $"Failed to delete portfolio: {ex.Message}"));
        }
    }
}
