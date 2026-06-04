using System.Net.NetworkInformation;

namespace GLMS_API.Models
{
    public class Contract : ICloneable
    {
        public int ContractId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? ServiceLevel { get; set; }
        public string? SignedAgreementFilePath { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int ClientId { get; set; }
        public int StatusId { get; set; }

        public virtual Client? Client { get; set; }
        public virtual Status? Status { get; set; }
        public virtual ICollection<ServiceRequest> ServiceRequests { get; set; }
            = new List<ServiceRequest>();

        // Prototype clone 
        public object Clone() => new Contract
        {
            ClientId = ClientId,
            ServiceLevel = ServiceLevel,
            StatusId = StatusId,
            StartDate = DateTime.Now,
            EndDate = DateTime.Now.AddYears(1),
            SignedAgreementFilePath = null
        };
    }
}
