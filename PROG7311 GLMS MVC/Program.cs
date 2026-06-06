// =============================================================
//  GLMS_MVC / Program.cs
// =============================================================

using PROG7311GLMS.Service;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.Name = ".GLMS.Session";
});

// NOTE: No cookie authentication registered - we use our own
// session-based guard middleware instead. This prevents ASP.NET's
// built-in auth from issuing its own redirects to /Auth/Login
// which conflict with our guard and cause redirect loops.

builder.Services.AddHttpClient<GlmsApiClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["GlmsApi:BaseUrl"] ?? "https://localhost:7008/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback =
        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
});

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

// ── Session guard ─────────────────────────────────────────────
var guardLogger = app.Services.GetRequiredService<ILoggerFactory>()
    .CreateLogger("SessionGuard");

app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? "";

    bool isPublicPath =
        path.StartsWith("/Auth", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/css", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/js", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/lib", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/favicon", StringComparison.OrdinalIgnoreCase);

    var token = context.Session.GetString("FirebaseToken");
    bool hasToken = !string.IsNullOrEmpty(token);

    guardLogger.LogInformation(
        "Guard: {Method} {Path} | public={Public} | hasToken={HasToken}",
        context.Request.Method, path, isPublicPath, hasToken);

    if (!isPublicPath && !hasToken)
    {
        guardLogger.LogInformation("Guard: no token – redirecting to /Auth/Login");
        context.Response.Redirect("/Auth/Login");
        return;
    }

    await next();
});

// No UseAuthentication / UseAuthorization - controllers must NOT
// have [Authorize] attributes as we handle auth via the guard above.

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Contracts}/{action=Index}/{id?}");

app.Run();