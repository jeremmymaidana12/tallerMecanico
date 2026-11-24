namespace TallerMecanico.Modelos;

/// <summary>Representa una fila de la tabla ventas_detalle.</summary>
public sealed class VentaDetalle : EntidadBase
{
    /// <summary>El precio ya incluye IVA 10%: el impuesto es precio / 11.</summary>
    private const decimal DivisorIva = 11m;

    public long IdVenta { get; set; }
    public int IdArticulo { get; set; }
    public decimal Cantidad { get; set; } = 1;
    public decimal Precio { get; set; }
    public decimal Iva { get; set; }
    public decimal Grav { get; set; }
    public decimal Excentas { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Descripcion del articulo (viene de articulos, no se guarda).</summary>
    public string Articulo { get; set; } = "";

    public decimal Subtotal => Grav + Iva + Excentas;

    /// <summary>Calcula grav (neto sin IVA), iva y excentas a partir de cantidad y precio.</summary>
    public void Calcular(bool exenta)
    {
        var importe = Math.Round(Cantidad * Precio, 2, MidpointRounding.AwayFromZero);
        Excentas = exenta ? importe : 0;
        Iva = exenta ? 0 : Math.Round(importe / DivisorIva, 2, MidpointRounding.AwayFromZero);
        Grav = exenta ? 0 : importe - Iva;
    }
}
