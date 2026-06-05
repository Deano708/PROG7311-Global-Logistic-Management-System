// =============================================================
//  GLMS_MVC / Controllers / ContractsController.cs
//
//  Replaces ALL direct EF Core / Facade calls with GlmsApiClient.
//  Views remain unchanged (same model shapes via ViewModels.cs).
// =============================================================

using PROG7311GLMS.Models;
using PROG7311GLMS.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;


namespace GLMS_MVC.Controllers;

[Authorize]
public class ContractsController : Controller
{
    private readonly GlmsApiClient _api;

    public ContractsController(GlmsApiClient api) => _api = api;

    // GET: /Contracts
    public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate, int? statusId)
    {
        var contracts = await _api.GetContractsAsync(startDate, endDate, statusId);
        ViewBag.Statuses = await _api.GetStatusesAsync("Contract");
        return View(contracts);
    }

    // GET: /Contracts/Create
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await PopulateCreateViewBags();
        return View(new CreateContractDto());
    }

    // POST: /Contracts/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateContractDto dto, IFormFile? pdfFile)
    {
        if (dto.ClientId == 0)
            ModelState.AddModelError("ClientId", "Client is required.");

        if (dto.EndDate <= dto.StartDate)
            ModelState.AddModelError("EndDate", "End Date must be after Start Date.");

        if (!ModelState.IsValid)
        {
            await PopulateCreateViewBags(dto.ClientId);
            return View(dto);
        }

        try
        {
            ContractDto? created;
            if (pdfFile != null && pdfFile.Length > 0)
            {
                await using var stream = pdfFile.OpenReadStream();
                created = await _api.CreateContractAsync(dto, stream, pdfFile.FileName);
            }
            else
            {
                created = await _api.CreateContractAsync(dto);
            }

            if (created == null)
            {
                ModelState.AddModelError("pdfFile", "Only PDF documents are allowed (Max 5MB).");
                await PopulateCreateViewBags(dto.ClientId);
                return View(dto);
            }

            TempData["Success"] = "Contract saved successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (HttpRequestException ex)
        {
            TempData["Error"] = "Failed to save contract: " + ex.Message;
            await PopulateCreateViewBags(dto.ClientId);
            return View(dto);
        }
    }

    // GET: /Contracts/Download/5
    public async Task<IActionResult> DownloadAgreement(int id)
    {
        var bytes = await _api.DownloadAgreementAsync(id);
        if (bytes == null) return NotFound();
        return File(bytes, "application/pdf", $"Contract_{id}.pdf");
    }

    // POST: /Contracts/Duplicate/5  (linked from view button)
    public async Task<IActionResult> Duplicate(int id)
    {
        var clone = await _api.DuplicateContractAsync(id);
        if (clone == null) return NotFound();
        TempData["Success"] = "Contract cloned successfully.";
        return RedirectToAction(nameof(Index));
    }

    // PATCH: /Contracts/UpdateStatus  (called via form post from UI)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, string statusName)
    {
        var updated = await _api.PatchContractStatusAsync(id, statusName);
        if (updated == null)
        {
            TempData["Error"] = "Could not update status.";
            return RedirectToAction(nameof(Index));
        }
        TempData["Success"] = $"Contract status updated to {statusName}.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Contracts/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var contract = await _api.GetContractAsync(id);
        if (contract == null) return NotFound();
        return View(contract);
    }

    // POST: /Contracts/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var success = await _api.DeleteContractAsync(id);
        TempData[success ? "Success" : "Error"] = success
            ? "Contract deleted successfully."
            : "Unable to delete contract – it may have linked service requests.";

        return RedirectToAction(nameof(Index));
    }

    // ── Helpers ───────────────────────────────────────────────

    private async Task PopulateCreateViewBags(int selectedClientId = 0)
    {
        var clients = await _api.GetClientsAsync();
        var statuses = await _api.GetStatusesAsync("Contract");
        ViewBag.ClientList = new SelectList(clients, "ClientId", "Name", selectedClientId);
        ViewBag.Statuses = statuses;
    }
}