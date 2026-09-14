using dagangOnline.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace dagangOnline.Controllers;

[Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
public class ApiManagementController : Controller
{
    [HttpGet("/ApiManagement")]
    public IActionResult Index()
    {
        ViewData["Title"] = "API & Endpoint Management";
        return View();
    }
}
