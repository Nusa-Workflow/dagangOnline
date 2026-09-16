using System;
using System.Threading.Tasks;
using dagangOnline.Application.Interfaces;
using dagangOnline.Domain;
using dagangOnline.Protos;
using Grpc.Core;

namespace dagangOnline.Services.Grpc;

public class CatalogGrpcService : CatalogGrpc.CatalogGrpcBase
{
    private readonly ICatalogService _catalogService;

    public CatalogGrpcService(ICatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    public override async Task<GetServicesResponse> GetPublishedServices(GetServicesRequest request, ServerCallContext context)
    {
        try
        {
            var page = request.Pagination?.Page > 0 ? request.Pagination.Page : 1;
            var pageSize = request.Pagination?.PageSize > 0 ? request.Pagination.PageSize : 10;
            
            var pagedResult = await _catalogService.Services.GetPublishedServicesAsync(page, pageSize, context?.CancellationToken ?? default(CancellationToken));
            var response = new GetServicesResponse
            {
                Pagination = new PaginationResponse
                {
                    Page = pagedResult.Page,
                    PageSize = pagedResult.PageSize,
                    TotalCount = pagedResult.TotalCount
                }
            };

            foreach (var s in pagedResult.Items)
            {
                response.Services.Add(new ServiceItem
                {
                    Id = s.Id.ToString(),
                    Name = s.Name,
                    Slug = s.Slug,
                    Summary = s.Summary,
                    Description = s.Description,
                    IsFeatured = s.IsFeatured
                });
            }

            return response;
        }
        catch (OperationCanceledException)
        {
            throw new RpcException(new Status(StatusCode.Cancelled, "Request cancelled by the client."));
        }
        catch (Exception)
        {
            throw new RpcException(new Status(StatusCode.Internal, "An internal server error occurred."));
        }
    }

    public override async Task<GetProductsResponse> GetProducts(GetProductsRequest request, ServerCallContext context)
    {
        try
        {
            ProductCategory? category = null;
            if (!string.IsNullOrWhiteSpace(request.Category) && Enum.TryParse<ProductCategory>(request.Category, true, out var cat))
            {
                category = cat;
            }

            var page = request.Pagination?.Page > 0 ? request.Pagination.Page : 1;
            var pageSize = request.Pagination?.PageSize > 0 ? request.Pagination.PageSize : 10;

            var pagedResult = await _catalogService.Products.GetPublishedProductsAsync(category, page, pageSize, context?.CancellationToken ?? default(CancellationToken));
            var response = new GetProductsResponse
            {
                Pagination = new PaginationResponse
                {
                    Page = pagedResult.Page,
                    PageSize = pagedResult.PageSize,
                    TotalCount = pagedResult.TotalCount
                }
            };

            foreach (var p in pagedResult.Items)
            {
                response.Products.Add(new ProductItem
                {
                    Id = p.Id.ToString(),
                    Name = p.Name,
                    Slug = p.Slug,
                    Summary = p.Summary,
                    Price = (double)p.Price,
                    Currency = p.Currency ?? "IDR",
                    Category = p.Category.ToString()
                });
            }

            return response;
        }
        catch (OperationCanceledException)
        {
            throw new RpcException(new Status(StatusCode.Cancelled, "Request cancelled by the client."));
        }
        catch (Exception)
        {
            throw new RpcException(new Status(StatusCode.Internal, "An internal server error occurred."));
        }
    }

    public override async Task<GetPortfolioResponse> GetFeaturedPortfolio(GetPortfolioRequest request, ServerCallContext context)
    {
        try
        {
            var page = request.Pagination?.Page > 0 ? request.Pagination.Page : 1;
            var pageSize = request.Pagination?.PageSize > 0 ? request.Pagination.PageSize : 10;

            var pagedResult = await _catalogService.Portfolio.GetFeaturedPortfolioAsync(page, pageSize, context?.CancellationToken ?? default(CancellationToken));
            var response = new GetPortfolioResponse
            {
                Pagination = new PaginationResponse
                {
                    Page = pagedResult.Page,
                    PageSize = pagedResult.PageSize,
                    TotalCount = pagedResult.TotalCount
                }
            };

            foreach (var p in pagedResult.Items)
            {
                response.Items.Add(new PortfolioItem
                {
                    Id = p.Id.ToString(),
                    Title = p.Title,
                    Summary = p.Summary,
                    Problem = p.Problem ?? string.Empty,
                    Solution = p.Solution ?? string.Empty,
                    Result = p.Result ?? string.Empty,
                    IsFeatured = p.IsFeatured
                });
            }

            return response;
        }
        catch (OperationCanceledException)
        {
            throw new RpcException(new Status(StatusCode.Cancelled, "Request cancelled by the client."));
        }
        catch (Exception)
        {
            throw new RpcException(new Status(StatusCode.Internal, "An internal server error occurred."));
        }
    }

    public override async Task<SearchCatalogResponse> SearchCatalog(SearchCatalogRequest request, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Search query cannot be empty."));
        }

        try
        {
            var page = request.Pagination?.Page > 0 ? request.Pagination.Page : 1;
            var pageSize = request.Pagination?.PageSize > 0 ? request.Pagination.PageSize : 10;

            var pagedResult = await _catalogService.Search.SearchAsync(request.Query, page, pageSize, context?.CancellationToken ?? default(CancellationToken));
            var response = new SearchCatalogResponse
            {
                Pagination = new PaginationResponse
                {
                    Page = pagedResult.Page,
                    PageSize = pagedResult.PageSize,
                    TotalCount = pagedResult.TotalCount
                }
            };

            foreach (var r in pagedResult.Items)
            {
                response.Results.Add(new SearchResultItemMessage
                {
                    Title = r.Title,
                    Description = r.Description,
                    Type = r.Type,
                    Url = r.Url
                });
            }

            return response;
        }
        catch (OperationCanceledException)
        {
            throw new RpcException(new Status(StatusCode.Cancelled, "Request cancelled by the client."));
        }
        catch (Exception)
        {
            throw new RpcException(new Status(StatusCode.Internal, "An internal server error occurred."));
        }
    }

