using System.Data;
using MySqlConnector;
using TallerMecanico.Datos;
using TallerMecanico.Modelos;

namespace TallerMecanico.Servicios;

/// <summary>Filtros del historial de ordenes; null = sin filtrar.</summary>
public sealed class FiltroOrdenes
{
    public DateTime? Desde { get; set; }
    public EstadoOrdenTrabajo? Estado { get; set; }
    /// <summary>True = solo las que todavia no se entregaron ni se cancelaron.</summary>
    public bool SoloEnCurso { get; set; }
    public int? IdEmpleado { get; set; }
}

/// <summary>
/// Ordenes de trabajo: guarda la cabecera y sus dos detalles (servicios y articulos) en una transaccion.
/// Los articulos descuentan stock como en una venta; cancelar o eliminar la orden lo devuelve.
/// </summary>
public class OrdenTrabajoService : ServicioBase<OrdenTrabajo>
{
    private readonly OrdenTrabajoServicioService _servicios = new();
    private readonly OrdenTrabajoArticuloService _articulos = new();

    protected override string Tabla => "ordenes_trabajo";
    protected override IReadOnlyList<(string Tabla, string Columna)> Referencias => [("facturas", "id_orden")];

    protected override IReadOnlyDictionary<string, object?> Columnas(OrdenTrabajo o) => new Dictionary<string, object?>
    {
        ["id_empleado"] = o.IdEmpleado,
        ["id_vehiculo"] = o.IdVehiculo,
        ["fecha_recepcion"] = o.FechaRecepcion,
        ["estado"] = o.Estado.ATexto(),
        ["diagnostico"] = string.IsNullOrWhiteSpace(o.Diagnostico) ? null : o.Diagnostico.Trim(),
        ["fecha_diagnostico"] = o.FechaDiagnostico,
        ["fecha_finalizacion"] = o.FechaFinalizacion,
        ["id_empresa"] = o.IdEmpresa
    };

    protected override OrdenTrabajo Mapear(DataRow f) => new()
    {
        Id = Convert.ToInt64(f["id"]),
        IdEmpleado = Entero(f, "id_empleado"),
        IdVehiculo = Entero(f, "id_vehiculo"),
        IdEmpresa = Entero(f, "id_empresa"),
        FechaRecepcion = Convert.ToDateTime(f["fecha_recepcion"]),
        Estado = Estados.Desde<EstadoOrdenTrabajo>(Texto(f, "estado")),
        Diagnostico = TextoONulo(f, "diagnostico"),
        FechaDiagnostico = FechaONula(f, "fecha_diagnostico"),
        FechaFinalizacion = FechaONula(f, "fecha_finalizacion")
    };

    protected override void Validar(OrdenTrabajo o)
    {
        Exigir(o.IdVehiculo > 0, "Seleccioná el vehículo.");
        Exigir(o.IdEmpleado > 0, "Seleccioná el mecánico responsable.");
        Exigir(o.Servicios.Count + o.Articulos.Count > 0, "Agregá al menos un servicio o un artículo.");
    }

    public override DataTable Listar() => Listar(new FiltroOrdenes());

