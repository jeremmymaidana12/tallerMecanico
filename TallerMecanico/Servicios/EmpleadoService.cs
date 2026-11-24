using System.Data;
using TallerMecanico.Datos;
using TallerMecanico.Modelos;

namespace TallerMecanico.Servicios;

public class EmpleadoService : ServicioBase<Empleado>
{
    protected override string Tabla => "empleados";
    protected override IReadOnlyList<(string Tabla, string Columna)> Referencias => [("ventas_cabecera", "id_empleado")];

    protected override IReadOnlyDictionary<string, object?> Columnas(Empleado e) => new Dictionary<string, object?>
    {
        ["nombre"] = e.Nombre,
        ["correo"] = e.Correo,
        ["password_hash"] = e.PasswordHash,
        ["id_tipo_empleado"] = e.IdTipoEmpleado,
        ["activo"] = e.Activo,
        ["id_empresa"] = e.IdEmpresa,
        ["id_sucursal"] = e.IdSucursal
    };

    protected override Empleado Mapear(DataRow f) => new()
    {
        Id = Convert.ToInt64(f["id"]),
        Nombre = Texto(f, "nombre"),
        Correo = Texto(f, "correo"),
        PasswordHash = Texto(f, "password_hash"),
        IdTipoEmpleado = Entero(f, "id_tipo_empleado"),
        Activo = f["activo"] is not DBNull && Convert.ToBoolean(f["activo"]),
        IdEmpresa = Entero(f, "id_empresa"),
        IdSucursal = Entero(f, "id_sucursal")
    };

    protected override void Validar(Empleado e)
    {
        Exigir(!string.IsNullOrWhiteSpace(e.Nombre), "Ingresá el nombre del empleado.");
        Exigir(!string.IsNullOrWhiteSpace(e.Correo), "Ingresá el correo del empleado.");
        Exigir(e.PasswordHash.Length > 0, "El empleado debe tener una clave.");
    }

    /// <summary>Listado sin el hash de la clave.</summary>
    public override DataTable Listar() => Db.ConsultarTabla(@"
        SELECT id, nombre, correo, id_tipo_empleado, activo, id_empresa, id_sucursal
          FROM empleados
      ORDER BY nombre");

    /// <summary>Activos de la empresa, mas el empleado indicado aunque este inactivo.</summary>
    public DataTable ListarCombo(int idEmpresa, int? incluirId = null) => Db.ConsultarTabla(@"
        SELECT id, nombre AS descripcion
          FROM empleados
         WHERE id_empresa = @empresa
           AND (activo = 1 OR id = @incluir)
      ORDER BY nombre", Db.P("@empresa", idEmpresa), Db.P("@incluir", incluirId));
}
