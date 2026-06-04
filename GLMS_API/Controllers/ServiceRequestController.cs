// =============================================================
//  GLMS_API / Controllers / ServiceRequestsController.cs
//
//  Endpoints:
//    GET    /api/servicerequests?contractId=5  – list for contract
//    POST   /api/servicerequests               – create (validates Active)
//    DELETE /api/servicerequests/{id}          – delete
//    GET    /api/servicerequests/{id}/convert  – USD → ZAR preview
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
public class ServiceRequestsController : ControllerBase
{
    private readonly ILogisticsService _service;
    private readonly ILogger<ServiceRequestsController> _logger;

    public ServiceRequestsController(ILogisticsService service,
        ILogger<ServiceRequestsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    // ── GET /api/servicerequests?contractId=5 ─────────────────
    /// <summary>Returns all service requests for a given contract.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ServiceRequestDto>), 200)]
    public async Task<IActionResult> Index([FromQuery] int contractId)
    {
        var requests = await _service.GetServiceRequestsByContractAsync(contractId);
        return Ok(requests);
    }

    // ── POST /api/servicerequests ─────────────────────────────
    /// <summary>
    /// Creates a service request. Only allowed on Active contracts.
    /// Currency is auto-converted from USD to ZAR using live rates.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ServiceRequestDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> Create([FromBody] CreateServiceRequestDto dto)
    {
        if (dto.ContractId == 0)
            return BadRequest(new { error = "ContractId is required." });

        if (string.IsNullOrWhiteSpace(dto.Description))
            return BadRequest(new { error = "Description is required." });

        if (dto.CostUsd <= 0)
            return BadRequest(new { error = "CostUsd must be greater than zero." });

        var (success, created) = await _service.CreateServiceRequestAsync(dto);

        if (!success)
            return UnprocessableEntity(new
            {
                error = "Requests can only be added to Active contracts."
            });

        return CreatedAtAction(nameof(Index),
            new { contractId = dto.ContractId }, created);
    }

    // ── DELETE /api/servicerequests/{id} ─────────────────────
    /// <summary>Deletes a service request by ID.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _service.DeleteServiceRequestAsync(id);
        return deleted ? NoContent() : NotFound();
    }

    // ── GET /api/servicerequests/convert?usd=100 ─────────────
    /// <summary>Converts a USD amount to ZAR using the live exchange rate (preview only).</summary>
    [HttpGet("convert")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> Convert([FromQuery] decimal usd)
    {
        if (usd <= 0) return BadRequest(new { error = "usd must be greater than zero." });
        var zar = await _service.ConvertUsdToZarAsync(usd);
        return Ok(new { usd, zar });
    }
}
