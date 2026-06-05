// =============================================================
//  GLMS_MVC / Controllers / AuthController.cs
//
//  Handles the Firebase sign-in flow:
//    1. User submits Firebase ID token (obtained client-side via
//       the Firebase JS SDK after email/password sign-in).
//    2. MVC posts the token to the API for server-side verification.
//    3. On success, token is stored in session for subsequent calls.
// =============================================================

using PROG7311GLMS.Service;
using Microsoft.AspNetCore.Mvc;


namespace GLMS_MVC.Controllers;

public class AuthController : Controller
{
    private readonly GlmsApiClient _api;

    public AuthController(GlmsApiClient api) => _api = api;

    // GET: /Auth/Login
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    // POST: /Auth/Login
    // Receives the Firebase ID token from the hidden form field
    // populated by the Firebase JS SDK after sign-in.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string idToken, string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(idToken))
        {
            ModelState.AddModelError("", "Sign-in failed. Please try again.");
            return View();
        }

        var result = await _api.VerifyFirebaseTokenAsync(idToken);

        if (result == null)
        {
            ModelState.AddModelError("", "Invalid or expired token. Please sign in again.");
            return View();
        }

        // Store the raw token in session; GlmsApiClient reads it for every API call
        HttpContext.Session.SetString("FirebaseToken", result.Token);
        HttpContext.Session.SetString("UserEmail", result.Email);
        HttpContext.Session.SetString("UserName", result.Name);
        HttpContext.Session.SetString("UserRole", result.Role);

        return LocalRedirect(returnUrl ?? "/Contracts");
    }

    // POST: /Auth/Logout
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Login");
    }
}