    /// <summary>Historial. Los alias son los titulos; las columnas de dos renglones se separan con un salto.</summary>
    public DataTable Listar(FiltroOrdenes filtro) => Db.ConsultarTabla(@"
        SELECT o.id,
               CONCAT('#', o.id, '\n', DATE_FORMAT(o.fecha_recepcion, '%d/%m/%Y %H:%i'))       AS `Orden / Recepción`,
               CONCAT(v.chapa, CONCAT_WS(' ', '', NULLIF(m.descripcion, ''), NULLIF(v.modelo, '')), '\n',
                      COALESCE(CONCAT(c.apellido, ', ', c.nombre), '(cliente eliminado)'))     AS `Vehículo / Cliente`,
               CONCAT(COALESCE(e.nombre, '—'), '\n',
                      COALESCE(ts.cantidad, 0), IF(COALESCE(ts.cantidad, 0) = 1, ' servicio', ' servicios'), ' · ',
                      COALESCE(ta.cantidad, 0), IF(COALESCE(ta.cantidad, 0) = 1, ' artículo', ' artículos'))
                                                                                                AS `Mecánico / Trabajo`,
               CONCAT(o.estado, '')                                                             AS `Estado`,
               COALESCE(ts.total, 0) + COALESCE(ta.total, 0)                                    AS `Total`,
               ''                                                                               AS `Acciones`,
               COALESCE(ts.nombres, '')                                                         AS servicios,
               COALESCE(ta.nombres, '')                                                         AS articulos,
               COALESCE(o.diagnostico, '')                                                      AS diagnostico,
               COALESCE(c.nro_documento, '')                                                    AS documento
          FROM ordenes_trabajo o
          JOIN vehiculos v      ON v.id = o.id_vehiculo
     LEFT JOIN marcas m         ON m.id = v.id_marca
     LEFT JOIN clientes c       ON c.id = v.id_cliente
     LEFT JOIN empleados e      ON e.id = o.id_empleado
     LEFT JOIN (SELECT d.id_orden, COUNT(*) AS cantidad, SUM(d.precio) AS total,
                       GROUP_CONCAT(s.descripcion SEPARATOR ', ') AS nombres
                  FROM ordenes_trabajo_servicios d
             LEFT JOIN servicios s ON s.id = d.id_servicio
              GROUP BY d.id_orden) ts ON ts.id_orden = o.id
     LEFT JOIN (SELECT d.id_orden, COUNT(*) AS cantidad, SUM(d.cantidad * d.precio) AS total,
                       GROUP_CONCAT(a.descripcion SEPARATOR ', ') AS nombres
                  FROM ordenes_trabajo_articulos d
             LEFT JOIN articulos a ON a.id = d.id_articulo
              GROUP BY d.id_orden) ta ON ta.id_orden = o.id
         WHERE (@desde IS NULL OR o.fecha_recepcion >= @desde)
           AND (@estado IS NULL OR o.estado = @estado)
           AND (@curso = 0 OR o.estado NOT IN ('Entregado', 'Cancelado'))
           AND (@empleado IS NULL OR o.id_empleado = @empleado)
      ORDER BY o.id DESC",
        Db.P("@desde", filtro.Desde), Db.P("@estado", filtro.Estado?.ATexto()),
        Db.P("@curso", filtro.SoloEnCurso ? 1 : 0), Db.P("@empleado", filtro.IdEmpleado));

    public override OrdenTrabajo? Obtener(long id)
    {
        var orden = base.Obtener(id);
        if (orden is null) return null;
        orden.Servicios = _servicios.ListarPorOrden(id);
        orden.Articulos = _articulos.ListarPorOrden(id);
        return orden;
    }

    public override long Crear(OrdenTrabajo o, MySqlTransaction tx)
    {
        Exigir(o.Estado != EstadoOrdenTrabajo.Cancelado, "No se puede crear una orden cancelada.");
        o.ActualizarFechas();
        ValidarRelaciones(o, tx);
        var id = base.Crear(o, tx);
        GuardarDetalles(o, id, tx);
        DescontarStock([], CantidadesPorArticulo(o.Articulos), tx);
        return id;
    }

    public override void Editar(OrdenTrabajo o, MySqlTransaction tx)
    {
        Exigir(o.Estado != EstadoOrdenTrabajo.Cancelado, "Para cancelar una orden usá CambiarEstado.");
        Exigir(BloquearYLeerEstado(o.Id, tx) != EstadoOrdenTrabajo.Cancelado, "Una orden cancelada no se puede editar.");
        o.ActualizarFechas();
        ValidarRelaciones(o, tx);
        var anteriores = CantidadesGuardadas(o.Id, tx);
        base.Editar(o, tx);

        // Se quitan las lineas que ya no estan; las demas conservan su id.
        BorrarQuitadas("ordenes_trabajo_servicios", o.Id, o.Servicios.Select(s => s.Id), tx);
        BorrarQuitadas("ordenes_trabajo_articulos", o.Id, o.Articulos.Select(a => a.Id), tx);
        GuardarDetalles(o, o.Id, tx);
        DescontarStock(anteriores, CantidadesPorArticulo(o.Articulos), tx);
    }

