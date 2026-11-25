using System.Data;
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

    public DataTable ListarCombo() => Db.ConsultarTabla(
        "SELECT id, nombre AS descripcion FROM empresas ORDER BY nombre");
}
