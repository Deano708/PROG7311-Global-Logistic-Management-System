namespace PROG7311GLMS.Models
{
    public class ServiceRequestDto
    {
        public int ServiceRequestId { get; set; }
        public string Description { get; set; } = "";
        public decimal CostUsd { get; set; }
        public decimal CostZar { get; set; }
        public string StatusName { get; set; } = "";
        public int StatusId { get; set; }
        public int ContractId { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    
}