    /// <summary>Los detalles no tienen ON DELETE CASCADE: se borran primero. Devuelve el stock.</summary>
    public override void Eliminar(long id, MySqlTransaction tx)
    {
        var facturas = Db.Escalar(tx, "SELECT COUNT(*) FROM facturas WHERE id_orden = @id", Db.P("@id", id));
        Exigir(Convert.ToInt64(facturas) == 0, "No se puede eliminar porque la orden ya está facturada. Cancelala en su lugar.");
        var anteriores = CantidadesGuardadas(id, tx);
        Db.Ejecutar(tx, "DELETE FROM ordenes_trabajo_servicios WHERE id_orden = @id", Db.P("@id", id));
        Db.Ejecutar(tx, "DELETE FROM ordenes_trabajo_articulos WHERE id_orden = @id", Db.P("@id", id));
        base.Eliminar(id, tx);
        DescontarStock(anteriores, [], tx);
    }

    /// <summary>Cambia el estado y registra las fechas. Cancelar devuelve el stock y es definitivo.</summary>
    public void CambiarEstado(long id, EstadoOrdenTrabajo estado) => Db.EnTransaccion(tx =>
    {
        var fila = Db.ConsultarTabla(tx, "SELECT * FROM ordenes_trabajo WHERE id = @id FOR UPDATE", Db.P("@id", id));
        Exigir(fila.Rows.Count > 0, "La orden ya no existe. Actualizá la pantalla.");
        var orden = Mapear(fila.Rows[0]);
        Exigir(orden.Estado != EstadoOrdenTrabajo.Cancelado, "La orden ya está cancelada.");

        if (estado == EstadoOrdenTrabajo.Cancelado) DescontarStock(CantidadesGuardadas(id, tx), [], tx);
        orden.Estado = estado;
        orden.ActualizarFechas();
        return Db.Ejecutar(tx, @"
            UPDATE ordenes_trabajo
               SET estado = @estado, fecha_diagnostico = @diagnostico, fecha_finalizacion = @finalizacion
             WHERE id = @id",
            Db.P("@estado", estado.ATexto()), Db.P("@diagnostico", orden.FechaDiagnostico),
            Db.P("@finalizacion", orden.FechaFinalizacion), Db.P("@id", id));
    });

    #region Listas para elegir

