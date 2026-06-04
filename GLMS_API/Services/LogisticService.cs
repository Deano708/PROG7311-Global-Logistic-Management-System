// =============================================================
//  GLMS_API / Services / LogisticsService.cs
//  Business logic layer for the API – reuses all the logic
//  from the original LogisticsFacade in the old MVC project.
// =============================================================

using Microsoft.EntityFrameworkCore;
using GLMS_API.Models;
using GLMS_API.DTOs;
using System.Text.Json;

namespace GLMS_API.Services;

// ── Observer interfaces (preserved from original MVC) ─────────
public interface IStatusObserver
{
    void Update(string message);
}

public class ComplianceNotificationService : IStatusObserver
{
    private readonly ILogger<ComplianceNotificationService> _logger;
    public ComplianceNotificationService(ILogger<ComplianceNotificationService> logger)
        => _logger = logger;

    public void Update(string message)
        => _logger.LogWarning("[Compliance Alert]: {Message}", message);
}

// ── Service contract ──────────────────────────────────────────
public interface ILogisticsService
{
    // Contracts
    IEnumerable<ContractDto> FilterContracts(DateTime start, DateTime end, int? statusId);
    Task<ContractDto?> GetContractByIdAsync(int id);
    Task<ContractDto> CreateContractAsync(CreateContractDto dto, string? agreementFilePath);
    Task<ContractDto?> PatchContractStatusAsync(int id, string statusName);
    Task<ContractDto?> DuplicateContractAsync(int id);
    Task<bool> DeleteContractAsync(int id);

    // Agreements
    Task<string?> UploadAgreementAsync(IFormFile file);
    Task<(Stream stream, string fileName)?> DownloadAgreementAsync(int contractId);

    // Service Requests
    Task<IEnumerable<ServiceRequestDto>> GetServiceRequestsByContractAsync(int contractId);
    Task<(bool success, ServiceRequestDto? dto)> CreateServiceRequestAsync(CreateServiceRequestDto dto);
    Task<bool> DeleteServiceRequestAsync(int id);

    // Lookup
    Task<IEnumerable<ClientDto>> GetClientsAsync();
    Task<IEnumerable<StatusDto>> GetStatusesByCategoryAsync(string category);

    // Currency
    Task<decimal> ConvertUsdToZarAsync(decimal amountUsd);
}

// ── Implementation ────────────────────────────────────────────
public class LogisticsService : ILogisticsService
{
    private readonly GlmsContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LogisticsService> _logger;
    private readonly List<IStatusObserver> _observers = new();

    public LogisticsService(
        GlmsContext context,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<LogisticsService> logger)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;

