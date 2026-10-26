using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Orbita.Infrastructure.Autenticacion;

public sealed class AuthenticationSettings
{
    public const string SectionName = "Authentication";

    public string Username { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public string SigningKey { get; init; } = string.Empty;

    public string Issuer { get; init; } = "Orbita";

    public string Audience { get; init; } = "Orbita.Web";

    public int TokenLifetimeMinutes { get; init; } = 60;

    public bool HasValidValues => !string.IsNullOrWhiteSpace(Username)
        && !string.IsNullOrWhiteSpace(Password)
        && !string.IsNullOrEmpty(SigningKey)
        && Encoding.UTF8.GetByteCount(SigningKey) >= 32
        && TokenLifetimeMinutes >= 1;

    public SymmetricSecurityKey ClaveDeFirma() => new(Encoding.UTF8.GetBytes(SigningKey));
}
