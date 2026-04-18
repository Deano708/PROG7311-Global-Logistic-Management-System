using Microsoft.AspNetCore.Mvc;
using PROG7311GLMS.Models;
using PROG7311GLMS.Service;

public class ServiceRequestsController : Controller
{
    private readonly ILogisticsFacade _facade;

    public ServiceRequestsController(ILogisticsFacade facade)
    {
        _facade = facade;
    }

    // GET: ServiceRequests/Create
    [HttpGet]
    public IActionResult Create(int? contractId)
    {
        var model = new ServiceRequest();
        if (contractId.HasValue)
            model.ContractId = contractId.Value;

        return View(model);
    }

    // GET: ServiceRequests (simple redirect to Contracts list for now)
    [HttpGet]
    public IActionResult Index()
    {
        return RedirectToAction("Index", "Contracts");
    }

    [HttpPost]
    public async Task<IActionResult> Create(ServiceRequest request)
    {
        // 1. Handle Currency Conversion via Facade
        request.CostZar = await _facade.ConvertUsdToZar(request.CostUsd);

        // 2. Perform Validation & Save via Facade
        // This checks if the parent contract is Expired/On-Hold
        bool isCreated = await _facade.CreateServiceRequest(request);

        if (!isCreated)
        {
            // If validation failed, the Facade already triggered the Observer/Notify logic
            TempData["Error"] = "Cannot create request: Contract is inactive or expired.";
            return RedirectToAction("Details", "Contracts", new { id = request.ContractId });
        }

        return RedirectToAction("Index", "Home");
    }
}
