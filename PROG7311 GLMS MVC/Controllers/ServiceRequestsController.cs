// =============================================================
//  GLMS_MVC / Controllers / ServiceRequestsController.cs
//  NO [Authorize] - protected by session guard in Program.cs
// =============================================================

using Microsoft.AspNetCore.Mvc;
using PROG7311GLMS.Models;
using PROG7311GLMS.Service;

namespace GLMS_MVC.Controllers;

public class ServiceRequestsController : Controller
{
    private readonly GlmsApiClient _api;

    public ServiceRequestsController(GlmsApiClient api) => _api = api;

    [HttpGet]
    public async Task<IActionResult> Index(int contractId)
    {
        var contract = await _api.GetContractAsync(contractId);
        if (contract == null) return NotFound();

        var requests = await _api.GetServiceRequestsAsync(contractId);
        var statuses = await _api.GetStatusesAsync("ServiceRequest");

        ViewBag.Statuses = statuses;

        var vm = new ContractServiceRequestsViewModel
        {
            Contract = contract,
            ServiceRequests = requests,
            NewRequest = new CreateServiceRequestDto { ContractId = contractId }
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateServiceRequestDto request)
    {
        var (success, _) = await _api.CreateServiceRequestAsync(request);

        TempData[success ? "Success" : "Error"] = success
            ? "Service Request successfully logged."
            : "Validation Failed: Requests can only be added to Active contracts.";

        return RedirectToAction("Index", new { contractId = request.ContractId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int contractId)
    {
        await _api.DeleteServiceRequestAsync(id);
        TempData["Success"] = "Request removed.";
        return RedirectToAction("Index", new { contractId });
    }
}