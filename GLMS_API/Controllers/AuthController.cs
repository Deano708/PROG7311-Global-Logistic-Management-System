// =============================================================
//  GLMS_API / Controllers / AuthController.cs
//  Firebase token verification endpoint.
//  The MVC sends the Firebase ID token here; if valid, it stores
//  it in session for subsequent API calls.
// =============================================================

using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Mvc;
using GLMS_API.DTOs;

namespace GLMS_API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly ILogger<AuthController> _logger;

    public AuthController(ILogger<AuthController> logger) => _logger = logger;

    // POST /api/auth/verify
    /// <summary>
    /// Verifies a Firebase ID token.
    /// Returns the decoded user claims if valid (uid, email, name).
    /// The MVC client stores the raw token to pass as Authorization: Bearer on all API calls.
    /// </summary>
    [HttpPost("verify")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Verify([FromBody] FirebaseTokenDto dto)
    {
        try
        {
            var decoded = await FirebaseAuth.DefaultInstance
                .VerifyIdTokenAsync(dto.IdToken);

            return Ok(new
            {
                uid = decoded.Uid,
                email = decoded.Claims.TryGetValue("email", out var email) ? email : null,
                name = decoded.Claims.TryGetValue("name", out var name) ? name : null,
                role = decoded.Claims.TryGetValue("role", out var role) ? role : "User",
                token = dto.IdToken   // Echo back so MVC can store it
            });
        }
        catch (FirebaseAuthException ex)
        {
            _logger.LogWarning("Firebase token verification failed: {Msg}", ex.Message);
            return Unauthorized(new { error = "Invalid or expired token." });
        }
    }
}
