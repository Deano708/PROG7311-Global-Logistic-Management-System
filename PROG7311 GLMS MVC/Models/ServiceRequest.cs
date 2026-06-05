using System;
using System.Collections.Generic;

namespace PROG7311GLMS.Models;

public partial class ServiceRequest
{
    public int ServiceRequestId { get; set; }

    public string Description { get; set; } = null!;

    public decimal CostUsd { get; set; }

    public decimal CostZar { get; set; }

    public DateTime? CreatedAt { get; set; }

    public int ContractId { get; set; }

    public int StatusId { get; set; }

    public virtual Contract Contract { get; set; } = null!;

    public virtual Status Status { get; set; } = null!;
}
