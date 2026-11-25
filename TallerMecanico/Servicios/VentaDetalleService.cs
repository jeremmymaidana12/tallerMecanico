using System.Data;
using TallerMecanico.Datos;
using TallerMecanico.Modelos;

namespace TallerMecanico.Servicios;

/// <summary>Lineas de venta. Se guardan a traves de VentaService, que mantiene los totales y el stock.</summary>
public class VentaDetalleService : ServicioBase<VentaDetalle>
{
    protected override string Tabla => "ventas_detalle";
    protected override bool TieneFechas => true;

    protected override IReadOnlyDictionary<string, object?> Columnas(VentaDetalle d) => new Dictionary<string, object?>
    {
        ["id_venta"] = d.IdVenta,
        ["id_articulo"] = d.IdArticulo,
        ["cantidad"] = d.Cantidad,
        ["precio"] = d.Precio,
        ["iva"] = d.Iva,
        ["grav"] = d.Grav,
        ["excentas"] = d.Excentas
    };

    protected override VentaDetalle Mapear(DataRow f) => new()
    {
        Id = Convert.ToInt64(f["id"]),
        IdVenta = Convert.ToInt64(f["id_venta"]),
        IdArticulo = Entero(f, "id_articulo"),
        Cantidad = Convert.ToDecimal(f["cantidad"]),
        Precio = Convert.ToDecimal(f["precio"]),
        Iva = Convert.ToDecimal(f["iva"]),
        Grav = Convert.ToDecimal(f["grav"]),
        Excentas = Convert.ToDecimal(f["excentas"]),
        CreatedAt = FechaONula(f, "created_at"),
        UpdatedAt = FechaONula(f, "updated_at"),
        Articulo = f.Table.Columns.Contains("articulo") ? Texto(f, "articulo") : ""
    };

    protected override void Validar(VentaDetalle d)
    {
        Exigir(d.IdVenta > 0, "El detalle no tiene una venta asignada.");
        Exigir(d.IdArticulo > 0, "Seleccioná un artículo.");
        Exigir(d.Cantidad > 0, "La cantidad debe ser mayor a cero.");
        Exigir(d.Cantidad == decimal.Truncate(d.Cantidad), "La cantidad debe ser un número entero.");
        Exigir(d.Precio >= 0, "El precio no puede ser negativo.");
    }

    public List<VentaDetalle> ListarPorVenta(long idVenta)
    {
        var tabla = Db.ConsultarTabla(@"
            SELECT d.*, COALESCE(a.descripcion, '(artículo eliminado)') AS articulo
              FROM ventas_detalle d
         LEFT JOIN articulos a ON a.id = d.id_articulo
             WHERE d.id_venta = @id
          ORDER BY d.id", Db.P("@id", idVenta));
        return tabla.Rows.Cast<DataRow>().Select(Mapear).ToList();
    }
}
