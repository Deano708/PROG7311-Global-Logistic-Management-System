
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using GLMS_API.DTOs;
using NUnit.Framework;

namespace PROG7311GLMS.Tests;

/*
Title: Disclosure of AI Usage in my Assessment.
• Section: ContractsAPITests.
• AI Tool: Claude Sonnet 4.6
• Purpose/intention : Design assistance of ContractsAPITests allowing for testing of HTTP calls of POST, GET, PATCH, and DELETE.
• Date(s) 05/06/2026.
• https://claude.ai/share/503d645e-0ce0-4796-920e-6e73ce7ccfb5
*/


[TestFixture]
public class ContractsApiTests
{
    // The factory boots the API in memory
    private ApiTestFactory _factory = null!;

    // The HttpClient talks to the in-memory API
    private HttpClient _client = null!;

    // JSON options matching the API's serializer settings
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // ── Runs once before all tests in this class ───────────────
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new ApiTestFactory();

        // CreateClient() boots the in-memory API and returns
        // an HttpClient wired directly to it — no network needed.
        _client = _factory.CreateClient();
    }

    // ── Runs once after all tests finish ──────────────────────
    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    // ==========================================================
    //  SECTION 1: Basic GET tests
    //  These verify the endpoints exist and respond correctly.
    // ==========================================================

    /// <summary>
    /// GET /api/contracts should return 200 OK.
    /// This is the simplest possible test — does the endpoint exist?
    /// </summary>
    [Test]
    public async Task GetContracts_ReturnsOk()
    {
        // Act: Make the HTTP request
        var response = await _client.GetAsync("/api/contracts");

        // Assert: Check the status code is 200 OK
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
            "GET /api/contracts should return 200 OK");
    }

    /// <summary>
    /// GET /api/contracts should return a JSON array (not null, not HTML).
    /// This verifies the Content-Type header and that the body is valid JSON.
    /// </summary>
    [Test]
    public async Task GetContracts_ReturnsJsonArray()
    {
        // Act
        var response = await _client.GetAsync("/api/contracts");
        var json = await response.Content.ReadAsStringAsync();

        // Assert: Must be JSON content type
        Assert.That(response.Content.Headers.ContentType?.MediaType,
            Is.EqualTo("application/json"),
            "Response should be application/json");

        // Assert: Body should not be null or empty
        Assert.That(json, Is.Not.Null.And.Not.Empty,
            "Response body should not be empty");

        // Assert: Should deserialize to a list (even if empty)
        var contracts = JsonSerializer.Deserialize<List<ContractDto>>(json, JsonOpts);
        Assert.That(contracts, Is.Not.Null,
            "Response should deserialize to a list of ContractDto");
    }

    /// <summary>
    /// GET /api/contracts/{id} for a non-existent ID should return 404.
    /// This verifies the API handles missing resources correctly.
    /// </summary>
    [Test]
    public async Task GetContractById_NonExistentId_Returns404()
    {
        // Act: ID 99999 does not exist in the test database
        var response = await _client.GetAsync("/api/contracts/99999");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound),
            "Non-existent contract ID should return 404");
    }

    // ==========================================================
    //  SECTION 2: POST (Create) tests
    //  These verify we can create new contracts.
    // ==========================================================

    /// <summary>
    /// POST /api/contracts with valid data should return 201 Created.
    /// The response body should contain the newly created contract.
    /// </summary>
    [Test]
    public async Task CreateContract_ValidData_Returns201()
    {
        // Arrange: Build the multipart form (same format the MVC sends)
        using var form = BuildContractForm(
            clientId: 1,
            serviceLevel: "Standard Shipping",
            startDate: DateTime.Today,
            endDate: DateTime.Today.AddYears(1));

        // Act
        var response = await _client.PostAsync("/api/contracts", form);
        var json = await response.Content.ReadAsStringAsync();

        // Assert: 201 Created
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created),
            $"Expected 201 Created but got {response.StatusCode}. Body: {json}");

        // Assert: Response body contains the contract
        var created = JsonSerializer.Deserialize<ContractDto>(json, JsonOpts);
        Assert.That(created, Is.Not.Null, "Response should contain the created contract");
        Assert.That(created!.ContractId, Is.GreaterThan(0),
            "Created contract should have a valid ID");
    }

    /// <summary>
    /// POST /api/contracts with invalid ClientId (0) should return 400.
    /// This tests that the API validates input correctly.
    /// </summary>
    [Test]
    public async Task CreateContract_MissingClientId_Returns400()
    {
        // Arrange: ClientId = 0 is invalid
        using var form = BuildContractForm(
            clientId: 0,
            serviceLevel: "Express",
            startDate: DateTime.Today,
            endDate: DateTime.Today.AddYears(1));

        // Act
        var response = await _client.PostAsync("/api/contracts", form);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest),
            "Missing ClientId should return 400 Bad Request");
    }

    /// <summary>
    /// POST /api/contracts where EndDate is before StartDate should return 400.
    /// </summary>
    [Test]
    public async Task CreateContract_EndDateBeforeStartDate_Returns400()
    {
        // Arrange: End date is in the past relative to start
        using var form = BuildContractForm(
            clientId: 1,
            serviceLevel: "Express",
            startDate: DateTime.Today.AddYears(1),  // Start: 1 year from now
            endDate: DateTime.Today);              // End: today (before start)

        // Act
        var response = await _client.PostAsync("/api/contracts", form);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest),
            "End date before start date should return 400 Bad Request");
    }

    // ==========================================================
    //  SECTION 3: Data Integrity Tests (Create → Read)
    //
    //  This is the most important section. We:
    //   1. CREATE a contract via POST
    //   2. READ it back via GET using the ID returned in step 1
    //   3. VERIFY the data we read matches what we sent
    //
    //  This proves the full stack works: Controller → Service →
    //  Database → back out again with correct data.
    // ==========================================================

    /// <summary>
    /// Create a contract then read it back and verify data integrity.
    /// POST /api/contracts → GET /api/contracts/{id}
    /// </summary>
    [Test]
    public async Task CreateThenRead_DataIntegrity_Passes()
    {
        // ── Step 1: CREATE ─────────────────────────────────────
        var expectedServiceLevel = "Priority Shipping - Test " + Guid.NewGuid();
        var expectedStartDate = DateTime.Today;
        var expectedEndDate = DateTime.Today.AddYears(2);

        using var form = BuildContractForm(
            clientId: 1,
            serviceLevel: expectedServiceLevel,
            startDate: expectedStartDate,
            endDate: expectedEndDate);

        var createResponse = await _client.PostAsync("/api/contracts", form);
        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created),
            "Step 1 (Create) failed – cannot proceed with data integrity check");

        var createJson = await createResponse.Content.ReadAsStringAsync();
        var created = JsonSerializer.Deserialize<ContractDto>(createJson, JsonOpts);

        Assert.That(created, Is.Not.Null, "Created contract should not be null");
        Assert.That(created!.ContractId, Is.GreaterThan(0), "Contract should have a valid ID");

        // ── Step 2: READ back the same contract ───────────────
        var getResponse = await _client.GetAsync($"/api/contracts/{created.ContractId}");
        Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
            $"Step 2 (Read) failed – GET /api/contracts/{created.ContractId} returned " +
            getResponse.StatusCode);

        var getJson = await getResponse.Content.ReadAsStringAsync();
        var fetched = JsonSerializer.Deserialize<ContractDto>(getJson, JsonOpts);

        // ── Step 3: VERIFY data matches what was sent ─────────
        Assert.That(fetched, Is.Not.Null, "Fetched contract should not be null");
        Assert.That(fetched!.ContractId, Is.EqualTo(created.ContractId), "Contract ID should match");
        Assert.That(fetched.ServiceLevel, Is.EqualTo(expectedServiceLevel), "Service Level should match what was sent");
        Assert.That(fetched.StartDate.Date, Is.EqualTo(expectedStartDate.Date), "Start Date should match");
        Assert.That(fetched.EndDate.Date, Is.EqualTo(expectedEndDate.Date), "End Date should match");
        Assert.That(fetched.ClientName, Is.Not.Null.And.Not.Empty, "Client name should be populated");
        Assert.That(fetched.StatusName, Is.Not.Null.And.Not.Empty, "Status name should be populated");
    }

    /// <summary>
    /// Verifies that a contract starting today gets status "Active".
    /// The API auto-computes status from dates — we verify the logic is correct.
    /// </summary>
    [Test]
    public async Task CreateContract_StartingToday_HasActiveStatus()
    {
        // Arrange: Start today = Active
        using var form = BuildContractForm(
            clientId: 1,
            serviceLevel: "Active Status Test",
            startDate: DateTime.Today,
            endDate: DateTime.Today.AddYears(1));

        // Act
        var response = await _client.PostAsync("/api/contracts", form);
        var json = await response.Content.ReadAsStringAsync();
        var created = JsonSerializer.Deserialize<ContractDto>(json, JsonOpts);

        // Assert
        Assert.That(created?.StatusName, Is.EqualTo("Active"),
            "A contract starting today and ending in the future should be Active");
    }

    /// <summary>
    /// Verifies that a contract starting in the future gets status "On-Hold".
    /// </summary>
    [Test]
    public async Task CreateContract_FutureStart_HasOnHoldStatus()
    {
        // Arrange: Start in 6 months = On-Hold
        using var form = BuildContractForm(
            clientId: 1,
            serviceLevel: "On-Hold Status Test",
            startDate: DateTime.Today.AddMonths(6),
            endDate: DateTime.Today.AddYears(2));

        // Act
        var response = await _client.PostAsync("/api/contracts", form);
        var json = await response.Content.ReadAsStringAsync();
        var created = JsonSerializer.Deserialize<ContractDto>(json, JsonOpts);

        // Assert
        Assert.That(created?.StatusName, Is.EqualTo("On-Hold"),
            "A contract with a future start date should be On-Hold");
    }

    // ==========================================================
    //  SECTION 4: PATCH status tests
    // ==========================================================

    /// <summary>
    /// PATCH /api/contracts/{id}/status should update the contract status.
    /// Create → PATCH → GET and verify the status changed.
    /// </summary>
    [Test]
    public async Task PatchContractStatus_ValidStatus_Returns200()
    {
        // ── Step 1: Create a contract ──────────────────────────
        using var form = BuildContractForm(
            clientId: 1, serviceLevel: "Patch Test",
            startDate: DateTime.Today, endDate: DateTime.Today.AddYears(1));

        var createResponse = await _client.PostAsync("/api/contracts", form);
        var created = JsonSerializer.Deserialize<ContractDto>(
            await createResponse.Content.ReadAsStringAsync(), JsonOpts);

        Assert.That(created, Is.Not.Null, "Setup failed – could not create contract");

        // ── Step 2: PATCH the status ───────────────────────────
        var patchBody = JsonSerializer.Serialize(new { statusName = "On-Hold" });
        var patchContent = new StringContent(patchBody, Encoding.UTF8, "application/json");
        var patchResponse = await _client.PatchAsync(
            $"/api/contracts/{created!.ContractId}/status", patchContent);

        Assert.That(patchResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
            "PATCH status should return 200 OK");

        // ── Step 3: Verify status changed ─────────────────────
        var getResponse = await _client.GetAsync($"/api/contracts/{created.ContractId}");
        var fetched = JsonSerializer.Deserialize<ContractDto>(
            await getResponse.Content.ReadAsStringAsync(), JsonOpts);

        Assert.That(fetched?.StatusName, Is.EqualTo("On-Hold"),
            "Status should have been updated to On-Hold");
    }

    /// <summary>
    /// PATCH with an invalid status name should return 404
    /// (no matching status in the database).
    /// </summary>
    [Test]
    public async Task PatchContractStatus_InvalidStatus_Returns404()
    {
        // Create a contract first
        using var form = BuildContractForm(
            clientId: 1, serviceLevel: "Patch Invalid Test",
            startDate: DateTime.Today, endDate: DateTime.Today.AddYears(1));
        var createResponse = await _client.PostAsync("/api/contracts", form);
        var created = JsonSerializer.Deserialize<ContractDto>(
            await createResponse.Content.ReadAsStringAsync(), JsonOpts);

        // PATCH with a status name that doesn't exist
        var patchBody = JsonSerializer.Serialize(new { statusName = "NonExistentStatus" });
        var patchContent = new StringContent(patchBody, Encoding.UTF8, "application/json");
        var patchResponse = await _client.PatchAsync(
            $"/api/contracts/{created!.ContractId}/status", patchContent);

        Assert.That(patchResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound),
            "Invalid status name should return 404");
    }

    // ==========================================================
    //  SECTION 5: DELETE tests
    // ==========================================================

    /// <summary>
    /// Create a contract then delete it. Verify it's gone (404 on GET).
    /// </summary>
    [Test]
    public async Task DeleteContract_ExistingContract_Returns204ThenNotFound()
    {
        // ── Step 1: Create ─────────────────────────────────────
        using var form = BuildContractForm(
            clientId: 1, serviceLevel: "Delete Test",
            startDate: DateTime.Today, endDate: DateTime.Today.AddYears(1));
        var createResponse = await _client.PostAsync("/api/contracts", form);
        var created = JsonSerializer.Deserialize<ContractDto>(
            await createResponse.Content.ReadAsStringAsync(), JsonOpts);

        Assert.That(created, Is.Not.Null, "Setup failed");

        // ── Step 2: DELETE ─────────────────────────────────────
        var deleteResponse = await _client.DeleteAsync(
            $"/api/contracts/{created!.ContractId}");
        Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent),
            "DELETE should return 204 No Content");

        // ── Step 3: Verify it's gone ───────────────────────────
        var getResponse = await _client.GetAsync($"/api/contracts/{created.ContractId}");
        Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound),
            "Deleted contract should return 404 on subsequent GET");
    }

    // ==========================================================
    //  SECTION 6: Lookup endpoints
    // ==========================================================

    [Test]
    public async Task GetClients_ReturnsOkWithData()
    {
        var response = await _client.GetAsync("/api/lookups/clients");
        var json = await response.Content.ReadAsStringAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var clients = JsonSerializer.Deserialize<List<ClientDto>>(json, JsonOpts);
        Assert.That(clients, Is.Not.Null.And.Not.Empty,
            "Clients list should contain the seeded test client");
    }

    [Test]
    public async Task GetStatuses_ContractCategory_ReturnsOkWithData()
    {
        var response = await _client.GetAsync("/api/lookups/statuses?category=Contract");
        var json = await response.Content.ReadAsStringAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var statuses = JsonSerializer.Deserialize<List<StatusDto>>(json, JsonOpts);
        Assert.That(statuses, Is.Not.Null.And.Not.Empty,
            "Should return Contract statuses");
        Assert.That(statuses!.All(s => s.Category == "Contract"), Is.True,
            "All returned statuses should be in the Contract category");
    }

    // ==========================================================
    //  HELPER: Builds the multipart form data for contract POST
    //  (mirrors what the MVC ContractsController sends)
    // ==========================================================
    private static MultipartFormDataContent BuildContractForm(
        int clientId, string serviceLevel, DateTime startDate, DateTime endDate)
    {
        var form = new MultipartFormDataContent();
        form.Add(new StringContent(clientId.ToString()), "ClientId");
        form.Add(new StringContent(serviceLevel), "ServiceLevel");
        form.Add(new StringContent(startDate.ToString("yyyy-MM-dd")), "StartDate");
        form.Add(new StringContent(endDate.ToString("yyyy-MM-dd")), "EndDate");
        return form;
    }
}