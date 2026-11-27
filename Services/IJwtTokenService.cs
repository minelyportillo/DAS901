using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace ClinicaApp.Services;

public sealed record IssuedJwtToken(string AccessToken, DateTimeOffset ExpiresAt);

public interface IJwtTokenService
{
    IssuedJwtToken IssueToken(ClinicUserSummary user);
}

public sealed class JwtTokenService(
    SymmetricSecurityKey signingKey,
    string issuer,
    string audience) : IJwtTokenService
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(30);

    public IssuedJwtToken IssueToken(ClinicUserSummary user)
    {
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.Add(TokenLifetime);
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims:
            [
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            ],
            notBefore: now.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new IssuedJwtToken(
            new JwtSecurityTokenHandler().WriteToken(token),
            expiresAt);
    }
}
