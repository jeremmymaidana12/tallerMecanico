using System.Data;
using MySqlConnector;
using TallerMecanico.Datos;
using TallerMecanico.Modelos;

namespace TallerMecanico.Servicios;

/// <summary>Filtros del historial; null = sin filtrar.</summary>
public sealed class FiltroVentas
{
    public DateTime? Desde { get; set; }
    public EstadoVenta? Estado { get; set; }
    public int? IdSucursal { get; set; }
    public int? IdEmpleado { get; set; }
}

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
        ["iva"] = v.Iva,
        ["estado"] = v.Estado.ATexto()
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
        Estado = Estados.Desde<EstadoVenta>(Texto(f, "estado")),
        CreatedAt = FechaONula(f, "created_at"),
        UpdatedAt = FechaONula(f, "updated_at")
    };

    protected override void Validar(VentaCabecera v) =>
        Exigir(v.Detalles.Count > 0, "Agregá al menos un artículo a la venta.");

    public override DataTable Listar() => Listar(new FiltroVentas());

    /// <summary>Historial. Los alias son los titulos; las columnas de dos renglones se separan con un salto.</summary>
    public DataTable Listar(FiltroVentas filtro) => Db.ConsultarTabla(@"
        SELECT v.id,
               CONCAT('#', v.id, '\n', COALESCE(DATE_FORMAT(v.created_at, '%d/%m/%Y %H:%i'), '')) AS `Comprobante / Fecha`,
               CONCAT(COALESCE(CONCAT(c.apellido, ', ', c.nombre), '(cliente eliminado)'), '\n',
                      CASE WHEN COALESCE(c.nro_documento, '') = '' THEN ''
                           WHEN c.nro_documento REGEXP '^[0-9]+$'
                           THEN CONCAT('CI ', REPLACE(FORMAT(c.nro_documento, 0, 'en_US'), ',', '.'))
                           ELSE CONCAT('CI ', c.nro_documento) END)                        AS `Cliente / Documento`,
               CONCAT(COALESCE(e.nombre, '—'), '\n', COALESCE(s.nombre, '—'))              AS `Vendedor / Sucursal`,
               CONCAT(v.estado, '')                                                        AS `Estado`,
               v.total                                                                     AS `Total`,
               ''                                                                          AS `Acciones`,
               COALESCE((SELECT GROUP_CONCAT(a.descripcion SEPARATOR ', ')
                           FROM ventas_detalle d
                           JOIN articulos a ON a.id = d.id_articulo
                          WHERE d.id_venta = v.id), '')                                    AS articulos,
               COALESCE(c.nro_documento, '')                                               AS documento
          FROM ventas_cabecera v
     LEFT JOIN clientes c   ON c.id = v.id_cliente
     LEFT JOIN empleados e  ON e.id = v.id_empleado
     LEFT JOIN sucursales s ON s.id = v.id_sucursal
         WHERE (@desde IS NULL OR v.created_at >= @desde)
           AND (@estado IS NULL OR v.estado = @estado)
           AND (@sucursal IS NULL OR v.id_sucursal = @sucursal)
           AND (@empleado IS NULL OR v.id_empleado = @empleado)
      ORDER BY v.id DESC",
        Db.P("@desde", filtro.Desde), Db.P("@estado", filtro.Estado?.ATexto()),
        Db.P("@sucursal", filtro.IdSucursal), Db.P("@empleado", filtro.IdEmpleado));

    public override VentaCabecera? Obtener(long id)
    {
        var venta = base.Obtener(id);
        if (venta is not null) venta.Detalles = _detalles.ListarPorVenta(id);
        return venta;
    }

    public override long Crear(VentaCabecera v, MySqlTransaction tx)
    {
        Exigir(v.Estado != EstadoVenta.Anulado, "No se puede crear una venta anulada.");
        v.CalcularTotales();
        ValidarRelaciones(v, tx);
        var id = base.Crear(v, tx);
        GuardarDetalles(v, id, tx);
        DescontarStock([], CantidadesPorArticulo(v.Detalles), tx);
        return id;
    }

    public override void Editar(VentaCabecera v, MySqlTransaction tx)
    {
        Exigir(v.Estado != EstadoVenta.Anulado, "Para anular una venta usá CambiarEstado.");
        Exigir(BloquearYLeerEstado(v.Id, tx) != EstadoVenta.Anulado, "Una venta anulada no se puede editar.");
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

    /// <summary>Cambia el estado. Anular devuelve el stock y es definitivo.</summary>
    public void CambiarEstado(long id, EstadoVenta estado) => Db.EnTransaccion(tx =>
    {
        Exigir(BloquearYLeerEstado(id, tx) != EstadoVenta.Anulado, "La venta ya está anulada.");
        if (estado == EstadoVenta.Anulado) DescontarStock(CantidadesGuardadas(id, tx), [], tx);
        return Db.Ejecutar(tx, "UPDATE ventas_cabecera SET estado = @estado, updated_at = NOW() WHERE id = @id",
            Db.P("@estado", estado.ATexto()), Db.P("@id", id));
    });

    /// <summary>Bloquea la venta para que nadie mas la modifique a la vez. Null si no existe.</summary>
    private static EstadoVenta? BloquearYLeerEstado(long id, MySqlTransaction tx)
    {
        var texto = Db.Escalar(tx, "SELECT estado FROM ventas_cabecera WHERE id = @id FOR UPDATE", Db.P("@id", id));
        return texto is null ? null : Estados.Desde<EstadoVenta>(Convert.ToString(texto));
    }

    #region Stock

    private static Dictionary<int, decimal> CantidadesPorArticulo(IEnumerable<VentaDetalle> detalles) =>
        detalles.GroupBy(d => d.IdArticulo).ToDictionary(g => g.Key, g => g.Sum(d => d.Cantidad));

    /// <summary>Cantidades que la venta tiene descontadas del stock (una anulada, ninguna).</summary>
    private static Dictionary<int, decimal> CantidadesGuardadas(long idVenta, MySqlTransaction tx)
    {
        if (BloquearYLeerEstado(idVenta, tx) is null or EstadoVenta.Anulado) return [];
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
