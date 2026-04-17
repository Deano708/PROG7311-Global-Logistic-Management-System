using Microsoft.AspNetCore.Mvc;
using PROG7311GLMS.Models;
using PROG7311GLMS.Service;

public class ContractsController : Controller
{
    private readonly ILogisticsFacade _facade;
    private readonly GlmsContext _context;

    public ContractsController(ILogisticsFacade facade, GlmsContext context)
    {
        _facade = facade;
        _context = context;
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
        if (ModelState.IsValid)
        {
            // Handle PDF Upload via Facade
            if (pdfFile != null)
            {
                contract.SignedAgreementFilePath = await _facade.UploadAgreement(pdfFile);
            }

            _context.Add(contract);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(contract);
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
}
