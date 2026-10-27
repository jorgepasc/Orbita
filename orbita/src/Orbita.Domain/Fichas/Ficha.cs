using System.Diagnostics.CodeAnalysis;

namespace Orbita.Domain.Fichas;

public sealed class Ficha
{
    private Ficha()
    {
    }

    public Guid Id { get; private set; } = Guid.NewGuid();

    public string Titulo { get; private set; } = string.Empty;

    public TipoFicha Tipo { get; private set; }

    public DateOnly? Fecha { get; private set; }

    public bool Terminado { get; private set; }

    public decimal? Valoracion { get; private set; }

    public string? Comentarios { get; private set; }

    public string? Compania { get; private set; }

    public string? Plataforma { get; private set; }

    public string? Idioma { get; private set; }

    public string? Autor { get; private set; }

    public Snapshot? Snapshot { get; private set; }

    public static bool TryCrear(
        DatosFicha datos,
        Snapshot? snapshot,
        [NotNullWhen(true)] out Ficha? ficha,
        out ErroresValidacion errores)
    {
        errores = Validar(datos, snapshot, out var tipo);
        if (errores.Hay)
        {
            ficha = null;
            return false;
        }

        ficha = new Ficha();
        ficha.Aplicar(datos, tipo);
        ficha.Snapshot = snapshot;
        return true;
    }

    // Recrea una Ficha de una Copia de seguridad conservando su Id original.
    public static bool TryRestaurar(
        Guid id,
        DatosFicha datos,
        Snapshot? snapshot,
        [NotNullWhen(true)] out Ficha? ficha,
        out ErroresValidacion errores)
    {
        var creada = TryCrear(datos, snapshot, out ficha, out errores);
        if (id == Guid.Empty)
        {
            errores.Agregar("Id", "El Id es obligatorio.");
        }

        if (!creada || errores.Hay)
        {
            ficha = null;
            return false;
        }

        ficha!.Id = id;
        return true;
    }

    // El Snapshot es inmutable tras crear (ADR 0002): no se puede pasar aquí.
    public ErroresValidacion Actualizar(DatosFicha datos)
    {
        var errores = Validar(datos, Snapshot, out var tipo);
        if (!errores.Hay)
        {
            Aplicar(datos, tipo);
        }

        return errores;
    }

    private static ErroresValidacion Validar(DatosFicha datos, Snapshot? snapshot, out TipoFicha tipo)
    {
        var errores = new ErroresValidacion();

        if (string.IsNullOrWhiteSpace(datos.Titulo))
        {
            errores.Agregar("Titulo", "El título es obligatorio.");
        }
        else if (datos.Titulo.Trim().Length > LimitesFicha.Titulo)
        {
            errores.Agregar("Titulo", "El título no puede superar los 300 caracteres.");
        }

        if (!TiposFicha.TryParse(datos.Tipo, out tipo))
        {
            errores.Agregar("Tipo", "El Tipo debe ser Película, Serie, Libro, Juego o Cómic.");
        }
        else
        {
            if (snapshot is not null)
            {
                if (string.IsNullOrWhiteSpace(snapshot.IdExterno))
                {
                    errores.Agregar("snapshot.idExterno", "El Snapshot necesita un idExterno.");
                }
                else if (!FuentesSnapshot.EsCompatible(snapshot.Fuente, tipo))
                {
                    errores.Agregar(
                        "snapshot.fuente",
                        $"La fuente '{FuentesSnapshot.Nombre(snapshot.Fuente)}' no es válida para el Tipo {TiposFicha.Nombre(tipo)}.");
                }
            }

            if (!string.IsNullOrWhiteSpace(datos.Plataforma) && tipo != TipoFicha.Juego)
            {
                errores.Agregar("Plataforma", "La Plataforma solo está disponible para el Tipo Juego.");
            }

            if (!string.IsNullOrWhiteSpace(datos.Compania) && tipo == TipoFicha.Libro)
            {
                errores.Agregar("Compania", "La Compañía no está disponible para el Tipo Libro.");
            }

            if (!string.IsNullOrWhiteSpace(datos.Autor) && tipo != TipoFicha.Libro)
            {
                errores.Agregar("Autor", "El Autor solo está disponible para el Tipo Libro.");
            }

            if (!string.IsNullOrWhiteSpace(datos.Idioma) && tipo is not (TipoFicha.Libro or TipoFicha.Comic))
            {
                errores.Agregar("Idioma", "El Idioma solo está disponible para los Tipos Libro y Cómic.");
            }
        }

        if (datos.Fecha is null)
        {
            errores.Agregar("Fecha", "La Fecha es obligatoria.");
        }

        if (datos.Valoracion is < 0 or > 10)
        {
            errores.Agregar("Valoracion", "La Valoración debe estar entre 0 y 10.");
        }

        if (datos.Comentarios?.Length > LimitesFicha.Comentarios)
        {
            errores.Agregar("Comentarios", "Los Comentarios no pueden superar los 4000 caracteres.");
        }

        if (datos.Compania?.Length > LimitesFicha.Texto)
        {
            errores.Agregar("Compania", "La Compañía no puede superar los 200 caracteres.");
        }

        if (datos.Plataforma?.Length > LimitesFicha.Texto)
        {
            errores.Agregar("Plataforma", "La Plataforma no puede superar los 200 caracteres.");
        }

        if (datos.Autor?.Length > LimitesFicha.Texto)
        {
            errores.Agregar("Autor", "El Autor no puede superar los 200 caracteres.");
        }

        if (datos.Idioma?.Length > LimitesFicha.Texto)
        {
            errores.Agregar("Idioma", "El Idioma no puede superar los 200 caracteres.");
        }

        return errores;
    }

    private void Aplicar(DatosFicha datos, TipoFicha tipo)
    {
        Titulo = datos.Titulo!.Trim();
        Tipo = tipo;
        Fecha = datos.Fecha;
        Terminado = datos.Terminado;
        Valoracion = datos.Valoracion;
        Comentarios = datos.Comentarios;
        Compania = datos.Compania;
        Plataforma = datos.Plataforma;
        Idioma = datos.Idioma;
        Autor = datos.Autor;
    }
}
