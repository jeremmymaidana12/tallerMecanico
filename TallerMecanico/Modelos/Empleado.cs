namespace TallerMecanico.Modelos;

/// <summary>Representa una fila de la tabla empleados.</summary>
public sealed class Empleado : EntidadBase
{
    public string Nombre { get; set; } = "";
    public string Correo { get; set; } = "";
    /// <summary>Hash de la clave, nunca la clave en texto plano.</summary>
    public string PasswordHash { get; set; } = "";
    public int IdTipoEmpleado { get; set; }
    public bool Activo { get; set; } = true;
    public int IdEmpresa { get; set; }
    public int IdSucursal { get; set; }
}
