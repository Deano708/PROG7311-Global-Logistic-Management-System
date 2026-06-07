using System.Net;
using System.Text;
using System.Text.Json;
using GLMS_API.DTOs;
using GLMS_API.Models;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace PROG7311GLMS.Tests;

[TestFixture]
public class ServiceRequestApiTests
{
    private ApiTestFactory _factory = null!;
    private HttpClient _client = null!;

    private static readonly JsonSerializerOptions JsonOpts = new()
    { PropertyNameCaseInsensitive = true };

    // ID of an Active contract created in SetUp — shared across tests
    private int _activeContractId;
    private int _expiredContractId;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _factory = new ApiTestFactory();
        _client = _factory.CreateClient();

        // Seed an Active and an Expired contract for our tests to use
        _activeContractId = await CreateContract(DateTime.Today, DateTime.Today.AddYears(1));
        _expiredContractId = await CreateContract(
            DateTime.Today.AddYears(-2), DateTime.Today.AddYears(-1));

        // Force the expired contract's status to "Expired" via PATCH
        // (the API auto-sets based on dates, so this should already be Expired,
        //  but we patch explicitly to be certain)
        await _client.PatchAsync(
            $"/api/contracts/{_expiredContractId}/status",
            new StringContent(
                JsonSerializer.Serialize(new { statusName = "Expired" }),
                Encoding.UTF8, "application/json"));
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    // ==========================================================
    //  GET service requests
    // ==========================================================

    [Test]
    public async Task GetServiceRequests_ForValidContract_ReturnsOk()
    {
        var response = await _client.GetAsync(
            $"/api/servicerequests?contractId={_activeContractId}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
            "GET service requests for a valid contract should return 200 OK");
    }

    [Test]
    public async Task GetServiceRequests_ReturnsJsonArray()
    {
        var response = await _client.GetAsync(
            $"/api/servicerequests?contractId={_activeContractId}");
        var json = await response.Content.ReadAsStringAsync();

        var requests = JsonSerializer.Deserialize<List<ServiceRequestDto>>(json, JsonOpts);
        Assert.That(requests, Is.Not.Null,
            "Response should deserialize to a list");
    }

    // ==========================================================
    //  POST service requests
    // ==========================================================

    /// <summary>
    /// Business rule: Service requests CAN be created on Active contracts.
    /// Create → assert 201.
    /// </summary>
    [Test]
    public async Task CreateServiceRequest_OnActiveContract_Returns201()
    {
        var body = JsonSerializer.Serialize(new CreateServiceRequestDto
        {
            ContractId = _activeContractId,
            Description = "Urgent freight delivery",
            CostUsd = 250.00m
        });
        var content = new StringContent(body, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/api/servicerequests", content);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created),
            "Service request on an Active contract should return 201 Created");
    }

    /// <summary>
    /// Business rule: Service requests CANNOT be created on Expired contracts.
    /// The API should return 422 Unprocessable Entity.
    /// </summary>
    [Test]
    public async Task CreateServiceRequest_OnExpiredContract_Returns422()
    {
        var body = JsonSerializer.Serialize(new CreateServiceRequestDto
        {
            ContractId = _expiredContractId,
            Description = "This should be rejected",
            CostUsd = 100.00m
        });
        var content = new StringContent(body, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/api/servicerequests", content);

        Assert.That(response.StatusCode,
            Is.EqualTo(HttpStatusCode.UnprocessableEntity),
            "Service request on an Expired contract should be rejected with 422");
    }

    /// <summary>
    /// Missing description should return 400.
    /// </summary>
    [Test]
    public async Task CreateServiceRequest_MissingDescription_Returns400()
    {
        var body = JsonSerializer.Serialize(new CreateServiceRequestDto
        {
            ContractId = _activeContractId,
            Description = "",          // ← empty
            CostUsd = 100.00m
        });
        var content = new StringContent(body, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/api/servicerequests", content);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest),
            "Empty description should return 400 Bad Request");
    }

    /// <summary>
    /// Zero or negative cost should return 400.
    /// </summary>
    [Test]
    public async Task CreateServiceRequest_ZeroCost_Returns400()
    {
        var body = JsonSerializer.Serialize(new CreateServiceRequestDto
        {
            ContractId = _activeContractId,
            Description = "Valid description",
            CostUsd = 0m              // ← invalid
        });
        var content = new StringContent(body, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/api/servicerequests", content);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest),
            "Zero cost should return 400 Bad Request");
    }

    // ==========================================================
    //  Data Integrity: Create → Read → verify USD and ZAR
    // ==========================================================

    /// <summary>
    /// Create a service request then read it back.
    /// Verify Description, CostUsd are preserved and CostZar was computed.
    /// </summary>
    [Test]
    public async Task CreateThenReadServiceRequest_DataIntegrity_Passes()
    {
        var expectedDescription = "Integrity Test " + Guid.NewGuid();
        var expectedUsd = 500.00m;

        // ── Step 1: CREATE ─────────────────────────────────────
        var body = JsonSerializer.Serialize(new CreateServiceRequestDto
        {
            ContractId = _activeContractId,
            Description = expectedDescription,
            CostUsd = expectedUsd
        });
        var createResponse = await _client.PostAsync(
            "/api/servicerequests",
            new StringContent(body, Encoding.UTF8, "application/json"));

        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created),
            "Step 1 (Create) failed");

