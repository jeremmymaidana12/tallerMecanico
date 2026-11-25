using System.Data;
using TallerMecanico.Datos;
using TallerMecanico.Modelos;

namespace TallerMecanico.Servicios;

public class SucursalService : ServicioBase<Sucursal>
{
    protected override string Tabla => "sucursales";
    protected override IReadOnlyList<(string Tabla, string Columna)> Referencias => [("ventas_cabecera", "id_sucursal")];

    protected override IReadOnlyDictionary<string, object?> Columnas(Sucursal s) => new Dictionary<string, object?>
    {
        ["id_empresa"] = s.IdEmpresa,
        ["nombre"] = s.Nombre,
        ["direccion"] = s.Direccion,
        ["timbrado"] = s.Timbrado,
        ["punto_expedicion"] = s.PuntoExpedicion,
        ["ultimo_numero"] = s.UltimoNumero
    };

    protected override Sucursal Mapear(DataRow f) => new()
    {
        Id = Convert.ToInt64(f["id"]),
        IdEmpresa = Entero(f, "id_empresa"),
        Nombre = Texto(f, "nombre"),
        Direccion = TextoONulo(f, "direccion"),
        Timbrado = TextoONulo(f, "timbrado"),
        PuntoExpedicion = TextoONulo(f, "punto_expedicion"),
        UltimoNumero = EnteroONulo(f, "ultimo_numero") ?? 0
    };

    protected override void Validar(Sucursal s)
    {
        Exigir(!string.IsNullOrWhiteSpace(s.Nombre), "Ingresá el nombre de la sucursal.");
        Exigir(s.IdEmpresa > 0, "Seleccioná la empresa de la sucursal.");
    }

    public DataTable ListarCombo(int idEmpresa) => Db.ConsultarTabla(
        "SELECT id, nombre AS descripcion FROM sucursales WHERE id_empresa = @empresa ORDER BY nombre",
        Db.P("@empresa", idEmpresa));
}
