namespace PROG7311GLMS.Models
{
    public class ContractServiceRequestsViewModel
    {
        public ContractDto Contract { get; set; } = new();
        public List<ServiceRequestDto> ServiceRequests { get; set; } = new();
        public CreateServiceRequestDto NewRequest { get; set; } = new();
    }
}
