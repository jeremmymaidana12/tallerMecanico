using System.Data;
using TallerMecanico.Datos;
using TallerMecanico.Modelos;

namespace TallerMecanico.Servicios;

public class ClienteService : ServicioBase<Cliente>
{
    protected override string Tabla => "clientes";
    protected override IReadOnlyList<(string Tabla, string Columna)> Referencias => [("ventas_cabecera", "id_cliente")];

    protected override IReadOnlyDictionary<string, object?> Columnas(Cliente c) => new Dictionary<string, object?>
    {
        ["nombre"] = c.Nombre,
        ["apellido"] = c.Apellido,
        ["nacimiento"] = c.Nacimiento,
        ["direccion"] = c.Direccion,
        ["nro_documento"] = c.NroDocumento,
        ["dv"] = c.Dv,
        ["telefono"] = c.Telefono,
        ["correo"] = c.Correo,
        ["id_empresa"] = c.IdEmpresa,
        ["id_sucursal"] = c.IdSucursal
    };

    protected override Cliente Mapear(DataRow f) => new()
    {
        Id = Convert.ToInt64(f["id"]),
        Nombre = Texto(f, "nombre"),
        Apellido = Texto(f, "apellido"),
        Nacimiento = FechaONula(f, "nacimiento"),
        Direccion = TextoONulo(f, "direccion"),
        NroDocumento = TextoONulo(f, "nro_documento"),
        Dv = TextoONulo(f, "dv"),
        Telefono = TextoONulo(f, "telefono"),
        Correo = TextoONulo(f, "correo"),
        IdEmpresa = EnteroONulo(f, "id_empresa"),
        IdSucursal = EnteroONulo(f, "id_sucursal")
    };

    protected override void Validar(Cliente c) =>
        Exigir(!string.IsNullOrWhiteSpace(c.Nombre) && !string.IsNullOrWhiteSpace(c.Apellido),
            "Ingresá el nombre y el apellido del cliente.");

    /// <summary>Para combos: id + "Apellido, Nombre - Documento".</summary>
    public DataTable ListarCombo() => Db.ConsultarTabla(@"
        SELECT id,
               CONCAT(apellido, ', ', nombre,
                      IF(COALESCE(nro_documento, '') = '', '', CONCAT('  -  ', nro_documento))) AS descripcion
          FROM clientes
      ORDER BY apellido, nombre");
}
