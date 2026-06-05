// =============================================================
//  GLMS_MVC / Services / GlmsApiClient.cs
//
//  Typed HttpClient that the MVC controllers use instead of
//  talking to the database directly.  All requests include the
//  Firebase Bearer token from the current user's session.
// =============================================================

using GLMS_API.DTOs;
using PROG7311GLMS.Models;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace PROG7311GLMS.Service;

public class GlmsApiClient
{
    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _ctx;

    // JSON options shared across all calls
    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public GlmsApiClient(HttpClient http, IHttpContextAccessor ctx)
    {
        _http = http;
        _ctx = ctx;
    }

    // ── Auth helper ───────────────────────────────────────────

    private void AttachToken()
    {
        var token = _ctx.HttpContext?.Session.GetString("FirebaseToken");
        if (!string.IsNullOrEmpty(token))
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
    }

    // ── Contracts ─────────────────────────────────────────────

    public async Task<List<ContractDto>> GetContractsAsync(
        DateTime? startDate = null, DateTime? endDate = null, int? statusId = null)
    {
        AttachToken();
        var query = BuildQuery(
            ("startDate", startDate?.ToString("yyyy-MM-dd")),
            ("endDate", endDate?.ToString("yyyy-MM-dd")),
            ("statusId", statusId?.ToString()));

        var response = await _http.GetAsync($"api/contracts{query}");
        response.EnsureSuccessStatusCode();
        return await Deserialize<List<ContractDto>>(response) ?? new();
    }

    public async Task<ContractDto?> GetContractAsync(int id)
    {
        AttachToken();
        var response = await _http.GetAsync($"api/contracts/{id}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await Deserialize<ContractDto>(response);
    }

    public async Task<ContractDto?> CreateContractAsync(
        CreateContractDto dto, Stream? pdfStream = null, string? pdfFileName = null)
    {
        AttachToken();
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(dto.ClientId.ToString()), "ClientId");
        form.Add(new StringContent(dto.ServiceLevel ?? ""), "ServiceLevel");
        form.Add(new StringContent(dto.StartDate.ToString("yyyy-MM-dd")), "StartDate");
        form.Add(new StringContent(dto.EndDate.ToString("yyyy-MM-dd")), "EndDate");

        if (pdfStream != null && pdfFileName != null)
        {
            var sc = new StreamContent(pdfStream);
            sc.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            form.Add(sc, "pdfFile", pdfFileName);
        }

        var response = await _http.PostAsync("api/contracts", form);
        response.EnsureSuccessStatusCode();
        return await Deserialize<ContractDto>(response);
    }

    public async Task<ContractDto?> PatchContractStatusAsync(int id, string statusName)
    {
        AttachToken();
        var body = JsonSerializer.Serialize(new { statusName });
        var content = new StringContent(body, Encoding.UTF8, "application/json");
        var response = await _http.PatchAsync($"api/contracts/{id}/status", content);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await Deserialize<ContractDto>(response);
    }

    public async Task<ContractDto?> DuplicateContractAsync(int id)
    {
        AttachToken();
        var response = await _http.PostAsync($"api/contracts/{id}/duplicate", null);
        response.EnsureSuccessStatusCode();
        return await Deserialize<ContractDto>(response);
    }

    public async Task<bool> DeleteContractAsync(int id)
    {
        AttachToken();
        var response = await _http.DeleteAsync($"api/contracts/{id}");
        return response.IsSuccessStatusCode;
    }

    public async Task<byte[]?> DownloadAgreementAsync(int id)
    {
        AttachToken();
        var response = await _http.GetAsync($"api/contracts/{id}/agreement");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadAsByteArrayAsync();
    }

    // ── Service Requests ──────────────────────────────────────

    public async Task<List<ServiceRequestDto>> GetServiceRequestsAsync(int contractId)
    {
        AttachToken();
        var response = await _http.GetAsync($"api/servicerequests?contractId={contractId}");
        response.EnsureSuccessStatusCode();
        return await Deserialize<List<ServiceRequestDto>>(response) ?? new();
    }

    public async Task<(bool success, ServiceRequestDto? dto)> CreateServiceRequestAsync(
        CreateServiceRequestDto dto)
    {
        AttachToken();
        var body = JsonSerializer.Serialize(dto);
        var content = new StringContent(body, Encoding.UTF8, "application/json");
        var response = await _http.PostAsync("api/servicerequests", content);

        if (response.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity)
            return (false, null);

        response.EnsureSuccessStatusCode();
        return (true, await Deserialize<ServiceRequestDto>(response));
    }

    public async Task<bool> DeleteServiceRequestAsync(int id)
    {
        AttachToken();
        var response = await _http.DeleteAsync($"api/servicerequests/{id}");
        return response.IsSuccessStatusCode;
    }

    // ── Lookups ───────────────────────────────────────────────

    public async Task<List<ClientDto>> GetClientsAsync()
    {
        AttachToken();
        var response = await _http.GetAsync("api/lookups/clients");
        response.EnsureSuccessStatusCode();
        return await Deserialize<List<ClientDto>>(response) ?? new();
    }

    public async Task<List<StatusDto>> GetStatusesAsync(string category)
    {
        AttachToken();
        var response = await _http.GetAsync($"api/lookups/statuses?category={category}");
        response.EnsureSuccessStatusCode();
        return await Deserialize<List<StatusDto>>(response) ?? new();
    }

    // ── Auth ──────────────────────────────────────────────────

    public async Task<FirebaseVerifyResult?> VerifyFirebaseTokenAsync(string idToken)
    {
        var body = JsonSerializer.Serialize(new { idToken });
        var content = new StringContent(body, Encoding.UTF8, "application/json");
        var response = await _http.PostAsync("api/auth/verify", content);
        if (!response.IsSuccessStatusCode) return null;
        return await Deserialize<FirebaseVerifyResult>(response);
    }

    // ── Helpers ───────────────────────────────────────────────

    private static async Task<T?> Deserialize<T>(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, _json);
    }

    private static string BuildQuery(params (string key, string? value)[] pairs)
    {
        var parts = pairs
            .Where(p => p.value != null)
            .Select(p => $"{p.key}={Uri.EscapeDataString(p.value!)}");
        var qs = string.Join("&", parts);
        return qs.Length > 0 ? "?" + qs : "";
    }
}