using System.ComponentModel;
using System.Reflection;

namespace TallerMecanico.Modelos;

// Los valores deben coincidir con los ENUM de la base. [Description] va solo cuando
// el texto de la base no puede ser un nombre de C# (por ejemplo, si tiene espacios).

/// <summary>ordenes_trabajo.estado</summary>
public enum EstadoOrdenTrabajo
{
    Recibido,
    Diagnosticado,
    Presupuestado,
    Aceptado,
    [Description("En reparacion")] EnReparacion,
    Finalizado,
    Entregado,
    Cancelado
}

/// <summary>facturas.estado</summary>
public enum EstadoFactura
{
    Pendiente,
    Pagada,
    Anulada
}

/// <summary>factura_detalle.tipo_item</summary>
public enum TipoItemFactura
{
    Articulo,
    Servicio
}

/// <summary>empleados.activo (0 = Inactivo, 1 = Activo)</summary>
public enum EstadoEmpleado
{
    Inactivo = 0,
    Activo = 1
}

/// <summary>Convierte los enums al texto que se guarda en la base y viceversa.</summary>
public static class Estados
{
    /// <summary>Texto para la base. Ej.: EstadoOrdenTrabajo.EnReparacion.ATexto() = "En reparacion".</summary>
    public static string ATexto<T>(this T valor) where T : struct, Enum =>
        typeof(T).GetField(valor.ToString())?.GetCustomAttribute<DescriptionAttribute>()?.Description
        ?? valor.ToString();

    /// <summary>Enum a partir del texto de la base. Ej.: Estados.Desde&lt;EstadoFactura&gt;("Pagada").</summary>
    public static T Desde<T>(string? texto) where T : struct, Enum
    {
        foreach (var valor in Enum.GetValues<T>())
        {
            if (string.Equals(valor.ATexto(), texto, StringComparison.OrdinalIgnoreCase)) return valor;
        }
        throw new ArgumentException($"«{texto}» no es un valor válido de {typeof(T).Name}.", nameof(texto));
    }

    /// <summary>Todos los textos, en orden, para cargar un combo.</summary>
    public static string[] Textos<T>() where T : struct, Enum =>
        Enum.GetValues<T>().Select(v => v.ATexto()).ToArray();
}