    public override async Task<GetProductResponse> GetProductById(GetProductByIdRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.Id, out var id))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid product ID format."));
        }

        try
        {
            var product = await _catalogService.Products.GetProductByIdAsync(id, context?.CancellationToken ?? default(CancellationToken));
            if (product == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "Product not found."));
            }

            return new GetProductResponse
            {
                Product = new ProductItem
                {
                    Id = product.Id.ToString(),
                    Name = product.Name,
                    Slug = product.Slug,
                    Summary = product.Summary,
                    Price = (double)product.Price,
                    Currency = product.Currency ?? "IDR",
                    Category = product.Category.ToString()
                }
            };
        }
        catch (RpcException) { throw; }
        catch (OperationCanceledException)
        {
            throw new RpcException(new Status(StatusCode.Cancelled, "Request cancelled by the client."));
        }
        catch (Exception)
        {
            throw new RpcException(new Status(StatusCode.Internal, "An internal server error occurred."));
        }
    }

    public override async Task<GetProductResponse> GetProductBySlug(GetProductBySlugRequest request, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.Slug))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Slug cannot be empty."));
        }

        try
        {
            var product = await _catalogService.Products.GetProductBySlugAsync(request.Slug, context?.CancellationToken ?? default(CancellationToken));
            if (product == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "Product not found."));
            }

            return new GetProductResponse
            {
                Product = new ProductItem
                {
                    Id = product.Id.ToString(),
                    Name = product.Name,
                    Slug = product.Slug,
                    Summary = product.Summary,
                    Price = (double)product.Price,
                    Currency = product.Currency ?? "IDR",
                    Category = product.Category.ToString()
                }
            };
        }
        catch (RpcException) { throw; }
        catch (OperationCanceledException)
        {
            throw new RpcException(new Status(StatusCode.Cancelled, "Request cancelled by the client."));
        }
        catch (Exception)
        {
            throw new RpcException(new Status(StatusCode.Internal, "An internal server error occurred."));
        }
    }

    public override async Task<GetServiceResponse> GetServiceBySlug(GetServiceBySlugRequest request, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.Slug))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Slug cannot be empty."));
        }

        try
        {
            var service = await _catalogService.Services.GetServiceBySlugAsync(request.Slug, context?.CancellationToken ?? default(CancellationToken));
            if (service == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "Service not found."));
            }

            return new GetServiceResponse
            {
                Service = new ServiceItem
                {
                    Id = service.Id.ToString(),
                    Name = service.Name,
                    Slug = service.Slug,
                    Summary = service.Summary,
                    Description = service.Description,
                    IsFeatured = service.IsFeatured
                }
            };
        }
        catch (RpcException) { throw; }
        catch (OperationCanceledException)
        {
            throw new RpcException(new Status(StatusCode.Cancelled, "Request cancelled by the client."));
        }
        catch (Exception)
        {
            throw new RpcException(new Status(StatusCode.Internal, "An internal server error occurred."));
        }
    }

    public override async Task<GetPortfolioItemResponse> GetPortfolioById(GetPortfolioByIdRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.Id, out var id))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid portfolio ID format."));
        }

        try
        {
            var portfolio = await _catalogService.Portfolio.GetPortfolioByIdAsync(id, context?.CancellationToken ?? default(CancellationToken));
            if (portfolio == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "Portfolio not found."));
            }

            return new GetPortfolioItemResponse
            {
                Portfolio = new PortfolioItem
                {
                    Id = portfolio.Id.ToString(),
                    Title = portfolio.Title,
                    Summary = portfolio.Summary,
                    Problem = portfolio.Problem ?? string.Empty,
                    Solution = portfolio.Solution ?? string.Empty,
                    Result = portfolio.Result ?? string.Empty,
                    IsFeatured = portfolio.IsFeatured
                }
            };
        }
        catch (RpcException) { throw; }
        catch (OperationCanceledException)
        {
            throw new RpcException(new Status(StatusCode.Cancelled, "Request cancelled by the client."));
        }
        catch (Exception)
        {
            throw new RpcException(new Status(StatusCode.Internal, "An internal server error occurred."));
        }
    }
}
