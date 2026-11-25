using System.Data;
using TallerMecanico.Datos;
using TallerMecanico.Modelos;

namespace TallerMecanico.Servicios;

/// <summary>Inicio de sesion de los empleados y creacion del administrador inicial.</summary>
public sealed class AutenticacionService
{
    /// <summary>Usuario que se crea solo si no hay ningun administrador que pueda ingresar.</summary>
    public const string CorreoAdminInicial = "admin@taller.com";
    public const string ClaveAdminInicial = "admin123";

    /// <summary>
    /// Valida correo y clave. Devuelve el usuario o lanza InvalidOperationException
    /// con un mensaje para mostrar (sin decir cual de los dos datos esta mal).
    /// </summary>
    public UsuarioSesion Ingresar(string correo, string clave)
    {
        var t = Db.ConsultarTabla(@"
            SELECT em.id, em.nombre, em.correo, em.password_hash, em.activo,
                   em.id_tipo_empleado, t.descripcion AS rol,
                   em.id_empresa, e.nombre AS empresa, em.id_sucursal, s.nombre AS sucursal
              FROM empleados em
              JOIN tipos_empleados t ON t.id = em.id_tipo_empleado
              JOIN empresas e        ON e.id = em.id_empresa
              JOIN sucursales s      ON s.id = em.id_sucursal
             WHERE em.correo = @correo", Db.P("@correo", correo.Trim()));

        const string Incorrecto = "Correo o clave incorrectos.";
        if (t.Rows.Count == 0) throw new InvalidOperationException(Incorrecto);
        var f = t.Rows[0];
        if (!Seguridad.VerificarClave(clave, Convert.ToString(f["password_hash"]) ?? ""))
            throw new InvalidOperationException(Incorrecto);
        if (f["activo"] is DBNull || !Convert.ToBoolean(f["activo"]))
            throw new InvalidOperationException("Tu usuario está desactivado. Pedile a un administrador que lo active.");

        var usuario = new UsuarioSesion(
            Convert.ToInt32(f["id"]), Convert.ToString(f["nombre"])!, Convert.ToString(f["correo"])!,
            Convert.ToInt32(f["id_tipo_empleado"]), Convert.ToString(f["rol"])!,
            Convert.ToInt32(f["id_empresa"]), Convert.ToString(f["empresa"])!,
            Convert.ToInt32(f["id_sucursal"]), Convert.ToString(f["sucursal"])!);

        Db.Ejecutar("UPDATE empleados SET ultimo_acceso = NOW() WHERE id = @id", Db.P("@id", usuario.Id));
        return usuario;
    }

    /// <summary>
    /// Si no existe ningun administrador activo con clave, crea (o repara) admin@taller.com / admin123.
    /// Devuelve true si lo tuvo que crear, para avisarlo en la pantalla de ingreso.
    /// </summary>
    public bool AsegurarAdministrador()
    {
        var idRol = IdRolAdministrador();
        var admins = Db.Escalar(@"
            SELECT COUNT(*) FROM empleados
             WHERE id_tipo_empleado = @rol AND activo = 1 AND password_hash LIKE 'pbkdf2$%'",
            Db.P("@rol", idRol));
        if (Convert.ToInt32(admins) > 0) return false;

        var hash = Seguridad.HashClave(ClaveAdminInicial);
        var existente = Db.Escalar("SELECT id FROM empleados WHERE correo = @c", Db.P("@c", CorreoAdminInicial));
        if (existente is not null)
        {
            Db.Ejecutar("UPDATE empleados SET password_hash = @h, id_tipo_empleado = @rol, activo = 1 WHERE id = @id",
                Db.P("@h", hash), Db.P("@rol", idRol), Db.P("@id", existente));
            return true;
        }

        var (idEmpresa, idSucursal) = EmpresaYSucursalParaAdmin();
        Db.Insertar(@"
            INSERT INTO empleados (nombre, correo, password_hash, id_tipo_empleado, activo, id_empresa, id_sucursal)
            VALUES ('Administrador', @c, @h, @rol, 1, @e, @s)",
            Db.P("@c", CorreoAdminInicial), Db.P("@h", hash), Db.P("@rol", idRol),
            Db.P("@e", idEmpresa), Db.P("@s", idSucursal));
        return true;
    }

    /// <summary>Id del tipo "Administrador" (lo crea si no esta).</summary>
    public static int IdRolAdministrador()
    {
        var id = Db.Escalar("SELECT id FROM tipos_empleados WHERE UPPER(descripcion) = UPPER(@d) LIMIT 1",
            Db.P("@d", Sesion.RolAdministrador));
        return id is not null
            ? Convert.ToInt32(id)
            : (int)Db.Insertar("INSERT INTO tipos_empleados (descripcion) VALUES (@d)", Db.P("@d", Sesion.RolAdministrador));
    }

    private static (int Empresa, int Sucursal) EmpresaYSucursalParaAdmin()
    {
        var t = Db.ConsultarTabla(@"
            SELECT s.id_empresa, s.id FROM sucursales s ORDER BY s.id_empresa, s.id LIMIT 1");
        if (t.Rows.Count > 0)
            return (Convert.ToInt32(t.Rows[0]["id_empresa"]), Convert.ToInt32(t.Rows[0]["id"]));

        // Base vacia: se crea una empresa (que a su vez crea su Casa Central).
        var idEmpresa = (int)new EmpresaService().Crear(new Empresa { Nombre = "Mi Taller" });
        var idSucursal = Convert.ToInt32(Db.Escalar(
            "SELECT id FROM sucursales WHERE id_empresa = @e ORDER BY id LIMIT 1", Db.P("@e", idEmpresa)));
        return (idEmpresa, idSucursal);
    }
}
