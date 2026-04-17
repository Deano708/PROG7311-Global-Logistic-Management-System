using PROG7311GLMS.Models;

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
            var contract = await _context.Contracts.FindAsync(request.ContractId);
            var status = await _context.Statuses.FindAsync(contract.StatusId);

            // Logic: Cannot create if Expired (Assume ID 3) or On-Hold (Assume ID 4)
            if (status.StatusName == "Expired" || status.StatusName == "On-Hold" || contract.EndDate < DateTime.Now)
            {
                Notify($"Request denied for Contract {contract.ContractId}: Contract is inactive.");
                return false;
            }

            _context.ServiceRequests.Add(request);
            await _context.SaveChangesAsync();
            return true;
        }

        // --- LINQ Filter Mechanism ---
        public IEnumerable<Contract> FilterContracts(DateTime start, DateTime end, int? statusId)
        {
            var query = _context.Contracts.AsQueryable();

            query = query.Where(c => c.StartDate >= start && c.EndDate <= end);

            if (statusId.HasValue)
                query = query.Where(c => c.StatusId == statusId);

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
            var path = Path.Combine("wwwroot/uploads", Guid.NewGuid() + "_" + file.FileName);
            using (var stream = new FileStream(path, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            return path;
        }
    }
}
