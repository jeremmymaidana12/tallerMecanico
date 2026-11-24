using System.Data;
using TallerMecanico.Datos;
using TallerMecanico.Modelos;

namespace TallerMecanico.Servicios;

public class ArticuloService : ServicioBase<Articulo>
{
    protected override string Tabla => "articulos";
    protected override IReadOnlyList<(string Tabla, string Columna)> Referencias => [("ventas_detalle", "id_articulo")];

    protected override IReadOnlyDictionary<string, object?> Columnas(Articulo a) => new Dictionary<string, object?>
    {
        ["id_categoria"] = a.IdCategoria,
        ["descripcion"] = a.Descripcion,
        ["precio"] = a.Precio,
        ["stock"] = a.Stock
    };

    protected override Articulo Mapear(DataRow f) => new()
    {
        Id = Convert.ToInt64(f["id"]),
        IdCategoria = EnteroONulo(f, "id_categoria"),
        Descripcion = Texto(f, "descripcion"),
        Precio = Convert.ToDecimal(f["precio"]),
        Stock = Entero(f, "stock")
    };

    protected override void Validar(Articulo a)
    {
        Exigir(!string.IsNullOrWhiteSpace(a.Descripcion), "Ingresá la descripción del artículo.");
        Exigir(a.Precio >= 0, "El precio no puede ser negativo.");
    }

    /// <summary>Para combos: la descripcion incluye el stock disponible; "nombre" es solo el articulo.</summary>
    public DataTable ListarCombo() => Db.ConsultarTabla(@"
        SELECT id,
               CONCAT(descripcion, '  (stock: ', stock, ')') AS descripcion,
               CONCAT(descripcion, '') AS nombre,
               precio
          FROM articulos
      ORDER BY articulos.descripcion");
}
