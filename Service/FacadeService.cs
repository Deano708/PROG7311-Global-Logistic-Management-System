using PROG7311GLMS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Net.Http;
using System.IO;

namespace PROG7311GLMS.Service
{
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
        private readonly List<IStatusObserver> _observers = new();

        public LogisticsFacade(GlmsContext context) { _context = context; }

        // --- Observer Methods ---
        public void Attach(IStatusObserver observer) => _observers.Add(observer);
        public void Notify(string message) => _observers.ForEach(o => o.Update(message));

        // --- Validation & Logic ---
        public async Task<bool> CreateServiceRequest(ServiceRequest request)
        {
            // 1. Fetch the parent contract including its status
            var contract = await _context.Contracts
                .Include(c => c.Status)
                .FirstOrDefaultAsync(c => c.ContractId == request.ContractId);

            // 2. Strict Validation: Only "Active" status allowed
            // We also check dates just in case the status hasn't been updated yet
            if (contract == null ||
                contract.Status.StatusName != "Active" ||
                contract.EndDate < DateTime.Now)
            {
                // Log the denial or notify observers here if needed
                return false;
            }

            // 3. Save the request if validation passes
            _context.ServiceRequests.Add(request);
            await _context.SaveChangesAsync();
            return true;
        }

        // --- LINQ Filter Mechanism ---
        public IEnumerable<Contract> FilterContracts(DateTime start, DateTime end, int? statusId)
        {
            // Include related navigation properties so views can access Client and Status without null refs
            var query = _context.Contracts
                .Include(c => c.Client)
                .Include(c => c.Status)
                .AsQueryable();

            // Use overlapping-range filter so contracts that intersect the requested range are returned
            query = query.Where(c => c.StartDate <= end && c.EndDate >= start);

            if (statusId.HasValue)
                query = query.Where(c => c.StatusId == statusId.Value);

            return query.ToList();
        }

        // --- Currency Conversion (External API) ---
        public async Task<decimal> ConvertUsdToZar(decimal amountUsd)
        {
            using var client = new HttpClient();
            // Example using a free API like ExchangeRate-API or similar
            var response = await client.GetStringAsync("https://api.exchangerate-api.com/v4/latest/USD");
            // Logic to parse JSON and return (amountUsd * zarRate)
            return amountUsd * 18.50m; // Mocked for brevity
        }

        // --- File Simulation ---
        public async Task<string> UploadAgreement(IFormFile file)
        {
            if (file == null) return null;
            var uploadsRoot = Path.Combine("wwwroot", "uploads");
            Directory.CreateDirectory(uploadsRoot);

            var safeFileName = Guid.NewGuid() + "_" + Path.GetFileName(file.FileName);
            var path = Path.Combine(uploadsRoot, safeFileName);

            using (var stream = new FileStream(path, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return path;
        }
    }
}
