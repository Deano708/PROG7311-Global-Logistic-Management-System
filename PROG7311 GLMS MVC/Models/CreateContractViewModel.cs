namespace PROG7311GLMS.Models
{
    public class CreateContractDto
    {
        public int ClientId { get; set; }
        public string ServiceLevel { get; set; } = "";
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
