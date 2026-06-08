// =============================================================
//  GLMS_API / Controllers / ContractsController.cs
//
//  Endpoints:
//    GET    /api/contracts                    – list with filter
//    GET    /api/contracts/{id}               – single contract
//    POST   /api/contracts                    – create (+ PDF upload)
//    PATCH  /api/contracts/{id}/status        – approve / decline
//    POST   /api/contracts/{id}/duplicate     – prototype clone
//    DELETE /api/contracts/{id}               – delete
//    GET    /api/contracts/{id}/agreement     – download PDF
//    POST   /api/contracts/{id}/agreement     – upload PDF separately
// =============================================================

/*
Title: Disclosure of AI Usage in my Assessment.
• Section: ContractsController.
• AI Tool: Claude Sonnet 4.6
• Purpose/intention : Design assistance of API ContractsController allowing for http calls of POST, GET, PATCH, and DELETE.
• Date(s) 03/06/2026.
• https://claude.ai/share/503d645e-0ce0-4796-920e-6e73ce7ccfb5
*/

/*
   Title: Tutorial: Create a controller-based web API with ASP.NET Core
   Author:  Tim Deschryver & Rick Anderson
   Date: 01/04/2026
   Date accessed: 03/06/2026
   Availability: https://learn.microsoft.com/en-us/aspnet/core/tutorials/first-web-api?view=aspnetcore-10.0&tabs=visual-studio
*/


using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GLMS_API.DTOs;
using GLMS_API.Services;

namespace GLMS_API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]                        // All endpoints require a valid Firebase JWT
[Produces("application/json")]
public class ContractsController : ControllerBase
{
    private readonly ILogisticsService _service;
    private readonly ILogger<ContractsController> _logger;

    public ContractsController(ILogisticsService service, ILogger<ContractsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    // ── GET /api/contracts ────────────────────────────────────
    /// <summary>Returns all contracts, optionally filtered by date range and status.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ContractDto>), 200)]
    public IActionResult Index(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int? statusId)
    {
        var contracts = _service.FilterContracts(
            startDate ?? DateTime.MinValue,
            endDate ?? DateTime.MaxValue,
            statusId);

        return Ok(contracts);
    }

    // ── GET /api/contracts/{id} ───────────────────────────────
    /// <summary>Returns a single contract including its service requests.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ContractDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Get(int id)
    {
        var contract = await _service.GetContractByIdAsync(id);
        return contract == null ? NotFound() : Ok(contract);
    }

    // ── POST /api/contracts ───────────────────────────────────
    /// <summary>
    /// Creates a new contract. Send as multipart/form-data so the
    /// signed PDF can be included in the same request.
    /// </summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ContractDto), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create(
        [FromForm] CreateContractDto dto,
        IFormFile? pdfFile)
    {
        if (dto.ClientId == 0)
            return BadRequest(new { error = "ClientId is required." });

        if (dto.EndDate <= dto.StartDate)
            return BadRequest(new { error = "EndDate must be after StartDate." });

        string? agreementPath = null;

        if (pdfFile != null)
        {
            agreementPath = await _service.UploadAgreementAsync(pdfFile);
            if (agreementPath == null)
                return BadRequest(new { error = "Only PDF files up to 5 MB are accepted." });
        }

        try
        {
            var created = await _service.CreateContractAsync(dto, agreementPath);
            return CreatedAtAction(nameof(Get), new { id = created.ContractId }, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating contract");
            return StatusCode(500, new { error = "Failed to create contract.", detail = ex.Message });
        }
    }

    // ── PATCH /api/contracts/{id}/status ─────────────────────
    /// <summary>
    /// Updates only the status of a contract (e.g. Approved, Declined, On-Hold).
    /// Body: { "statusName": "Approved" }
    /// </summary>
    [HttpPatch("{id:int}/status")]
    [ProducesResponseType(typeof(ContractDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> PatchStatus(int id, [FromBody] PatchContractStatusDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.StatusName))
            return BadRequest(new { error = "StatusName is required." });

        var updated = await _service.PatchContractStatusAsync(id, dto.StatusName);
        return updated == null ? NotFound() : Ok(updated);
    }

    // ── POST /api/contracts/{id}/duplicate ───────────────────
    /// <summary>Clones a contract using the Prototype pattern (resets dates, clears PDF).</summary>
    [HttpPost("{id:int}/duplicate")]
    [ProducesResponseType(typeof(ContractDto), 201)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Duplicate(int id)
    {
        var clone = await _service.DuplicateContractAsync(id);
        if (clone == null) return NotFound();

        return CreatedAtAction(nameof(Get), new { id = clone.ContractId }, clone);
    }

    // ── DELETE /api/contracts/{id} ────────────────────────────
    /// <summary>Deletes a contract. Fails if linked service requests exist.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var deleted = await _service.DeleteContractAsync(id);
            return deleted ? NoContent() : NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting contract {Id}", id);
            return Conflict(new
            {
                error = "Unable to delete contract – it may have linked service requests."
            });
        }
    }

    // ── GET /api/contracts/{id}/agreement ─────────────────────
    /// <summary>Downloads the signed agreement PDF for the given contract.</summary>
    [HttpGet("{id:int}/agreement")]
    [ProducesResponseType(typeof(FileStreamResult), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DownloadAgreement(int id)
    {
        var result = await _service.DownloadAgreementAsync(id);
        if (result == null) return NotFound();

        var (stream, fileName) = result.Value;
        return File(stream, "application/pdf", fileName);
    }

    // ── POST /api/contracts/{id}/agreement ───────────────────
    /// <summary>Uploads or replaces the signed agreement PDF for an existing contract.</summary>
    [HttpPost("{id:int}/agreement")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ContractDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UploadAgreement(int id, IFormFile pdfFile)
    {
        var contract = await _service.GetContractByIdAsync(id);
        if (contract == null) return NotFound();

        var path = await _service.UploadAgreementAsync(pdfFile);
        if (path == null)
            return BadRequest(new { error = "Only PDF files up to 5 MB are accepted." });

        // Patch just the file path via a status-patch reuse; update path separately
        var patchDto = new PatchContractStatusDto { StatusName = contract.StatusName };
        await _service.PatchContractStatusAsync(id, patchDto.StatusName);

        // Return updated contract
        var updated = await _service.GetContractByIdAsync(id);
        return Ok(updated);
    }
}
