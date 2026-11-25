namespace TallerMecanico.UI;

/// <summary>Ayudas chicas para textos de los formularios.</summary>
public static class Texto
{
    /// <summary>Texto recortado, o null si quedo vacio (para guardar NULL en la base).</summary>
    public static string? ONulo(string texto)
    {
        var t = texto.Trim();
        return t.Length == 0 ? null : t;
    }
}
