using System;
using System.Collections.Generic;

namespace PROG7311GLMS.Models;

public partial class Status
{
    public int StatusId { get; set; }

    public string StatusName { get; set; } = null!;

    public string Category { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<Contract> Contracts { get; set; } = new List<Contract>();

    public virtual ICollection<ServiceRequest> ServiceRequests { get; set; } = new List<ServiceRequest>();
}
