using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using dagangOnline.Domain.Chat;
using dagangOnline.Models;
using Microsoft.AspNetCore.Identity;

using dagangOnline.Application.Interfaces;

using dagangOnline.Application.Services.Economic;

namespace dagangOnline.Application.Services;

public class GraphContextBuilder
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICatalogService _catalogService;
    private readonly EconomicGraphEngine _economicGraphEngine;

    public GraphContextBuilder(
        UserManager<ApplicationUser> userManager,
        ICatalogService catalogService,
        EconomicGraphEngine economicGraphEngine)
    {
        _userManager = userManager;
        _catalogService = catalogService;
        _economicGraphEngine = economicGraphEngine;
    }

    public async Task<string> BuildGraphContextAsync(Conversation session, CancellationToken cancellationToken = default)
    {
        var nodes = new List<object>();
        var edges = new List<object>();

        // 1. User Node
        var user = await _userManager.FindByIdAsync(session.CustomerId);
        string userRole = "Guest";
        if (user != null)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userRole = roles.FirstOrDefault() ?? "User";
            nodes.Add(new { id = user.Id, type = "User", role = userRole, name = user.DisplayName });
        }
        else
        {
            // Anonymous or unauthenticated user
            nodes.Add(new { id = session.CustomerId, type = "User", role = "Guest", name = "Anonymous Guest" });
        }

        // 2. Capabilities based on Role
        if (userRole == "Mitra")
        {
            edges.Add(new { source = session.CustomerId, relation = "CAN_CREATE_PRODUCTS", target = "Platform" });
        }
        else if (userRole == "Admin")
        {
            edges.Add(new { source = session.CustomerId, relation = "CAN_MANAGE_ALL", target = "Platform" });
        }
        else if (userRole == "Guest")
        {
            edges.Add(new { source = session.CustomerId, relation = "CAN_VIEW_PUBLIC_ONLY", target = "Platform" });
        }

        // 3. Current Conversation Node
        nodes.Add(new { id = session.Id.ToString(), type = "Conversation", status = session.Status.ToString(), intent = session.Intent.ToString() });
        edges.Add(new { source = session.CustomerId, relation = "INVOLVED_IN", target = session.Id.ToString() });

        // 4. Products & Services (Simplified Graph representation)
        // In a real scenario, we'd filter this or use vector search. For now, add top 10 products as nodes.
        var products = await _catalogService.Products.GetPublishedProductsAsync(null, 1, 10, cancellationToken);
        foreach (var p in products.Items)
        {
            var pId = $"P_{p.Id}";
            nodes.Add(new { id = pId, type = "Product", name = p.Name, price = p.Price, category = p.Category.ToString() });
            edges.Add(new { source = "Platform", relation = "OFFERS", target = pId });
        }

        var services = await _catalogService.Services.GetPublishedServicesAsync(1, 10, cancellationToken);
        foreach (var s in services.Items)
        {
            var sId = $"S_{s.Id}";
            nodes.Add(new { id = sId, type = "Service", name = s.Name });
            edges.Add(new { source = "Platform", relation = "OFFERS", target = sId });
        }

        // 5. Macroeconomic Indicators & Commodity Shocks from EconomicGraphEngine
        var econNodes = _economicGraphEngine.GetAllNodes();
        foreach (var econNode in econNodes.Take(8))
        {
            nodes.Add(new
            {
                id = econNode.Id,
                type = econNode.Category.ToString(),
                name = econNode.Label,
                value = $"{econNode.CurrentValue} {econNode.Unit}",
                sentiment = econNode.SentimentScore
            });
        }

        var econEdges = _economicGraphEngine.GetAllEdges();
        foreach (var econEdge in econEdges.Take(10))
        {
            edges.Add(new
            {
                source = econEdge.SourceId,
                relation = econEdge.RelationType,
                target = econEdge.TargetId,
                weight = econEdge.Weight,
                lagMonths = econEdge.LeadLagMonths
            });
        }

        var graph = new
        {
            graph_context = new
            {
                nodes = nodes,
                edges = edges
            }
        };

        return JsonSerializer.Serialize(graph, new JsonSerializerOptions { WriteIndented = true });
    }
}
