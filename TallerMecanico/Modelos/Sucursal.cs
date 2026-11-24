namespace TallerMecanico.Modelos;

/// <summary>Representa una fila de la tabla sucursales.</summary>
public sealed class Sucursal : EntidadBase
{
    public int IdEmpresa { get; set; }
    public string Nombre { get; set; } = "";
    public string? Direccion { get; set; }
    public string? Timbrado { get; set; }
    public string? PuntoExpedicion { get; set; }
    public int UltimoNumero { get; set; }
}
