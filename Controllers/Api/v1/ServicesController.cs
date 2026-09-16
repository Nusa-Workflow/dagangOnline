using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using dagangOnline.Application.DTOs;
using dagangOnline.Application.Interfaces;
using dagangOnline.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace dagangOnline.Controllers.Api.v1;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class ServicesController : ControllerBase
{
    private readonly ICatalogService _catalogService;

    public ServicesController(ICatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<ServiceDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 100, CancellationToken cancellationToken = default)
    {
        var result = await _catalogService.Services.GetPublishedServicesAsync(page, pageSize, cancellationToken);
        return Ok(ApiResponse<List<ServiceDto>>.Ok(result.Items));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ServiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var dto = await _catalogService.Services.GetServiceByIdAsync(id, cancellationToken);
        if (dto == null)
        {
            return NotFound(Problem(statusCode: StatusCodes.Status404NotFound, title: "Layanan tidak ditemukan.", detail: $"Layanan dengan ID {id} tidak terdaftar."));
        }
        return Ok(ApiResponse<ServiceDto>.Ok(dto));
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
    [ProducesResponseType(typeof(ApiResponse<ServiceDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateServiceDto input, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var currentUserName = User.Identity?.Name;
        var dto = await _catalogService.Services.CreateServiceAsync(input, currentUserName, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, ApiResponse<ServiceDto>.Ok(dto, "Layanan berhasil dibuat."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
    [ProducesResponseType(typeof(ApiResponse<ServiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateServiceDto input, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var currentUserName = User.Identity?.Name;
        try
        {
            var dto = await _catalogService.Services.UpdateServiceAsync(id, input, currentUserName, cancellationToken);
            return Ok(ApiResponse<ServiceDto>.Ok(dto, "Layanan berhasil diperbarui."));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(Problem(statusCode: StatusCodes.Status404NotFound, title: "Layanan tidak ditemukan."));
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var currentUserName = User.Identity?.Name;
        try
        {
            await _catalogService.Services.DeleteServiceAsync(id, currentUserName, cancellationToken);
            return Ok(ApiResponse<bool>.Ok(true, "Layanan berhasil dihapus."));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(Problem(statusCode: StatusCodes.Status404NotFound, title: "Layanan tidak ditemukan."));
        }
    }
}
