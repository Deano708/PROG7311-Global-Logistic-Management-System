using System;
using System.Collections.Generic;

namespace PROG7311GLMS.Models;

public partial class Contract
{
    public int ContractId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string? ServiceLevel { get; set; }

    public string? SignedAgreementFilePath { get; set; }

    public DateTime? CreatedAt { get; set; }

    public int ClientId { get; set; }

    public int StatusId { get; set; }

    public virtual Client Client { get; set; } = null!;

    public virtual ICollection<ServiceRequest> ServiceRequests { get; set; } = new List<ServiceRequest>();

    public virtual Status Status { get; set; } = null!;
}

// Prototype partial class
public partial class Contract : ICloneable
{
    public object Clone()
    {
        return new Contract
        {
            ClientId = this.ClientId,
            ServiceLevel = this.ServiceLevel,
            StatusId = this.StatusId,
            // Reset dates and files for the new clone
            StartDate = DateTime.Now,
            EndDate = DateTime.Now.AddYears(1),
            SignedAgreementFilePath = null
        };
    }
}
