using System.Data;
using TallerMecanico.Modelos;

namespace TallerMecanico.Datos;

/// <summary>Consultas y operaciones sobre la tabla vehiculos.</summary>
public sealed class VehiculoDatos
{
    /// <summary>Listado para la grilla. Los alias son los titulos de las columnas.</summary>
    public DataTable Listar() => Db.ConsultarTabla(@"
        SELECT v.id,
               v.chapa                             AS `Chapa`,
               COALESCE(m.descripcion, '')         AS `Marca`,
               COALESCE(v.modelo, '')              AS `Modelo`,
               v.anho                              AS `Año`,
               COALESCE(v.color, '')               AS `Color`,
               CONCAT(c.apellido, ', ', c.nombre)  AS `Cliente`,
               COALESCE(c.nro_documento, '')       AS `Documento`,
               COALESCE(c.telefono, '')            AS `Teléfono`
          FROM vehiculos v
          JOIN clientes c ON c.id = v.id_cliente
     LEFT JOIN marcas m   ON m.id = v.id_marca
      ORDER BY v.chapa");

    public Vehiculo? Obtener(int id)
    {
        var tabla = Db.ConsultarTabla(@"
            SELECT id, id_cliente, chapa, id_marca, color, observacion, modelo, anho
              FROM vehiculos
             WHERE id = @id", Db.P("@id", id));

        if (tabla.Rows.Count == 0) return null;
        var f = tabla.Rows[0];
        return new Vehiculo
        {
            Id = Convert.ToInt32(f["id"]),
            IdCliente = Convert.ToInt32(f["id_cliente"]),
            Chapa = Convert.ToString(f["chapa"]) ?? "",
            IdMarca = f["id_marca"] is DBNull ? null : Convert.ToInt32(f["id_marca"]),
            Color = f["color"] as string,
            Observacion = f["observacion"] as string,
            Modelo = f["modelo"] as string,
            Anho = f["anho"] is DBNull ? null : Convert.ToInt32(f["anho"])
        };
    }

    public int Insertar(Vehiculo v) => (int)Db.Insertar(@"
        INSERT INTO vehiculos (id_cliente, chapa, id_marca, color, observacion, modelo, anho)
        VALUES (@id_cliente, @chapa, @id_marca, @color, @observacion, @modelo, @anho)",
        Parametros(v));

    public void Actualizar(Vehiculo v)
    {
        var parametros = Parametros(v).Append(Db.P("@id", v.Id)).ToArray();
        Db.Ejecutar(@"
            UPDATE vehiculos
               SET id_cliente  = @id_cliente,
                   chapa       = @chapa,
                   id_marca    = @id_marca,
                   color       = @color,
                   observacion = @observacion,
                   modelo      = @modelo,
                   anho        = @anho
             WHERE id = @id", parametros);
    }

    public void Eliminar(int id) =>
        Db.Ejecutar("DELETE FROM vehiculos WHERE id = @id", Db.P("@id", id));

    /// <summary>Indica si la chapa ya esta cargada en otro vehiculo (ignora espacios y mayusculas).</summary>
    public bool ExisteChapa(string chapa, int? excluirId)
    {
        var cantidad = Db.Escalar(@"
            SELECT COUNT(*)
              FROM vehiculos
             WHERE UPPER(REPLACE(chapa, ' ', '')) = @chapa
               AND (@id IS NULL OR id <> @id)",
            Db.P("@chapa", NormalizarChapa(chapa)), Db.P("@id", excluirId));
        return Convert.ToInt32(cantidad) > 0;
    }

    /// <summary>Clientes para el combo: id + "Apellido, Nombre - Documento".</summary>
    public DataTable ListarClientes() => Db.ConsultarTabla(@"
        SELECT id,
               CONCAT(apellido, ', ', nombre,
                      IF(COALESCE(nro_documento, '') = '', '', CONCAT('  -  ', nro_documento))) AS descripcion
          FROM clientes
      ORDER BY apellido, nombre");

    public DataTable ListarMarcas() => Db.ConsultarTabla(
        "SELECT id, descripcion FROM marcas ORDER BY descripcion");

    public DataTable ListarModelos(int idMarca) => Db.ConsultarTabla(
        "SELECT id, descripcion FROM modelos WHERE id_marca = @marca ORDER BY descripcion",
        Db.P("@marca", idMarca));

    public static string NormalizarChapa(string chapa) =>
        chapa.Replace(" ", "").Trim().ToUpperInvariant();

    private static MySqlConnector.MySqlParameter[] Parametros(Vehiculo v) =>
    [
        Db.P("@id_cliente", v.IdCliente),
        Db.P("@chapa", v.Chapa),
        Db.P("@id_marca", v.IdMarca),
        Db.P("@color", v.Color),
        Db.P("@observacion", v.Observacion),
        Db.P("@modelo", v.Modelo),
        Db.P("@anho", v.Anho)
    ];
}
