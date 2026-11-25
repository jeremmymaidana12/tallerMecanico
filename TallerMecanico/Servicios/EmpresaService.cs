using System.Data;
using MySqlConnector;
using TallerMecanico.Datos;
using TallerMecanico.Modelos;

namespace TallerMecanico.Servicios;

public class EmpresaService : ServicioBase<Empresa>
{
    protected override string Tabla => "empresas";
    protected override IReadOnlyList<(string Tabla, string Columna)> Referencias => [("ventas_cabecera", "id_empresa")];

    protected override IReadOnlyDictionary<string, object?> Columnas(Empresa e) => new Dictionary<string, object?>
    {
        ["nombre"] = e.Nombre,
        ["ruc"] = e.Ruc,
        ["direccion"] = e.Direccion,
        ["correo"] = e.Correo,
        ["rubro"] = e.Rubro,
        ["img_url"] = e.ImgUrl
    };

    protected override Empresa Mapear(DataRow f) => new()
    {
        Id = Convert.ToInt64(f["id"]),
        Nombre = Texto(f, "nombre"),
        Ruc = TextoONulo(f, "ruc"),
        Direccion = TextoONulo(f, "direccion"),
        Correo = TextoONulo(f, "correo"),
        Rubro = TextoONulo(f, "rubro"),
        ImgUrl = TextoONulo(f, "img_url")
    };

    protected override void Validar(Empresa e) =>
        Exigir(!string.IsNullOrWhiteSpace(e.Nombre), "Ingresá el nombre de la empresa.");

    /// <summary>Nombre de la sucursal que se crea sola con cada empresa nueva.</summary>
    public const string NombreCasaCentral = "Casa Central";

    /// <summary>Crea la empresa y, en la misma transaccion, su sucursal Casa Central.</summary>
    public override long Crear(Empresa e, MySqlTransaction tx)
    {
        var id = base.Crear(e, tx);
        new SucursalService().Crear(new Sucursal
        {
            IdEmpresa = (int)id,
            Nombre = NombreCasaCentral,
            Direccion = e.Direccion,
            PuntoExpedicion = "001-001"
        }, tx);
        return id;
    }

    /// <summary>Borra la empresa junto con sus sucursales (si nada las usa).</summary>
    public override void Eliminar(long id, MySqlTransaction tx)
    {
        foreach (var (tabla, columna) in Referencias)
        {
            var cantidad = Db.Escalar(tx, $"SELECT COUNT(*) FROM {tabla} WHERE {columna} = @id", Db.P("@id", id));
            Exigir(Convert.ToInt64(cantidad) == 0, $"No se puede eliminar porque tiene registros en {tabla}.");
        }
        Db.Ejecutar(tx, "DELETE FROM sucursales WHERE id_empresa = @id", Db.P("@id", id));
        base.Eliminar(id, tx);
    }

    public DataTable ListarCombo() => Db.ConsultarTabla(
        "SELECT id, nombre AS descripcion FROM empresas ORDER BY nombre");
}
