namespace PROG7311GLMS.Models
{
    public class CreateServiceRequestDto
    {
        public int ContractId { get; set; }
        public string Description { get; set; } = "";
        public decimal CostUsd { get; set; }
    }
}
