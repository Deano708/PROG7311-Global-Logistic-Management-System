using PROG7311GLMS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Net.Http;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

//Title: Disclosure of AI Usage in my Assessment.
//• Section: FacadeService.
//• Purpose/intention : Design and syntax implementation of FacadeService, including the currency conversion method and LINQ filtering mechanism.
//• Date(s) 19/04/2026 to 22/04/2026.
//• https://gemini.google.com/app/3de15ef0f6ce635b. 



namespace PROG7311GLMS.Service
{
    // DTO for JSON Deserialization
    public class ExchangeRateResponse
    {
        public string result { get; set; }
        public Dictionary<string, decimal> conversion_rates { get; set; }
    }

    public interface ILogisticsFacade
    {
        Task<bool> CreateServiceRequest(ServiceRequest request);
        Task<string> UploadAgreement(IFormFile file);
        Task<decimal> ConvertUsdToZar(decimal amountUsd);
        IEnumerable<Contract> FilterContracts(DateTime start, DateTime end, int? statusId);
    }

    public class LogisticsFacade : ILogisticsFacade, IStatusSubject
    {
        private readonly GlmsContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly List<IStatusObserver> _observers = new();

        // Updated Constructor with Injection
        public LogisticsFacade(GlmsContext context, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        // --- Observer Methods ---
        public void Attach(IStatusObserver observer) => _observers.Add(observer);
        public void Notify(string message) => _observers.ForEach(o => o.Update(message));

        // --- Validation & Logic ---
        public async Task<bool> CreateServiceRequest(ServiceRequest request)
        {
            // 1. Validate the Contract is Active
            var contract = await _context.Contracts
                .Include(c => c.Status)
                .FirstOrDefaultAsync(c => c.ContractId == request.ContractId);

            if (contract == null || contract.Status.StatusName != "Active" || contract.EndDate < DateTime.Now)
            {
                Notify($"Alert: Attempted to add request to non-active contract ID {request.ContractId}");
                return false;
            }

            var pendingStatus = await _context.Statuses
                .FirstOrDefaultAsync(s => s.StatusName == "Pending" && s.Category == "ServiceRequest");

            if (pendingStatus != null)
            {
                request.StatusId = pendingStatus.StatusId;
            }
            else
            {
                // Safety fallback: if 'Pending' is missing, find the first status in that category
                var fallback = await _context.Statuses.FirstOrDefaultAsync(s => s.Category == "ServiceRequest");
                if (fallback != null) request.StatusId = fallback.StatusId;
            }

            // Save the request
            _context.ServiceRequests.Add(request);
            await _context.SaveChangesAsync();
            return true;
        }

        // --- LINQ Filter Mechanism ---
        public IEnumerable<Contract> FilterContracts(DateTime start, DateTime end, int? statusId)
        {
            var query = _context.Contracts
                .Include(c => c.Client)
                .Include(c => c.Status)
                .AsQueryable();

            query = query.Where(c => c.StartDate <= end && c.EndDate >= start);

            if (statusId.HasValue)
                query = query.Where(c => c.StatusId == statusId.Value);

            return query.ToList();
        }

        // --- Currency Conversion (ExchangeRate-API Integration) ---
        public async Task<decimal> ConvertUsdToZar(decimal amountUsd)
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

                    if (data?.result == "success" && data.conversion_rates.TryGetValue("ZAR", out decimal zarRate))
                    {
                        return amountUsd * zarRate;
                    }
                }
            }
            catch (Exception ex)
            {
                // In production, log this with ILogger. For now, we fallback to a safe estimate.
                Console.WriteLine($"Currency API Error: {ex.Message}");
            }

            // Fallback rate if the API is down or key is invalid (Standard ZAR/USD approx)
            return amountUsd * 18.50m;
        }

        // --- File Storage Logic ---
        public async Task<string> UploadAgreement(IFormFile file)
        {
            if (file == null) return null;

            var uploadsRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadsRoot)) Directory.CreateDirectory(uploadsRoot);

            var safeFileName = Guid.NewGuid() + "_" + Path.GetFileName(file.FileName);
            var path = Path.Combine(uploadsRoot, safeFileName);

            using (var stream = new FileStream(path, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Return relative path for database storage
            return "/uploads/" + safeFileName;
        }
    }
}