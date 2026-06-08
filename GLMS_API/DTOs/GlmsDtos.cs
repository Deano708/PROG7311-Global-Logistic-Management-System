// =============================================================
//  GLMS_API / DTOs / GlmsDtos.cs
//  All Data Transfer Objects used by the API controllers.
//  Keeps EF navigation properties out of the HTTP layer.
// =============================================================


/*
   Title: Create Data Transfer Objects (DTOs)
   Author:  Microsoft Learn
   Date: 05/01/2022
   Date accessed: 03/06/2026
   Availability: https://learn.microsoft.com/en-us/aspnet/web-api/overview/data/using-web-api-with-entity-framework/part-5
*/

namespace GLMS_API.DTOs;

// ── Contracts ─────────────────────────────────────────────────

public class ContractDto
{
    public int ContractId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public int ClientId { get; set; }
    public string ServiceLevel { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public int StatusId { get; set; }
    public string? SignedAgreementPath { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public class CreateContractDto
{
    public int ClientId { get; set; }
    public string ServiceLevel { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    // PDF is uploaded separately via multipart; path stored here after upload
}

public class ContractFilterDto
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? StatusId { get; set; }
}

public class PatchContractStatusDto
{
    /// <summary>"Approved" or "Declined" (maps to existing Status names)</summary>
    public string StatusName { get; set; } = string.Empty;
}

// ── Service Requests ──────────────────────────────────────────

public class ServiceRequestDto
{
    public int ServiceRequestId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal CostUsd { get; set; }
    public decimal CostZar { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public int StatusId { get; set; }
    public int ContractId { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public class CreateServiceRequestDto
{
    public int ContractId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal CostUsd { get; set; }
}

// ── Clients ───────────────────────────────────────────────────

public class ClientDto
{
    public int ClientId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ClientEmail { get; set; }
    public string? Region { get; set; }
}

// ── Statuses ──────────────────────────────────────────────────

public class StatusDto
{
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? Description { get; set; }
}

// ── Auth ──────────────────────────────────────────────────────

public class FirebaseTokenDto
{
    /// <summary>Firebase ID token obtained on the client after sign-in.</summary>
    public string IdToken { get; set; } = string.Empty;
}
