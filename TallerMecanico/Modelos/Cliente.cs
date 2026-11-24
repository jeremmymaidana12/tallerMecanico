namespace TallerMecanico.Modelos;

/// <summary>Representa una fila de la tabla clientes.</summary>
public sealed class Cliente : EntidadBase
{
    public string Nombre { get; set; } = "";
    public string Apellido { get; set; } = "";
    public DateTime? Nacimiento { get; set; }
    public string? Direccion { get; set; }
    public string? NroDocumento { get; set; }
    public string? Dv { get; set; }
    public string? Telefono { get; set; }
    public string? Correo { get; set; }
    public int? IdEmpresa { get; set; }
    public int? IdSucursal { get; set; }
}
