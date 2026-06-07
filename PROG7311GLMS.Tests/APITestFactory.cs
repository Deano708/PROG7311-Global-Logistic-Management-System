using System.Security.Claims;
using System.Text.Encodings.Web;
using GLMS_API.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PROG7311GLMS.Tests;

/// <summary>
/// Boots the GLMS_API in memory with:
///  - SQLite in-memory database (isolated per test run)
///  - Fake authentication that always succeeds
/// </summary>
public class ApiTestFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // ── 1. Remove the real SQL Server DbContext ────────────────
            // Find and remove the existing GlmsContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<GlmsContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            // ── 2. Add an in-memory database instead ──────────────────
            // Each test run gets a fresh, isolated database.
            // "GlmsTestDb" is just a name — it doesn't create a real file.
            services.AddDbContext<GlmsContext>(options =>
                options.UseInMemoryDatabase("GlmsTestDb_" + Guid.NewGuid()));

            // ── 3. Remove the real Firebase JWT authentication ─────────
            var authDescriptors = services
                .Where(d => d.ServiceType.Namespace?.Contains("Authentication") == true)
                .ToList();
            foreach (var d in authDescriptors)
                services.Remove(d);

            // ── 4. Add a fake auth scheme that always says "logged in" ─
            // In real life a test would get a Firebase token, but that
            // requires network access and a real user account.
            // For automated tests we use this bypass instead.
            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    "Test", _ => { });

            // ── 5. Seed the database with test data ───────────────────
            // We need at least one Client and some Statuses to exist
            // before we can create Contracts.
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<GlmsContext>();
            db.Database.EnsureCreated();
            SeedDatabase(db);
        });

        // Tell the factory this is a test environment
        builder.UseEnvironment("Development");
    }

    /// <summary>
    /// Adds the minimum data every test needs to run.
    /// Think of this as setting up your LEGO base plate before
    /// building anything on top of it.
    /// </summary>
    public static void SeedDatabase(GlmsContext db)
    {
        // Only seed if empty (prevents duplicate key errors on re-use)
        if (db.Clients.Any()) return;

        // Add test client
        db.Clients.Add(new Client
        {
            ClientId = 1,
            Name = "Test Client Ltd",
            ClientEmail = "test@client.com",
            Region = "ZA"
        });

        // Add contract statuses (the API logic depends on these names)
        db.Statuses.AddRange(
            new Status { StatusId = 1, StatusName = "Active", Category = "Contract", Description = "Active contract" },
            new Status { StatusId = 2, StatusName = "On-Hold", Category = "Contract", Description = "On hold" },
            new Status { StatusId = 3, StatusName = "Expired", Category = "Contract", Description = "Expired" },
            new Status { StatusId = 4, StatusName = "Pending", Category = "ServiceRequest", Description = "Pending request" },
            new Status { StatusId = 5, StatusName = "Approved", Category = "ServiceRequest", Description = "Approved" }
        );

        db.SaveChanges();
    }
}

/// <summary>
/// Fake authentication handler used only during testing.
///
/// Normally the API checks your Firebase token to know who you are.
/// During tests we can't get a real Firebase token, so this handler
/// pretends every request comes from a valid logged-in test user.
/// </summary>
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Create a fake identity — like a test ID badge
        var claims = new[]
        {
            new Claim(ClaimTypes.Name,  "testuser@glms.test"),
            new Claim(ClaimTypes.Email, "testuser@glms.test"),
            new Claim("uid",            "test-uid-001")
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        // Always succeed — every test request is "authenticated"
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}