namespace TallerMecanico.Servicios;

/// <summary>Datos del usuario (empleado) que inicio sesion.</summary>
public sealed record UsuarioSesion(
    int Id, string Nombre, string Correo, int IdTipo, string Rol,
    int IdEmpresa, string Empresa, int IdSucursal, string Sucursal)
{
    public bool EsAdmin => string.Equals(Rol, Sesion.RolAdministrador, StringComparison.OrdinalIgnoreCase);

    /// <summary>Iniciales para el avatar del menu. Ej.: "María González" = "MG".</summary>
    public string Iniciales => string.Concat(Nombre
        .Split(' ', StringSplitOptions.RemoveEmptyEntries)
        .Take(2)
        .Select(p => char.ToUpperInvariant(p[0])));
}

/// <summary>Usuario conectado en este momento. Todos los formularios lo consultan desde aca.</summary>
public static class Sesion
{
    /// <summary>Texto de tipos_empleados.descripcion que tiene permisos de administrador.</summary>
    public const string RolAdministrador = "Administrador";

    public static UsuarioSesion? Actual { get; private set; }

    public static bool EsAdmin => Actual?.EsAdmin == true;

    public static void Iniciar(UsuarioSesion usuario) => Actual = usuario;

    public static void Cerrar() => Actual = null;
}
