namespace TallerMecanico.Modelos;

/// <summary>Representa una fila de la tabla articulos.</summary>
public sealed class Articulo : EntidadBase
{
    public int? IdCategoria { get; set; }
    public string Descripcion { get; set; } = "";
    public decimal Precio { get; set; }
    public int Stock { get; set; }
}
