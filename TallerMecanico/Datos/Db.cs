using System.Data;
using MySqlConnector;

namespace TallerMecanico.Datos;

/// <summary>
/// Acceso a la base MySQL/MariaDB de XAMPP. Todos los ABM usan esta clase.
/// </summary>
public static class Db
{
    /// <summary>
    /// Conexion por defecto de XAMPP (usuario root sin clave).
    /// Si tu MySQL tiene otra clave/puerto, crea un archivo "conexion.txt"
    /// junto al .csproj con la cadena completa y se usa esa.
    /// </summary>
    public const string CadenaPorDefecto =
        "Server=localhost;Port=3306;Database=taller_mecanico;Uid=root;Pwd=;CharSet=utf8mb4;";

    public static string CadenaConexion { get; set; } = LeerCadena();

    private static string LeerCadena()
    {
        try
        {
            var archivo = Path.Combine(AppContext.BaseDirectory, "conexion.txt");
            if (File.Exists(archivo))
            {
                var texto = File.ReadAllText(archivo).Trim();
                if (texto.Length > 0) return texto;
            }
        }
        catch
        {
            // Si no se puede leer el archivo se usa la cadena por defecto.
        }
        return CadenaPorDefecto;
    }

    public static MySqlConnection AbrirConexion()
    {
        var conexion = new MySqlConnection(CadenaConexion);
        conexion.Open();
        return conexion;
    }

    /// <summary>Crea un parametro; los null se mandan como NULL a la base.</summary>
    public static MySqlParameter P(string nombre, object? valor) => new(nombre, valor ?? DBNull.Value);

    public static DataTable ConsultarTabla(string sql, params MySqlParameter[] parametros)
    {
        using var conexion = AbrirConexion();
        using var comando = CrearComando(conexion, sql, parametros);
        using var lector = comando.ExecuteReader();
        var tabla = new DataTable();
        tabla.Load(lector);
        return tabla;
    }

    public static int Ejecutar(string sql, params MySqlParameter[] parametros)
    {
        using var conexion = AbrirConexion();
        using var comando = CrearComando(conexion, sql, parametros);
        return comando.ExecuteNonQuery();
    }

    /// <summary>Ejecuta un INSERT y devuelve el id autoincremental generado.</summary>
    public static long Insertar(string sql, params MySqlParameter[] parametros)
    {
        using var conexion = AbrirConexion();
        using var comando = CrearComando(conexion, sql, parametros);
        comando.ExecuteNonQuery();
        return comando.LastInsertedId;
    }

    public static object? Escalar(string sql, params MySqlParameter[] parametros)
    {
        using var conexion = AbrirConexion();
        using var comando = CrearComando(conexion, sql, parametros);
        var valor = comando.ExecuteScalar();
        return valor == DBNull.Value ? null : valor;
    }

    public static bool ProbarConexion(out string error)
    {
        try
        {
            using var conexion = AbrirConexion();
            error = "";
            return true;
        }
        catch (Exception ex)
        {
            error = TraducirError(ex);
            return false;
        }
    }

    /// <summary>Convierte los errores de MySQL en mensajes entendibles para el usuario.</summary>
    public static string TraducirError(Exception ex)
    {
        if (ex is MySqlException mex)
        {
            return (int)mex.ErrorCode switch
            {
                1042 => "No se pudo conectar con MySQL. Verifica que el servicio MySQL este iniciado en el panel de XAMPP.",
                1045 => "Usuario o clave de MySQL incorrectos. Revisa la cadena de conexion (conexion.txt).",
                1049 => "La base 'taller_mecanico' no existe. Importa el script sql/taller_mecanico.sql desde phpMyAdmin.",
                1146 => "Falta una tabla en la base. Volve a importar el script sql/taller_mecanico.sql.",
                1451 => "No se puede eliminar porque tiene registros relacionados (por ejemplo, ordenes de trabajo).",
                1452 => "El dato relacionado seleccionado ya no existe. Actualiza la pantalla e intenta de nuevo.",
                1062 => "Ya existe un registro con ese valor.",
                1406 => "Uno de los textos es demasiado largo para el campo.",
                _ => $"Error de base de datos ({(int)mex.ErrorCode}): {mex.Message}"
            };
        }
        return ex.Message;
    }

    private static MySqlCommand CrearComando(MySqlConnection conexion, string sql, MySqlParameter[] parametros)
    {
        var comando = new MySqlCommand(sql, conexion);
        if (parametros.Length > 0) comando.Parameters.AddRange(parametros);
        return comando;
    }
}
