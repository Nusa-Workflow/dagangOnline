using System;
using System.Threading;
using System.Threading.Tasks;
using dagangOnline.Application.DTOs;
using dagangOnline.Domain;

namespace dagangOnline.Application.Interfaces;

// This acts as an aggregated facade for the catalog operations
public interface ICatalogService
{
    IProductService Products { get; }
    IServiceCatalogService Services { get; }
    IPortfolioService Portfolio { get; }
    ISearchService Search { get; }
}
