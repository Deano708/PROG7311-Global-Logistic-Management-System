using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROG7311GLMS.Models;
using PROG7311GLMS.Service;

//Title: Disclosure of AI Usage in my Assessment.
//• Section: ServiceRequestsController.
//• AI Tool: Gemini
//• Purpose/intention : Design and syntax implementation of ServiceRequestsController, including the USD to ZAR conversion and create service request functionality.
//• Date(s) 19/04/2026 to 22/04/2026.
//• https://gemini.google.com/app/3de15ef0f6ce635b. 

public class ServiceRequestsController : Controller
{
    private readonly ILogisticsFacade _facade;
    private readonly GlmsContext _context;

    public ServiceRequestsController(ILogisticsFacade facade, GlmsContext context)
    {
        _facade = facade;
        _context = context;
    }

    // GET: ServiceRequests/Index?contractId=5
    [HttpGet]
    public async Task<IActionResult> Index(int contractId)
    {
        var contract = await _context.Contracts
            .Include(c => c.Client)
            .Include(c => c.Status)
            .Include(c => c.ServiceRequests)
                .ThenInclude(sr => sr.Status)
            .FirstOrDefaultAsync(c => c.ContractId == contractId);

        if (contract == null) return NotFound();

        // Pass the contract to the view; the view will use contract.ServiceRequests for the list
        // and create a new ServiceRequest object for the form.
        ViewBag.Statuses = _context.Statuses.Where(s => s.Category == "ServiceRequest").ToList();
        return View(contract);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ServiceRequest request)
    {
        // Always calculate ZAR via Facade before attempting save
        request.CostZar = await _facade.ConvertUsdToZar(request.CostUsd);

        // Attempt creation
        bool isCreated = await _facade.CreateServiceRequest(request);

        if (!isCreated)
        {
            // Use TempData to send the warning back to the Index page
            TempData["Error"] = "Validation Failed: Requests can only be added to Active contracts.";
        }
        else
        {
            TempData["Success"] = "Service Request successfully logged.";
        }

        return RedirectToAction("Index", new { contractId = request.ContractId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int contractId)
    {
        var request = await _context.ServiceRequests.FindAsync(id);
        if (request != null)
        {
            _context.ServiceRequests.Remove(request);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Request removed.";
        }
        return RedirectToAction("Index", new { contractId = contractId });
    }
}