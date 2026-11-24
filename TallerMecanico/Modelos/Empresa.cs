namespace TallerMecanico.Modelos;

/// <summary>Representa una fila de la tabla empresas.</summary>
public sealed class Empresa : EntidadBase
{
    public string Nombre { get; set; } = "";
    public string? Ruc { get; set; }
    public string? Direccion { get; set; }
    public string? Correo { get; set; }
    public string? Rubro { get; set; }
    public string? ImgUrl { get; set; }
}
