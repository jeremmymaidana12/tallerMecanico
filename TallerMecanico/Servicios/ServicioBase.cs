using System.Data;
using MySqlConnector;
using TallerMecanico.Datos;
using TallerMecanico.Modelos;

namespace TallerMecanico.Servicios;

public enum Operacion { Crear, Editar, Eliminar }

/// <summary>
/// Base de todos los services: ya trae Listar, Obtener, Crear, Editar y Eliminar.
///
/// Para crear un service nuevo, heredar e indicar Tabla, Columnas y Mapear.
/// Opcionalmente sobrescribir Listar, Validar y Referencias.
/// Ver ArticuloService como ejemplo corto.
/// </summary>
public abstract class ServicioBase<T> where T : EntidadBase
{
    protected abstract string Tabla { get; }

    /// <summary>Columnas que se guardan (sin el id) y el valor de cada una.</summary>
    protected abstract IReadOnlyDictionary<string, object?> Columnas(T entidad);

    protected abstract T Mapear(DataRow fila);

    /// <summary>True si la tabla tiene created_at y updated_at.</summary>
    protected virtual bool TieneFechas => false;

    /// <summary>Tablas sin clave foranea que usan este registro: impiden eliminarlo.</summary>
    protected virtual IReadOnlyList<(string Tabla, string Columna)> Referencias => [];

    /// <summary>Lanza una excepcion si los datos no son validos.</summary>
    protected virtual void Validar(T entidad) { }

    public virtual DataTable Listar() => Db.ConsultarTabla($"SELECT * FROM {Tabla} ORDER BY id");

    public virtual T? Obtener(long id)
    {
        var tabla = Db.ConsultarTabla($"SELECT * FROM {Tabla} WHERE id = @id", Db.P("@id", id));
        return tabla.Rows.Count == 0 ? null : Mapear(tabla.Rows[0]);
    }

    /// <summary>Ejecuta la operacion indicada y devuelve el id del registro.</summary>
    public long Procesar(Operacion operacion, T entidad)
    {
        switch (operacion)
        {
            case Operacion.Crear: return Crear(entidad);
            case Operacion.Editar: Editar(entidad); break;
            case Operacion.Eliminar: Eliminar(entidad.Id); break;
            default: throw new ArgumentOutOfRangeException(nameof(operacion));
        }
        return entidad.Id;
    }

    public long Crear(T entidad) => Db.EnTransaccion(tx => Crear(entidad, tx));

    public void Editar(T entidad) => Db.EnTransaccion(tx => { Editar(entidad, tx); return 0; });

    public void Eliminar(long id) => Db.EnTransaccion(tx => { Eliminar(id, tx); return 0; });

    /// <summary>Crea dentro de una transaccion abierta (para guardar varias tablas juntas).</summary>
    public virtual long Crear(T entidad, MySqlTransaction tx)
    {
        Validar(entidad);
        var columnas = Columnas(entidad);
        var nombres = string.Join(", ", columnas.Keys);
        var valores = string.Join(", ", columnas.Keys.Select(c => "@" + c));
        if (TieneFechas)
        {
            nombres += ", created_at, updated_at";
            valores += ", NOW(), NOW()";
        }
        return Db.Insertar(tx, $"INSERT INTO {Tabla} ({nombres}) VALUES ({valores})", Parametros(columnas));
    }

    public virtual void Editar(T entidad, MySqlTransaction tx)
    {
        Validar(entidad);
        var columnas = Columnas(entidad);
        var cambios = string.Join(", ", columnas.Keys.Select(c => $"{c} = @{c}"));
        if (TieneFechas) cambios += ", updated_at = NOW()";
        ExigirFila(Db.Ejecutar(tx, $"UPDATE {Tabla} SET {cambios} WHERE id = @id",
            [.. Parametros(columnas), Db.P("@id", entidad.Id)]));
    }

    public virtual void Eliminar(long id, MySqlTransaction tx)
    {
        foreach (var (tabla, columna) in Referencias)
        {
            var cantidad = Db.Escalar(tx, $"SELECT COUNT(*) FROM {tabla} WHERE {columna} = @id", Db.P("@id", id));
            Exigir(Convert.ToInt64(cantidad) == 0, $"No se puede eliminar porque tiene registros en {tabla}.");
        }
        ExigirFila(Db.Ejecutar(tx, $"DELETE FROM {Tabla} WHERE id = @id", Db.P("@id", id)));
    }

    #region Auxiliares

    protected static void Exigir(bool condicion, string mensaje)
    {
        if (!condicion) throw new InvalidOperationException(mensaje);
    }

    protected static string Texto(DataRow f, string columna) => Convert.ToString(f[columna]) ?? "";
    protected static string? TextoONulo(DataRow f, string columna) => f[columna] as string;
    protected static int Entero(DataRow f, string columna) => Convert.ToInt32(f[columna]);
    protected static int? EnteroONulo(DataRow f, string columna) =>
        f[columna] is DBNull ? null : Convert.ToInt32(f[columna]);
    protected static DateTime? FechaONula(DataRow f, string columna) =>
        f[columna] is DBNull ? null : Convert.ToDateTime(f[columna]);

    private static void ExigirFila(int filas) =>
        Exigir(filas > 0, "El registro ya no existe. Actualizá la pantalla.");

    private static MySqlParameter[] Parametros(IReadOnlyDictionary<string, object?> columnas) =>
        columnas.Select(c => Db.P("@" + c.Key, c.Value)).ToArray();

    #endregion
}