        // Attach the compliance observer
        _observers.Add(new ComplianceNotificationService(
            Microsoft.Extensions.Logging
                .LoggerFactory.Create(b => b.AddConsole())
                .CreateLogger<ComplianceNotificationService>()));
    }

    private void Notify(string message) => _observers.ForEach(o => o.Update(message));

    // ─── Contracts ────────────────────────────────────────────

    public IEnumerable<ContractDto> FilterContracts(DateTime start, DateTime end, int? statusId)
    {
        var query = _context.Contracts
            .Include(c => c.Client)
            .Include(c => c.Status)
            .Where(c => c.StartDate <= end && c.EndDate >= start)
            .AsQueryable();

        if (statusId.HasValue)
            query = query.Where(c => c.StatusId == statusId.Value);

        return query.ToList().Select(MapContractToDto);
    }

    public async Task<ContractDto?> GetContractByIdAsync(int id)
    {
        var c = await _context.Contracts
            .Include(c => c.Client)
            .Include(c => c.Status)
            .Include(c => c.ServiceRequests).ThenInclude(sr => sr.Status)
            .FirstOrDefaultAsync(c => c.ContractId == id);

        return c == null ? null : MapContractToDto(c);
    }

    public async Task<ContractDto> CreateContractAsync(CreateContractDto dto, string? agreementFilePath)
    {
        // Compute status from dates (same logic as original ContractsController)
        var today = DateTime.Now.Date;
        string computedName = "Active";
        if (today < dto.StartDate.Date) computedName = "On-Hold";
        else if (today > dto.EndDate.Date) computedName = "Expired";

        var status = await _context.Statuses
            .FirstOrDefaultAsync(s => s.Category == "Contract" && s.StatusName == computedName)
            ?? await _context.Statuses.FirstAsync(s => s.Category == "Contract");

        var contract = new Contract
        {
            ClientId = dto.ClientId,
            ServiceLevel = dto.ServiceLevel,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            StatusId = status.StatusId,
            SignedAgreementFilePath = agreementFilePath,
            CreatedAt = DateTime.Now
        };

        _context.Contracts.Add(contract);
        await _context.SaveChangesAsync();

        await _context.Entry(contract).Reference(c => c.Client).LoadAsync();
        await _context.Entry(contract).Reference(c => c.Status).LoadAsync();

        return MapContractToDto(contract);
    }

    public async Task<ContractDto?> PatchContractStatusAsync(int id, string statusName)
    {
        var contract = await _context.Contracts
            .Include(c => c.Client)
            .Include(c => c.Status)
            .FirstOrDefaultAsync(c => c.ContractId == id);

        if (contract == null) return null;

        var newStatus = await _context.Statuses
            .FirstOrDefaultAsync(s => s.Category == "Contract" && s.StatusName == statusName);

        if (newStatus == null) return null;

        contract.StatusId = newStatus.StatusId;
        contract.Status = newStatus;
        await _context.SaveChangesAsync();

        Notify($"Contract {id} status changed to {statusName}");
        return MapContractToDto(contract);
    }

    public async Task<ContractDto?> DuplicateContractAsync(int id)
    {
        var existing = await _context.Contracts.FindAsync(id);
        if (existing == null) return null;

        // Prototype clone (same as original Clone() method)
        var clone = new Contract
        {
            ClientId = existing.ClientId,
            ServiceLevel = existing.ServiceLevel,
            StatusId = existing.StatusId,
            StartDate = DateTime.Now,
            EndDate = DateTime.Now.AddYears(1),
            SignedAgreementFilePath = null,
            CreatedAt = DateTime.Now
        };

        _context.Contracts.Add(clone);
        await _context.SaveChangesAsync();

        await _context.Entry(clone).Reference(c => c.Client).LoadAsync();
        await _context.Entry(clone).Reference(c => c.Status).LoadAsync();

        return MapContractToDto(clone);
    }

    public async Task<bool> DeleteContractAsync(int id)
    {
        var contract = await _context.Contracts.FindAsync(id);
        if (contract == null) return false;

        _context.Contracts.Remove(contract);
        await _context.SaveChangesAsync();
        return true;
    }

    // ─── Agreements ───────────────────────────────────────────

    public async Task<string?> UploadAgreementAsync(IFormFile file)
    {
        if (file == null || file.Length == 0) return null;

        var extension = Path.GetExtension(file.FileName)?.ToLower() ?? "";
        var contentType = file.ContentType?.ToLower() ?? "";

        if (extension != ".pdf" || contentType != "application/pdf") return null;
        if (file.Length > 5 * 1024 * 1024) return null;

        var uploadsRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        if (!Directory.Exists(uploadsRoot)) Directory.CreateDirectory(uploadsRoot);

        var safeFileName = Guid.NewGuid() + "_" + Path.GetFileName(file.FileName);
        var path = Path.Combine(uploadsRoot, safeFileName);

        await using var stream = new FileStream(path, FileMode.Create);
        await file.CopyToAsync(stream);

        return "/uploads/" + safeFileName;
    }

    public async Task<(Stream stream, string fileName)?> DownloadAgreementAsync(int contractId)
    {
        var contract = await _context.Contracts.FindAsync(contractId);
        if (contract == null || string.IsNullOrEmpty(contract.SignedAgreementFilePath))
            return null;

        // Build physical path from stored relative path
        var physicalPath = Path.Combine(
            Directory.GetCurrentDirectory(), "wwwroot",
            contract.SignedAgreementFilePath.TrimStart('/'));

        if (!File.Exists(physicalPath)) return null;

        var memory = new MemoryStream();
        await using (var fs = new FileStream(physicalPath, FileMode.Open, FileAccess.Read))
            await fs.CopyToAsync(memory);

        memory.Position = 0;
        return (memory, $"Contract_{contractId}.pdf");
    }

    // ─── Service Requests ─────────────────────────────────────

    public async Task<IEnumerable<ServiceRequestDto>> GetServiceRequestsByContractAsync(int contractId)
    {
        var requests = await _context.ServiceRequests
            .Include(sr => sr.Status)
            .Where(sr => sr.ContractId == contractId)
            .ToListAsync();

        return requests.Select(sr => new ServiceRequestDto
        {
            ServiceRequestId = sr.ServiceRequestId,
            Description = sr.Description,
            CostUsd = sr.CostUsd,
            CostZar = sr.CostZar,
            StatusName = sr.Status?.StatusName ?? "",
            StatusId = sr.StatusId,
            ContractId = sr.ContractId,
            CreatedAt = sr.CreatedAt
        });
    }

    public async Task<(bool success, ServiceRequestDto? dto)> CreateServiceRequestAsync(
        CreateServiceRequestDto dto)
    {
        // Validate the contract is Active (same guard as original Facade)
        var contract = await _context.Contracts
            .Include(c => c.Status)
            .FirstOrDefaultAsync(c => c.ContractId == dto.ContractId);

        if (contract == null ||
            contract.Status?.StatusName != "Active" ||
            contract.EndDate < DateTime.Now)
        {
            Notify($"Alert: Request attempted on non-active contract ID {dto.ContractId}");
            return (false, null);
        }

        var costZar = await ConvertUsdToZarAsync(dto.CostUsd);

        var pendingStatus = await _context.Statuses
            .FirstOrDefaultAsync(s => s.StatusName == "Pending" && s.Category == "ServiceRequest")
            ?? await _context.Statuses.FirstAsync(s => s.Category == "ServiceRequest");

        var request = new ServiceRequest
        {
            ContractId = dto.ContractId,
            Description = dto.Description,
            CostUsd = dto.CostUsd,
            CostZar = costZar,
            StatusId = pendingStatus.StatusId,
            CreatedAt = DateTime.Now
        };

        _context.ServiceRequests.Add(request);
        await _context.SaveChangesAsync();

        await _context.Entry(request).Reference(r => r.Status).LoadAsync();

        return (true, new ServiceRequestDto
        {
            ServiceRequestId = request.ServiceRequestId,
            Description = request.Description,
            CostUsd = request.CostUsd,
            CostZar = request.CostZar,
            StatusName = request.Status?.StatusName ?? "",
            StatusId = request.StatusId,
            ContractId = request.ContractId,
            CreatedAt = request.CreatedAt
        });
    }

    public async Task<bool> DeleteServiceRequestAsync(int id)
    {
        var sr = await _context.ServiceRequests.FindAsync(id);
        if (sr == null) return false;

        _context.ServiceRequests.Remove(sr);
        await _context.SaveChangesAsync();
        return true;
    }

    // ─── Lookup ───────────────────────────────────────────────

    public async Task<IEnumerable<ClientDto>> GetClientsAsync()
        => (await _context.Clients.ToListAsync())
            .Select(c => new ClientDto
            {
                ClientId = c.ClientId,
                Name = c.Name,
                ClientEmail = c.ClientEmail,
                Region = c.Region
            });

    public async Task<IEnumerable<StatusDto>> GetStatusesByCategoryAsync(string category)
        => (await _context.Statuses
                .Where(s => s.Category == category)
                .ToListAsync())
            .Select(s => new StatusDto
            {
                StatusId = s.StatusId,
                StatusName = s.StatusName,
                Category = s.Category,
                Description = s.Description
            });

    // ─── Currency Conversion ──────────────────────────────────

    public async Task<decimal> ConvertUsdToZarAsync(decimal amountUsd)
    {
        try
        {
            var apiKey = _configuration["ExchangeRateApi:ApiKey"];
            var baseUrl = _configuration["ExchangeRateApi:BaseUrl"];
            var url = $"{baseUrl}{apiKey}/latest/USD";

            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<ExchangeRateResponse>(json);

                if (data?.result == "success" &&
                    data.conversion_rates.TryGetValue("ZAR", out decimal zarRate))
                    return amountUsd * zarRate;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Currency API error");
        }

        return amountUsd * 18.50m; // Fallback rate
    }

    // ─── Mappers ──────────────────────────────────────────────

    private static ContractDto MapContractToDto(Contract c) => new()
    {
        ContractId = c.ContractId,
        ClientId = c.ClientId,
        ClientName = c.Client?.Name ?? "",
        ServiceLevel = c.ServiceLevel ?? "",
        StartDate = c.StartDate,
        EndDate = c.EndDate,
        StatusId = c.StatusId,
        StatusName = c.Status?.StatusName ?? "",
        SignedAgreementPath = c.SignedAgreementFilePath,
        CreatedAt = c.CreatedAt
    };
}

// Deserialization helper (reused from original FacadeService)
public class ExchangeRateResponse
{
    public string result { get; set; } = string.Empty;
    public Dictionary<string, decimal> conversion_rates { get; set; } = new();
}