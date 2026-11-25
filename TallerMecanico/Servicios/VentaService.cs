using System.Data;
using MySqlConnector;
using TallerMecanico.Datos;
using TallerMecanico.Modelos;

namespace TallerMecanico.Servicios;

/// <summary>Ventas: guarda cabecera y detalles juntos en una transaccion.</summary>
public class VentaService : ServicioBase<VentaCabecera>
{
    private readonly VentaDetalleService _detalles = new();

    protected override string Tabla => "ventas_cabecera";
    protected override bool TieneFechas => true;

    protected override IReadOnlyDictionary<string, object?> Columnas(VentaCabecera v) => new Dictionary<string, object?>
    {
        ["id_cliente"] = v.IdCliente,
        ["id_empleado"] = v.IdEmpleado,
        ["id_empresa"] = v.IdEmpresa,
        ["id_sucursal"] = v.IdSucursal,
        ["total"] = v.Total,
        ["grav"] = v.Grav,
        ["exc"] = v.Exc,
        ["iva"] = v.Iva
    };

    protected override VentaCabecera Mapear(DataRow f) => new()
    {
        Id = Convert.ToInt64(f["id"]),
        IdCliente = Entero(f, "id_cliente"),
        IdEmpleado = Entero(f, "id_empleado"),
        IdEmpresa = Entero(f, "id_empresa"),
        IdSucursal = Entero(f, "id_sucursal"),
        Total = Convert.ToDecimal(f["total"]),
        Grav = Convert.ToDecimal(f["grav"]),
        Exc = Convert.ToDecimal(f["exc"]),
        Iva = Convert.ToDecimal(f["iva"]),
        CreatedAt = FechaONula(f, "created_at"),
        UpdatedAt = FechaONula(f, "updated_at")
    };

    protected override void Validar(VentaCabecera v) =>
        Exigir(v.Detalles.Count > 0, "Agregá al menos un artículo a la venta.");

