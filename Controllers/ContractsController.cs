using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PROG7311GLMS.Models;
using PROG7311GLMS.Service;
using System.Linq;
using System.Text.Json;

//Title: Disclosure of AI Usage in my Assessment.
//• Section: ContractsController.
//• AI Tool: Gemini
//• Purpose/intention : Design and syntax implementation of ContractsController, including the PDF upload and contract creation functionality.
//• Date(s) 19/04/2026 to 22/04/2026.
//• https://gemini.google.com/app/3de15ef0f6ce635b. 


public class ContractsController : Controller
{
    private readonly ILogisticsFacade _facade;
    private readonly GlmsContext _context;
    private readonly ILogger<ContractsController> _logger;

    public ContractsController(ILogisticsFacade facade, GlmsContext context, ILogger<ContractsController> logger)
    {
        _facade = facade;
        _context = context;
        _logger = logger;
    }

    // GET: Contracts (with Search & Filter)
    public IActionResult Index(DateTime? startDate, DateTime? endDate, int? statusId)
    {
        // Use the Facade to perform the LINQ filtering logic
        var contracts = _facade.FilterContracts(
            startDate ?? DateTime.MinValue,
            endDate ?? DateTime.MaxValue,
            statusId
        );

        ViewBag.Statuses = _context.Statuses.Where(s => s.Category == "Contract").ToList();
        return View(contracts);
    }

    // POST: Contracts/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Contract contract, IFormFile pdfFile)
    {
        // Compute contract status based on dates BEFORE validating ModelState so StatusId isn't treated as missing
        if (contract != null)
        {
            var today = DateTime.Now.Date;
            string computedName = "Active";
            if (today < contract.StartDate.Date)
                computedName = "On-Hold";
            else if (today > contract.EndDate.Date)
                computedName = "Expired";

            var status = _context.Statuses.FirstOrDefault(s => s.Category == "Contract" && s.StatusName == computedName);
            if (status != null)
            {
                contract.StatusId = status.StatusId;
                // Remove any modelstate entries for StatusId so validation uses the computed value
                if (ModelState.ContainsKey(nameof(contract.StatusId)))
                    ModelState.Remove(nameof(contract.StatusId));
            }
        }

        // Ensure a client was selected
        if (contract == null || contract.ClientId == 0)
        {
            ModelState.AddModelError("ClientId", "Client is required.");
        }

        // Validate model now
        if (!ModelState.IsValid)
        {
            var modelErrors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .Where(m => !string.IsNullOrEmpty(m))
                .ToList();

            if (modelErrors.Any())
            {
                TempData["ModelErrors"] = JsonSerializer.Serialize(modelErrors);
                _logger.LogWarning("Contract Create validation failed: {Errors}", string.Join("; ", modelErrors));
            }

            ViewBag.ClientList = new SelectList(_context.Clients.ToList(), "ClientId", "Name", contract?.ClientId);
            ViewBag.Statuses = _context.Statuses.Where(s => s.Category == "Contract").ToList();
            return View(contract);
        }

        try
        {
            if (pdfFile != null)
            {
                var filePath = await _facade.UploadAgreement(pdfFile);

                if (filePath == null)
                {
                    ModelState.AddModelError("pdfFile", "Only PDF documents are allowed (Max 5MB).");
                    // Re-populate ViewBags and return view
                    ViewBag.ClientList = new SelectList(_context.Clients.ToList(), "ClientId", "Name");
                    return View(contract);
                }

                contract.SignedAgreementFilePath = filePath;
            }

            _context.Add(contract);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Contract saved successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving contract");
            TempData["Error"] = "Failed to save contract: " + ex.Message;
            TempData["ErrorDetails"] = ex.ToString();
            ViewBag.ClientList = new SelectList(_context.Clients.ToList(), "ClientId", "Name", contract?.ClientId);
            ViewBag.Statuses = _context.Statuses.Where(s => s.Category == "Contract").ToList();
            return View(contract);
        }
    }

    // GET: Contracts/Create
    [HttpGet]
    public IActionResult Create()
    {
        ViewBag.ClientList = new SelectList(_context.Clients.ToList(), "ClientId", "Name");
        ViewBag.Statuses = _context.Statuses.Where(s => s.Category == "Contract").ToList();
        return View();
    }

    // GET: Contracts/Download/5
    public async Task<IActionResult> DownloadAgreement(int id)
    {
        var contract = await _context.Contracts.FindAsync(id);
        if (contract == null || string.IsNullOrEmpty(contract.SignedAgreementFilePath))
            return NotFound();

        var memory = new MemoryStream();
        using (var stream = new FileStream(contract.SignedAgreementFilePath, FileMode.Open))
        {
            await stream.CopyToAsync(memory);
        }
        memory.Position = 0;

        // Return the file to the browser
        return File(memory, "application/pdf", $"Contract_{id}.pdf");
    }

    // Prototype Pattern: Cloning a contract
    public async Task<IActionResult> Duplicate(int id)
    {
        var existing = await _context.Contracts.FindAsync(id);
        if (existing == null) return NotFound();

        // Use the Prototype Clone method
        var newContract = (Contract)existing.Clone();

        _context.Contracts.Add(newContract);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // GET: Contracts/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var contract = await _context.Contracts
            .Include(c => c.Client)
            .Include(c => c.Status)
            .FirstOrDefaultAsync(m => m.ContractId == id);

        if (contract == null) return NotFound();

        return View(contract);
    }

    // POST: Contracts/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            var contract = await _context.Contracts.FindAsync(id);
            if (contract != null)
            {
                _context.Contracts.Remove(contract);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Contract deleted successfully.";
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Unable to delete contract. It might be linked to existing Service Requests.";
            _logger.LogError(ex, "Error deleting contract {Id}", id);
        }
        return RedirectToAction(nameof(Index));
    }

}
