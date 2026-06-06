// =============================================================
//  GLMS_MVC / Controllers / AuthController.cs
// =============================================================

using Microsoft.AspNetCore.Mvc;
using PROG7311GLMS.Models;
using PROG7311GLMS.Service;

namespace GLMS_MVC.Controllers;

// NO [Authorize] - guarded by session middleware in Program.cs
public class AuthController : Controller
{
    private readonly GlmsApiClient _api;
    private readonly ILogger<AuthController> _logger;

    public AuthController(GlmsApiClient api, ILogger<AuthController> logger)
    {
        _api = api;
        _logger = logger;
    }

    // GET: /Auth/Login
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        // If already has a valid session, go to Contracts directly
        var existing = HttpContext.Session.GetString("FirebaseToken");
        if (!string.IsNullOrEmpty(existing))
            return RedirectToAction("Index", "Contracts");

        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    // POST: /Auth/Login
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string idToken, string? returnUrl = null)
    {
        _logger.LogInformation("Login POST – idToken length: {Len}", idToken?.Length ?? 0);

        if (string.IsNullOrWhiteSpace(idToken))
        {
            ModelState.AddModelError("", "Sign-in failed – no token received.");
            return View();
        }

        FirebaseVerifyResult? result;
        try
        {
            result = await _api.VerifyFirebaseTokenAsync(idToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VerifyFirebaseTokenAsync threw an exception");
            ModelState.AddModelError("", "Could not reach the authentication service.");
            return View();
        }

        if (result == null)
        {
            _logger.LogWarning("VerifyFirebaseTokenAsync returned null");
            ModelState.AddModelError("", "Invalid or expired token. Please sign in again.");
            return View();
        }

        _logger.LogInformation("Token verified for {Email}", result.Email);

        HttpContext.Session.SetString("FirebaseToken", result.Token ?? idToken);
        HttpContext.Session.SetString("UserEmail", result.Email ?? "");
        HttpContext.Session.SetString("UserName", result.Name ?? "");
        HttpContext.Session.SetString("UserRole", result.Role ?? "User");

        // Only use returnUrl if it is a real sub-path, never "/" or empty
        bool safeUrl = !string.IsNullOrEmpty(returnUrl)
                       && returnUrl != "/"
                       && returnUrl.StartsWith("/")
                       && !returnUrl.StartsWith("//")
                       && !returnUrl.StartsWith("/Auth", StringComparison.OrdinalIgnoreCase);

        var destination = safeUrl ? returnUrl! : "/Contracts";
        _logger.LogInformation("Login success – redirecting to {Dest}", destination);
        return LocalRedirect(destination);
    }

    // POST: /Auth/Logout
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Login");
    }

    // GET: /Auth/SessionCheck (debug – remove before submission)
    [HttpGet]
    public IActionResult SessionCheck()
    {
        var token = HttpContext.Session.GetString("FirebaseToken");
        var email = HttpContext.Session.GetString("UserEmail");
        return Content(
            $"Token present: {!string.IsNullOrEmpty(token)}\n" +
            $"Email: {email ?? "(empty)"}\n" +
            $"Session ID: {HttpContext.Session.Id}",
            "text/plain");
    }
}