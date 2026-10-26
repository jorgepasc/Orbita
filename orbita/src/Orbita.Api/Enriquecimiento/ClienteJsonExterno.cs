using System.Net.Http.Json;
using System.Text.Json;

using Orbita.Application.Enriquecimiento;

namespace Orbita.Api.Enriquecimiento;

internal static class ClienteJsonExterno
{
    public static async Task<T> EnviarAsync<T>(
        HttpClient http,
        HttpRequestMessage solicitud,
        string fuente,
        JsonSerializerOptions? opciones,
        CancellationToken cancellationToken)
    {
        using (solicitud)
        using (var respuesta = await http.SendAsync(solicitud, cancellationToken))
        {
            if (!respuesta.IsSuccessStatusCode)
            {
                throw new ApiExternaException(
                    $"{fuente} respondió {(int)respuesta.StatusCode} al consultar la API.");
            }

            return await respuesta.Content.ReadFromJsonAsync<T>(opciones ?? JsonSerializerOptions.Web, cancellationToken)
                ?? throw new ApiExternaException($"{fuente} devolvió una respuesta vacía.");
        }
    }
}
