using ClinicaApp.DTOs;
using ClinicaApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicaApp.Controllers.Api;

/// <summary>Emite tokens de acceso para los usuarios registrados.</summary>
[ApiController]
[Route("api/auth")]
[AllowAnonymous]
[Produces("application/json")]
public sealed class AuthApiController(
    IClinicUserService userService,
    IJwtTokenService tokenService,
    ILogger<AuthApiController> logger) : ControllerBase
{
    /// <summary>Valida las credenciales y devuelve un token JWT de corta duración.</summary>
    [HttpPost("token")]
    [ProducesResponseType(typeof(ApiTokenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiTokenResponseDto>> CreateToken(
        [FromBody] ApiTokenRequestDto request,
        CancellationToken cancellationToken)
    {
        var user = await userService.AuthenticateAsync(
            request.Email,
            request.Password,
            cancellationToken);
        if (user is null)
        {
            logger.LogWarning("Falló una solicitud de token API por credenciales inválidas.");
            return Unauthorized(new ProblemDetails
            {
                Title = "Credenciales inválidas",
                Detail = "El correo o la contraseña no son correctos.",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        var token = tokenService.IssueToken(user);
        Response.Headers.CacheControl = "no-store";
        Response.Headers.Pragma = "no-cache";

        return Ok(new ApiTokenResponseDto(
            token.AccessToken,
            "Bearer",
            Math.Max(0, (int)(token.ExpiresAt - DateTimeOffset.UtcNow).TotalSeconds)));
    }
}
