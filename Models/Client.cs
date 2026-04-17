using System;
using System.Collections.Generic;

namespace PROG7311GLMS.Models;

public partial class Client
{
    public int ClientId { get; set; }

    public string Name { get; set; } = null!;

    public string? ClientEmail { get; set; }

    public string? Region { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Contract> Contracts { get; set; } = new List<Contract>();
}
