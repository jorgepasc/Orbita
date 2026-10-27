using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Orbita.Application.Autenticacion;

namespace Orbita.Infrastructure.Autenticacion;

public sealed class ServicioAutenticacionJwt(AuthenticationSettings settings) : IServicioAutenticacion
{
    public SesionIniciada? IniciarSesion(string? usuario, string? password)
    {
        if (!CredentialsMatch(usuario, settings.Username)
            || !CredentialsMatch(password, settings.Password))
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddMinutes(settings.TokenLifetimeMinutes);
        var token = new JwtSecurityToken(
            issuer: settings.Issuer,
            audience: settings.Audience,
            claims: [new Claim(ClaimTypes.Name, settings.Username)],
            notBefore: now.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: new SigningCredentials(
                settings.ClaveDeFirma(),
                SecurityAlgorithms.HmacSha256));

        return new SesionIniciada(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    private static bool CredentialsMatch(string? actual, string expected)
    {
        if (actual is null)
        {
            return false;
        }

        var actualBytes = Encoding.UTF8.GetBytes(actual);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);

        return actualBytes.Length == expectedBytes.Length
            && CryptographicOperations.FixedTimeEquals(actualBytes, expectedBytes);
    }
}
