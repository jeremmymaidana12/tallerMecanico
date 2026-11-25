namespace TallerMecanico.Modelos;

/// <summary>Representa una fila de la tabla ventas_cabecera con sus detalles.</summary>
public sealed class VentaCabecera : EntidadBase
{
    public int IdCliente { get; set; }
    public int IdEmpleado { get; set; }
    public int IdEmpresa { get; set; }
    public int IdSucursal { get; set; }
    public decimal Total { get; set; }
    public decimal Grav { get; set; }
    public decimal Exc { get; set; }
    public decimal Iva { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public List<VentaDetalle> Detalles { get; set; } = [];

    /// <summary>Recalcula los importes de la cabecera sumando los detalles.</summary>
    public void CalcularTotales()
    {
        Grav = Detalles.Sum(d => d.Grav);
        Exc = Detalles.Sum(d => d.Excentas);
        Iva = Detalles.Sum(d => d.Iva);
        Total = Grav + Exc + Iva;
    }
}
