namespace PROG7311GLMS.Models
{
    public class StatusDto
    {
        public int StatusId { get; set; }
        public string StatusName { get; set; } = "";
        public string Category { get; set; } = "";
        public string? Description { get; set; }
    }
}
