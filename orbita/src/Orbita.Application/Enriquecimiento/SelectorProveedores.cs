using Orbita.Domain.Fichas;

namespace Orbita.Application.Enriquecimiento;

public sealed class SelectorProveedores(IEnumerable<IProveedorEnriquecimiento> proveedores)
{
    public IProveedorEnriquecimiento Para(TipoFicha tipo)
    {
        var fuente = FuentesSnapshot.PorTipo(tipo);

        return proveedores.FirstOrDefault(proveedor => proveedor.Fuente == fuente)
            ?? throw new ArgumentOutOfRangeException(nameof(tipo), tipo, "Tipo sin proveedor de enriquecimiento.");
    }
}
