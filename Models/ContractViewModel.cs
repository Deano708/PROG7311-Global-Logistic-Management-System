namespace PROG7311GLMS.Models
{
    public class ContractDto
    {
        public int ContractId { get; set; }
        public string ClientName { get; set; } = "";
        public int ClientId { get; set; }
        public string ServiceLevel { get; set; } = "";
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string StatusName { get; set; } = "";
        public int StatusId { get; set; }
        public string? SignedAgreementPath { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    
}
