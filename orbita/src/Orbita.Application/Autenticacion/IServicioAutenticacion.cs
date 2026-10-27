namespace Orbita.Application.Autenticacion;

public sealed record SesionIniciada(string AccessToken, DateTimeOffset ExpiresAtUtc);

public interface IServicioAutenticacion
{
    SesionIniciada? IniciarSesion(string? usuario, string? password);
}
