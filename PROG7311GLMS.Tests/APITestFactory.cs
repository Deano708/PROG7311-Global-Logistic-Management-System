// =============================================================
//  PROG7311GLMS.Tests / ApiTestFactory.cs
// =============================================================

using System.Security.Claims;
using System.Text.Encodings.Web;
using GLMS_API.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PROG7311GLMS.Tests;

/// <summary>
/// Single shared factory for ALL test classes.
/// Making it a singleton (via the static field pattern below) means
/// FirebaseApp.Create() is only called once per test run, which
/// fixes the "default FirebaseApp already exists" error.
/// </summary>
public class ApiTestFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // ── 1. Remove ALL existing DbContext registrations ────────
            // We need to remove every EF-related descriptor to prevent
            // the "two providers registered" conflict.
            // RemoveAll is more thorough than SingleOrDefault + Remove.
            services.RemoveAll<DbContextOptions<GlmsContext>>();
            services.RemoveAll<GlmsContext>();

            // Also remove the DbContextOptions (non-generic) if present
            var efDescriptors = services
                .Where(d => d.ServiceType.FullName?.Contains("EntityFramework") == true
                         || d.ServiceType.FullName?.Contains("DbContext") == true)
                .ToList();
            foreach (var d in efDescriptors)
                services.Remove(d);

            // ── 2. Register a fresh in-memory DbContext ───────────────
            // Each factory instance gets its own uniquely-named database
            // so test classes don't share state.
            var dbName = "GlmsTestDb_" + Guid.NewGuid().ToString("N");

            services.AddDbContext<GlmsContext>(options =>
                options.UseInMemoryDatabase(dbName));

            // ── 3. Replace Firebase JWT auth with a test scheme ───────
            // Remove JwtBearer and any other auth schemes registered
            // by the real Program.cs
            var authDescriptors = services
                .Where(d => d.ServiceType.Namespace != null &&
                           (d.ServiceType.Namespace.Contains("Authentication") ||
                            d.ServiceType.Namespace.Contains("JwtBearer")))
                .ToList();
            foreach (var d in authDescriptors)
                services.Remove(d);

            // Register our fake "always authenticated" scheme
            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    "Test", _ => { });

            // ── 4. Seed the database ──────────────────────────────────
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<GlmsContext>();
            db.Database.EnsureCreated();
            SeedDatabase(db);
        });

        builder.UseEnvironment("Development");
    }

    public static void SeedDatabase(GlmsContext db)
    {
        if (db.Clients.Any()) return;

        db.Clients.Add(new Client
        {
            ClientId = 1,
            Name = "Test Client Ltd",
            ClientEmail = "test@client.com",
            Region = "ZA"
        });

        db.Statuses.AddRange(
            new Status { StatusId = 1, StatusName = "Active", Category = "Contract", Description = "Active" },
            new Status { StatusId = 2, StatusName = "On-Hold", Category = "Contract", Description = "On hold" },
            new Status { StatusId = 3, StatusName = "Expired", Category = "Contract", Description = "Expired" },
            new Status { StatusId = 4, StatusName = "Pending", Category = "ServiceRequest", Description = "Pending" },
            new Status { StatusId = 5, StatusName = "Approved", Category = "ServiceRequest", Description = "Approved" }
        );

        db.SaveChanges();
    }
}

/// <summary>
/// Always-authenticated handler — replaces Firebase JWT for tests.
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
        var claims = new[] { new Claim(ClaimTypes.Name, "testuser@glms.test") };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}