    /// <summary>Vehiculos: id + "CHAPA · Marca Modelo · Apellido, Nombre".</summary>
    public DataTable ListarVehiculos() => Db.ConsultarTabla(@"
        SELECT v.id,
               CONCAT_WS(' · ', v.chapa, NULLIF(TRIM(CONCAT_WS(' ', m.descripcion, v.modelo)), ''),
                         CONCAT(c.apellido, ', ', c.nombre)) AS descripcion,
               v.chapa,
               CONCAT(c.apellido, ', ', c.nombre) AS cliente
          FROM vehiculos v
          JOIN clientes c ON c.id = v.id_cliente
     LEFT JOIN marcas m   ON m.id = v.id_marca
      ORDER BY v.chapa");

    /// <summary>Catalogo de servicios: "descripcion" para buscar, "nombre" y "precio" para la linea.</summary>
    public DataTable ListarServicios() => Db.ConsultarTabla(@"
        SELECT id,
               descripcion,
               CONCAT(descripcion, '') AS nombre,
               precio_servicio AS precio
          FROM servicios
      ORDER BY descripcion");

    /// <summary>Empresa del usuario conectado; si no hay sesion, la primera.</summary>
    public static int EmpresaPorDefecto() =>
        Sesion.Actual?.IdEmpresa ?? Convert.ToInt32(Db.Escalar("SELECT MIN(id) FROM empresas") ?? 0);

    #endregion

    #region Stock

    private static Dictionary<int, int> CantidadesPorArticulo(IEnumerable<OrdenTrabajoArticulo> articulos) =>
        articulos.GroupBy(a => a.IdArticulo).ToDictionary(g => g.Key, g => g.Sum(a => a.Cantidad));

    /// <summary>Cantidades que la orden tiene descontadas del stock (una cancelada, ninguna).</summary>
    private static Dictionary<int, int> CantidadesGuardadas(long idOrden, MySqlTransaction tx)
    {
        if (BloquearYLeerEstado(idOrden, tx) is null or EstadoOrdenTrabajo.Cancelado) return [];
        var tabla = Db.ConsultarTabla(tx, @"
            SELECT id_articulo, SUM(cantidad) AS cantidad
              FROM ordenes_trabajo_articulos
             WHERE id_orden = @id
          GROUP BY id_articulo", Db.P("@id", idOrden));
        return tabla.Rows.Cast<DataRow>().ToDictionary(f => Entero(f, "id_articulo"), f => Entero(f, "cantidad"));
    }

    /// <summary>Aplica al stock solo la diferencia entre lo que habia y lo que queda en la orden.</summary>
    private static void DescontarStock(Dictionary<int, int> anteriores, Dictionary<int, int> nuevas, MySqlTransaction tx)
    {
        // Siempre en el mismo orden, para que dos operaciones simultaneas no se bloqueen entre si.
        foreach (var idArticulo in anteriores.Keys.Union(nuevas.Keys).Order())
        {
            var diferencia = nuevas.GetValueOrDefault(idArticulo) - anteriores.GetValueOrDefault(idArticulo);
            if (diferencia == 0) continue;

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
                          $"hay {articulo["stock"]} y se necesitan {diferencia} más.");
        }
    }

    #endregion

    /// <summary>Bloquea la orden para que nadie mas la modifique a la vez. Null si no existe.</summary>
    private static EstadoOrdenTrabajo? BloquearYLeerEstado(long id, MySqlTransaction tx)
    {
        var texto = Db.Escalar(tx, "SELECT estado FROM ordenes_trabajo WHERE id = @id FOR UPDATE", Db.P("@id", id));
        return texto is null ? null : Estados.Desde<EstadoOrdenTrabajo>(Convert.ToString(texto));
    }

    private static void BorrarQuitadas(string tabla, long idOrden, IEnumerable<long> ids, MySqlTransaction tx)
    {
        var conservar = ids.Where(i => i > 0).ToList();
        var filtro = conservar.Count == 0 ? "" : $" AND id NOT IN ({string.Join(", ", conservar)})";
        Db.Ejecutar(tx, $"DELETE FROM {tabla} WHERE id_orden = @id" + filtro, Db.P("@id", idOrden));
    }

    private void GuardarDetalles(OrdenTrabajo o, long idOrden, MySqlTransaction tx)
    {
        foreach (var s in o.Servicios)
        {
            s.IdOrden = idOrden;
            if (s.Id > 0) _servicios.Editar(s, tx);
            else s.Id = _servicios.Crear(s, tx);
        }
        foreach (var a in o.Articulos)
        {
            a.IdOrden = idOrden;
            if (a.Id > 0) _articulos.Editar(a, tx);
            else a.Id = _articulos.Crear(a, tx);
        }
    }

    private static void ValidarRelaciones(OrdenTrabajo o, MySqlTransaction tx)
    {
        void Existe(string tabla, long id, string mensaje, string filtro = "") =>
            Exigir(Convert.ToInt64(Db.Escalar(tx, $"SELECT COUNT(*) FROM {tabla} WHERE id = @id{filtro}",
                Db.P("@id", id), Db.P("@empresa", o.IdEmpresa))) > 0, mensaje);

        Existe("empresas", o.IdEmpresa, "La empresa de la orden no existe.");
        Existe("empleados", o.IdEmpleado, "El mecánico no pertenece a la empresa.", " AND id_empresa = @empresa");
        Existe("vehiculos", o.IdVehiculo, "El vehículo seleccionado ya no existe.");
        foreach (var s in o.Servicios)
            Existe("servicios", s.IdServicio, $"El servicio «{s.Servicio}» ya no existe.");
        foreach (var a in o.Articulos)
            Existe("articulos", a.IdArticulo, $"El artículo «{a.Articulo}» ya no existe.");
    }
}