    /// <summary>Listado para la grilla. Los alias son los titulos de las columnas.</summary>
    public override DataTable Listar() => Db.ConsultarTabla(@"
        SELECT v.id,
               v.id + 0                                          AS `Nro.`,
               v.created_at                                      AS `Fecha`,
               COALESCE(CONCAT(c.apellido, ', ', c.nombre), '')  AS `Cliente`,
               COALESCE(e.nombre, '')                            AS `Empleado`,
               COALESCE(em.nombre, '')                           AS `Empresa`,
               COALESCE(s.nombre, '')                            AS `Sucursal`,
               v.grav                                            AS `Gravado`,
               v.exc                                             AS `Exento`,
               v.iva                                             AS `IVA`,
               v.total                                           AS `Total`
          FROM ventas_cabecera v
     LEFT JOIN clientes c   ON c.id  = v.id_cliente
     LEFT JOIN empleados e  ON e.id  = v.id_empleado
     LEFT JOIN empresas em  ON em.id = v.id_empresa
     LEFT JOIN sucursales s ON s.id  = v.id_sucursal
      ORDER BY v.id DESC");

    public override VentaCabecera? Obtener(long id)
    {
        var venta = base.Obtener(id);
        if (venta is not null) venta.Detalles = _detalles.ListarPorVenta(id);
        return venta;
    }

    public override long Crear(VentaCabecera v, MySqlTransaction tx)
    {
        v.CalcularTotales();
        ValidarRelaciones(v, tx);
        var id = base.Crear(v, tx);
        GuardarDetalles(v, id, tx);
        DescontarStock([], CantidadesPorArticulo(v.Detalles), tx);
        return id;
    }

    public override void Editar(VentaCabecera v, MySqlTransaction tx)
    {
        v.CalcularTotales();
        ValidarRelaciones(v, tx);
        var anteriores = CantidadesGuardadas(v.Id, tx);
        base.Editar(v, tx);

        // Se quitan las lineas que ya no estan; las demas conservan su id y created_at.
        var ids = v.Detalles.Where(d => d.Id > 0).Select(d => d.Id).ToList();
        var conservar = ids.Count == 0 ? "" : $" AND id NOT IN ({string.Join(", ", ids)})";
        Db.Ejecutar(tx, "DELETE FROM ventas_detalle WHERE id_venta = @id" + conservar, Db.P("@id", v.Id));
        GuardarDetalles(v, v.Id, tx);
        DescontarStock(anteriores, CantidadesPorArticulo(v.Detalles), tx);
    }

    /// <summary>Devuelve el stock. Los detalles se borran solos: fk_vd_venta es ON DELETE CASCADE.</summary>
    public override void Eliminar(long id, MySqlTransaction tx)
    {
        var anteriores = CantidadesGuardadas(id, tx);
        base.Eliminar(id, tx);
        DescontarStock(anteriores, [], tx);
    }

    #region Stock

    private static Dictionary<int, decimal> CantidadesPorArticulo(IEnumerable<VentaDetalle> detalles) =>
        detalles.GroupBy(d => d.IdArticulo).ToDictionary(g => g.Key, g => g.Sum(d => d.Cantidad));

    /// <summary>Cantidades ya guardadas. Bloquea la venta para que nadie mas la modifique a la vez.</summary>
    private static Dictionary<int, decimal> CantidadesGuardadas(long idVenta, MySqlTransaction tx)
    {
        Db.Escalar(tx, "SELECT id FROM ventas_cabecera WHERE id = @id FOR UPDATE", Db.P("@id", idVenta));
        var tabla = Db.ConsultarTabla(tx, @"
            SELECT id_articulo, SUM(cantidad) AS cantidad
              FROM ventas_detalle
             WHERE id_venta = @id
          GROUP BY id_articulo", Db.P("@id", idVenta));
        return tabla.Rows.Cast<DataRow>().ToDictionary(
            f => Entero(f, "id_articulo"), f => Convert.ToDecimal(f["cantidad"]));
    }

    /// <summary>Aplica al stock solo la diferencia entre lo que habia y lo que queda en la venta.</summary>
    private static void DescontarStock(Dictionary<int, decimal> anteriores, Dictionary<int, decimal> nuevas,
                                       MySqlTransaction tx)
    {
        // Siempre en el mismo orden, para que dos ventas simultaneas no se bloqueen entre si.
        foreach (var idArticulo in anteriores.Keys.Union(nuevas.Keys).Order())
        {
            var diferencia = nuevas.GetValueOrDefault(idArticulo) - anteriores.GetValueOrDefault(idArticulo);
            if (diferencia == 0) continue;

            // La condicion va en el UPDATE: asi no se puede vender dos veces la misma unidad.
            var filas = Db.Ejecutar(tx, @"
                UPDATE articulos
                   SET stock = stock - @cantidad
                 WHERE id = @id
                   AND (@cantidad < 0 OR stock >= @cantidad)",
                Db.P("@cantidad", diferencia), Db.P("@id", idArticulo));
            if (filas > 0 || diferencia < 0) continue;

            var articulo = Db.ConsultarTabla(tx, "SELECT descripcion, stock FROM articulos WHERE id = @id",
                Db.P("@id", idArticulo)).Rows[0];
            Exigir(false, $"Stock insuficiente de «{articulo["descripcion"]}»: " +
                          $"hay {articulo["stock"]} y se necesitan {diferencia:0} más.");
        }
    }

    #endregion

    private void GuardarDetalles(VentaCabecera v, long idVenta, MySqlTransaction tx)
    {
        foreach (var d in v.Detalles)
        {
            d.IdVenta = idVenta;
            if (d.Id > 0) _detalles.Editar(d, tx);
            else _detalles.Crear(d, tx);
        }
    }

    /// <summary>ventas_* no tiene claves foraneas a estas tablas, asi que se controla aca.</summary>
    private static void ValidarRelaciones(VentaCabecera v, MySqlTransaction tx)
    {
        void Existe(string tabla, int id, string mensaje, string filtro = "") =>
            Exigir(Convert.ToInt64(Db.Escalar(tx, $"SELECT COUNT(*) FROM {tabla} WHERE id = @id{filtro}",
                Db.P("@id", id), Db.P("@empresa", v.IdEmpresa))) > 0, mensaje);

        Existe("empresas", v.IdEmpresa, "La empresa seleccionada no existe.");
        Existe("sucursales", v.IdSucursal, "La sucursal no pertenece a la empresa.", " AND id_empresa = @empresa");
        Existe("empleados", v.IdEmpleado, "El empleado no pertenece a la empresa.", " AND id_empresa = @empresa");
        Existe("clientes", v.IdCliente, "El cliente seleccionado no existe.");
        foreach (var d in v.Detalles)
            Existe("articulos", d.IdArticulo, $"El artículo «{d.Articulo}» ya no existe.");
    }
}
