namespace TallerMecanico.Modelos;

/// <summary>Representa una fila de la tabla vehiculos.</summary>
public sealed class Vehiculo
{
    public int Id { get; set; }
    public int IdCliente { get; set; }
    public string Chapa { get; set; } = "";
    public int? IdMarca { get; set; }
    public string? Color { get; set; }
    public string? Observacion { get; set; }
    public string? Modelo { get; set; }
    public int? Anho { get; set; }
}
