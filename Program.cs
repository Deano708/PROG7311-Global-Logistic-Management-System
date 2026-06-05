// =============================================================
//  GLMS_MVC / Program.cs
//
//  The updated MVC no longer references EF Core or GlmsContext.
//  All data comes from the GLMS Web API via GlmsApiClient.
// =============================================================

using PROG7311GLMS.Service;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// ── 1. MVC ────────────────────────────────────────────────────
builder.Services.AddControllersWithViews();

// ── 2. Session (stores the Firebase token between requests) ───
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ── 3. Cookie Authentication (guards MVC routes) ──────────────
//  The user is "authenticated" in the MVC once they have a valid
//  Firebase session stored.  A simple middleware check (see below)
//  redirects unauthenticated requests to /Auth/Login.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
    });

builder.Services.AddAuthorization();

// ── 4. Typed HttpClient for the GLMS API ─────────────────────
builder.Services.AddHttpClient<GlmsApiClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["GlmsApi:BaseUrl"] ?? "https://localhost:7100/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// ─────────────────────────────────────────────────────────────
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();

// ── Firebase session guard ────────────────────────────────────
// If the user hits any route other than Auth/*, redirect to login
// unless they have a Firebase token in session.
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? "";
    bool isAuthPath = path.StartsWith("/Auth", StringComparison.OrdinalIgnoreCase);
    bool hasToken = context.Session.Keys.Contains("FirebaseToken");

    if (!isAuthPath && !hasToken)
    {
        context.Response.Redirect($"/Auth/Login?returnUrl={Uri.EscapeDataString(path)}");
        return;
    }
    await next();
});

app.UseAuthorization();

app.MapStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Contracts}/{action=Index}/{id?}")
   .WithStaticAssets();

app.Run();