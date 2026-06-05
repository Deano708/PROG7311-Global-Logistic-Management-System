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
public interface IStatusObserver
{
    void Update(string message);
}

public class ComplianceNotificationService : IStatusObserver
{
    public void Update(string message)
    {
        // Logic to log or email the Compliance Officer
        Console.WriteLine($"[Compliance Alert]: {message}");
    }
}

public interface IStatusSubject
{
    void Attach(IStatusObserver observer);
    void Notify(string message);
}
