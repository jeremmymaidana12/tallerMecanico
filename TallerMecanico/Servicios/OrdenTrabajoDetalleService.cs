using System.Data;
using TallerMecanico.Datos;
using TallerMecanico.Modelos;

namespace TallerMecanico.Servicios;

/// <summary>Servicios de una orden. Se guardan a traves de OrdenTrabajoService.</summary>
public class OrdenTrabajoServicioService : ServicioBase<OrdenTrabajoServicio>
{
    protected override string Tabla => "ordenes_trabajo_servicios";

    protected override IReadOnlyDictionary<string, object?> Columnas(OrdenTrabajoServicio s) => new Dictionary<string, object?>
    {
        ["id_orden"] = s.IdOrden,
        ["id_servicio"] = s.IdServicio,
        ["observacion"] = string.IsNullOrWhiteSpace(s.Observacion) ? null : s.Observacion.Trim(),
        ["precio"] = s.Precio
    };

    protected override OrdenTrabajoServicio Mapear(DataRow f) => new()
    {
        Id = Convert.ToInt64(f["id"]),
        IdOrden = Convert.ToInt64(f["id_orden"]),
        IdServicio = Entero(f, "id_servicio"),
        Observacion = TextoONulo(f, "observacion"),
        Precio = Convert.ToDecimal(f["precio"]),
        Servicio = f.Table.Columns.Contains("servicio") ? Texto(f, "servicio") : ""
    };

    protected override void Validar(OrdenTrabajoServicio s)
    {
        Exigir(s.IdOrden > 0, "El servicio no tiene una orden asignada.");
        Exigir(s.IdServicio > 0, "Seleccioná un servicio.");
        Exigir(s.Precio >= 0, "El precio no puede ser negativo.");
    }

    public List<OrdenTrabajoServicio> ListarPorOrden(long idOrden) => Db.ConsultarTabla(@"
        SELECT d.*, COALESCE(s.descripcion, '(servicio eliminado)') AS servicio
          FROM ordenes_trabajo_servicios d
     LEFT JOIN servicios s ON s.id = d.id_servicio
         WHERE d.id_orden = @id
      ORDER BY d.id", Db.P("@id", idOrden)).Rows.Cast<DataRow>().Select(Mapear).ToList();
}

/// <summary>Articulos (repuestos) de una orden. Se guardan a traves de OrdenTrabajoService, que maneja el stock.</summary>
public class OrdenTrabajoArticuloService : ServicioBase<OrdenTrabajoArticulo>
{
    protected override string Tabla => "ordenes_trabajo_articulos";

    protected override IReadOnlyDictionary<string, object?> Columnas(OrdenTrabajoArticulo a) => new Dictionary<string, object?>
    {
        ["id_orden"] = a.IdOrden,
        ["id_articulo"] = a.IdArticulo,
        ["cantidad"] = a.Cantidad,
        ["precio"] = a.Precio
    };

    protected override OrdenTrabajoArticulo Mapear(DataRow f) => new()
    {
        Id = Convert.ToInt64(f["id"]),
        IdOrden = Convert.ToInt64(f["id_orden"]),
        IdArticulo = Entero(f, "id_articulo"),
        Cantidad = Entero(f, "cantidad"),
        Precio = Convert.ToDecimal(f["precio"]),
        Articulo = f.Table.Columns.Contains("articulo") ? Texto(f, "articulo") : ""
    };

    protected override void Validar(OrdenTrabajoArticulo a)
    {
        Exigir(a.IdOrden > 0, "El artículo no tiene una orden asignada.");
        Exigir(a.IdArticulo > 0, "Seleccioná un artículo.");
        Exigir(a.Cantidad > 0, "La cantidad debe ser mayor a cero.");
        Exigir(a.Precio >= 0, "El precio no puede ser negativo.");
    }

    public List<OrdenTrabajoArticulo> ListarPorOrden(long idOrden) => Db.ConsultarTabla(@"
        SELECT d.*, COALESCE(a.descripcion, '(artículo eliminado)') AS articulo
          FROM ordenes_trabajo_articulos d
     LEFT JOIN articulos a ON a.id = d.id_articulo
         WHERE d.id_orden = @id
      ORDER BY d.id", Db.P("@id", idOrden)).Rows.Cast<DataRow>().Select(Mapear).ToList();
}