        var created = JsonSerializer.Deserialize<ServiceRequestDto>(
            await createResponse.Content.ReadAsStringAsync(), JsonOpts);

        Assert.That(created, Is.Not.Null);
        Assert.That(created!.ServiceRequestId, Is.GreaterThan(0));

        // ── Step 2: READ back via GET ──────────────────────────
        var getResponse = await _client.GetAsync(
            $"/api/servicerequests?contractId={_activeContractId}");

        var requests = JsonSerializer.Deserialize<List<ServiceRequestDto>>(
            await getResponse.Content.ReadAsStringAsync(), JsonOpts);

        var fetched = requests?.FirstOrDefault(
            r => r.ServiceRequestId == created.ServiceRequestId);

        // ── Step 3: VERIFY data integrity ─────────────────────
        Assert.That(fetched, Is.Not.Null, "Request not found in GET response");
        Assert.That(fetched!.Description, Is.EqualTo(expectedDescription), "Description should match");
        Assert.That(fetched.CostUsd, Is.EqualTo(expectedUsd), "USD cost should match");
        Assert.That(fetched.CostZar, Is.GreaterThan(0), "ZAR cost should be converted (> 0)");
        Assert.That(fetched.StatusName, Is.EqualTo("Pending"), "New requests should be Pending");
    }

    // ==========================================================
    //  DELETE service request
    // ==========================================================

    [Test]
    public async Task DeleteServiceRequest_ExistingRequest_Returns204()
    {
        // Create one to delete
        var body = JsonSerializer.Serialize(new CreateServiceRequestDto
        {
            ContractId = _activeContractId,
            Description = "Delete me",
            CostUsd = 50.00m
        });
        var createResponse = await _client.PostAsync(
            "/api/servicerequests",
            new StringContent(body, Encoding.UTF8, "application/json"));
        var created = JsonSerializer.Deserialize<ServiceRequestDto>(
            await createResponse.Content.ReadAsStringAsync(), JsonOpts);

        // Delete it
        var deleteResponse = await _client.DeleteAsync(
            $"/api/servicerequests/{created!.ServiceRequestId}");

        Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent),
            "DELETE should return 204 No Content");
    }

    // ==========================================================
    //  USD → ZAR conversion preview endpoint
    // ==========================================================

    [Test]
    public async Task ConvertCurrency_ValidAmount_ReturnsOkWithZar()
    {
        var response = await _client.GetAsync("/api/servicerequests/convert?usd=100");
        var json = await response.Content.ReadAsStringAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
            "Currency convert endpoint should return 200 OK");

        using var doc = JsonDocument.Parse(json);
        var zar = doc.RootElement.GetProperty("zar").GetDecimal();

        Assert.That(zar, Is.GreaterThan(0),
            "ZAR conversion result should be greater than 0");
    }

    [Test]
    public async Task ConvertCurrency_ZeroAmount_Returns400()
    {
        var response = await _client.GetAsync("/api/servicerequests/convert?usd=0");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest),
            "Zero USD amount should return 400");
    }

    // ── Helper ────────────────────────────────────────────────

    private async Task<int> CreateContract(DateTime start, DateTime end)
    {
        var form = new MultipartFormDataContent();
        form.Add(new StringContent("1"), "ClientId");
        form.Add(new StringContent("Test SLA"), "ServiceLevel");
        form.Add(new StringContent(start.ToString("yyyy-MM-dd")), "StartDate");
        form.Add(new StringContent(end.ToString("yyyy-MM-dd")), "EndDate");

        var response = await _client.PostAsync("/api/contracts", form);
        var json = await response.Content.ReadAsStringAsync();
        var dto = JsonSerializer.Deserialize<ContractDto>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return dto?.ContractId ?? throw new Exception($"Failed to create contract: {json}");
    }
}