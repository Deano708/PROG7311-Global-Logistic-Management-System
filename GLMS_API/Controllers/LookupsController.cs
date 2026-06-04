// =============================================================
//  GLMS_API / Controllers / LookupsController.cs
//  Provides reference data (clients, statuses) consumed by the MVC.
// =============================================================

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GLMS_API.DTOs;
using GLMS_API.Services;

namespace GLMS_API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class LookupsController : ControllerBase
{
    private readonly ILogisticsService _service;

    public LookupsController(ILogisticsService service) => _service = service;

    // GET /api/lookups/clients
    /// <summary>Returns all clients for use in dropdowns.</summary>
    [HttpGet("clients")]
    [ProducesResponseType(typeof(IEnumerable<ClientDto>), 200)]
    public async Task<IActionResult> Clients()
        => Ok(await _service.GetClientsAsync());

    // GET /api/lookups/statuses?category=Contract
    /// <summary>Returns statuses filtered by category (Contract | ServiceRequest).</summary>
    [HttpGet("statuses")]
    [ProducesResponseType(typeof(IEnumerable<StatusDto>), 200)]
    public async Task<IActionResult> Statuses([FromQuery] string category = "Contract")
        => Ok(await _service.GetStatusesByCategoryAsync(category));
}