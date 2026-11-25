using System.Data;

namespace TallerMecanico.UI;

/// <summary>Carga y lectura de combos "id + descripcion" usados en todos los ABM.</summary>
public static class Combos
{
    public const string SinEspecificar = "— Sin especificar —";

    /// <summary>
    /// Llena el combo con una tabla que tenga columnas "id" y "descripcion".
    /// Si se pasa textoVacio, agrega esa opcion al principio con id nulo.
    /// </summary>
    public static void Cargar(ComboBox combo, DataTable datos, string? textoVacio = null)
    {
        if (textoVacio is not null)
        {
            datos.PrimaryKey = [];
            datos.Columns["id"]!.AllowDBNull = true;
            var vacio = datos.NewRow();
            vacio["id"] = DBNull.Value;
            vacio["descripcion"] = textoVacio;
            datos.Rows.InsertAt(vacio, 0);
        }
        var anterior = Valor(combo);
        combo.DisplayMember = "descripcion";
        combo.ValueMember = "id";
        combo.DataSource = datos;
        Seleccionar(combo, anterior);
    }

    /// <summary>Id elegido o null.</summary>
    public static int? Valor(ComboBox combo) =>
        combo.SelectedValue is null or DBNull ? null : Convert.ToInt32(combo.SelectedValue);

    /// <summary>Selecciona por id; con null elige la opcion vacia (o ninguna).</summary>
    public static void Seleccionar(ComboBox combo, object? id)
    {
        if (combo.DataSource is not DataTable tabla) return;
        for (int i = 0; i < tabla.Rows.Count; i++)
        {
            var valor = tabla.Rows[i]["id"];
            bool coincide = id is null or DBNull
                ? valor is DBNull
                : valor is not DBNull && Convert.ToInt64(valor) == Convert.ToInt64(id);
            if (coincide)
            {
                combo.SelectedIndex = i;
                return;
            }
        }
        combo.SelectedIndex = tabla.Rows.Count > 0 && combo.DropDownStyle == ComboBoxStyle.DropDownList ? 0 : -1;
    }
}
