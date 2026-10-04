using System.Net;
using System.Text;

namespace Orbita.Api.Tests;

public sealed class StubApisExternas : HttpMessageHandler
{
    private readonly Dictionary<string, (HttpStatusCode Estado, string Json)> respuestas = [];

    public List<string> Solicitudes { get; } = [];

    public void Responder(string urlContiene, string json, HttpStatusCode estado = HttpStatusCode.OK) =>
        respuestas[urlContiene] = (estado, json);

    public void Reiniciar()
    {
        respuestas.Clear();
        Solicitudes.Clear();
    }

    public List<string> SolicitudesQueContienen(string fragmento) =>
        [.. Solicitudes.Where(url => url.Contains(fragmento, StringComparison.Ordinal))];

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var url = request.RequestUri?.AbsoluteUri
            ?? throw new InvalidOperationException("La petición stubbeada no tiene URL.");

        Solicitudes.Add(url);

        var clave = respuestas.Keys.FirstOrDefault(url.Contains)
            ?? throw new InvalidOperationException($"No hay respuesta stubbeada para {url}.");
        var (estado, json) = respuestas[clave];

        return Task.FromResult(new HttpResponseMessage(estado)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });
    }
}
