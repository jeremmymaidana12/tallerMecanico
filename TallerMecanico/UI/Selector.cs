using System.Data;

namespace TallerMecanico.UI;

/// <summary>Lista emergente con busqueda. Usa las columnas "id" y "descripcion".</summary>
public static class Selector
{
    public static void Mostrar(Control ancla, Point posicion, DataTable datos, Action<DataRow> elegir)
    {
        var buscar = new TextBox
        {
            Dock = DockStyle.Top, Font = Lienzo.Fuente(13), PlaceholderText = "Escribí para buscar...",
            BorderStyle = BorderStyle.FixedSingle
        };
        var lista = new ListBox
        {
            Dock = DockStyle.Fill, Font = Lienzo.Fuente(13), BorderStyle = BorderStyle.None, IntegralHeight = false
        };
        var panel = new Panel { Size = new Size(Lienzo.Px(300), Lienzo.Px(260)), BackColor = Tema.Superficie };
        panel.Controls.Add(lista);
        panel.Controls.Add(buscar);

        var filas = new List<DataRow>();
        void Filtrar()
        {
            filas = Buscar(datos, buscar.Text);
            lista.BeginUpdate();
            lista.Items.Clear();
            foreach (var f in filas) lista.Items.Add(Convert.ToString(f["descripcion"]) ?? "");
            if (lista.Items.Count > 0) lista.SelectedIndex = 0;
            lista.EndUpdate();
        }

        var emergente = new ToolStripDropDown { Padding = Padding.Empty, AutoSize = true };
        emergente.Items.Add(new ToolStripControlHost(panel)
        {
            Margin = Padding.Empty, Padding = Padding.Empty, AutoSize = false, Size = panel.Size
        });
        void Aceptar()
        {
            if (lista.SelectedIndex < 0) return;
            var fila = filas[lista.SelectedIndex];
            emergente.Close();
            elegir(fila);
        }

        buscar.TextChanged += (_, _) => Filtrar();
        buscar.KeyDown += (_, e) => Teclas(e, lista, Aceptar);
        lista.MouseClick += (_, _) => Aceptar();
        // Se libera recien cuando termina de procesarse el cierre.
        emergente.Closed += (_, _) => ancla.BeginInvoke(emergente.Dispose);
        Filtrar();
        emergente.Show(ancla, posicion);
        buscar.Focus();
    }

    /// <summary>Filas que contienen todas las palabras, sin distinguir mayusculas ni tildes.</summary>
    public static List<DataRow> Buscar(DataTable datos, string texto)
    {
        var comparar = System.Globalization.CultureInfo.InvariantCulture.CompareInfo;
        const System.Globalization.CompareOptions opciones =
            System.Globalization.CompareOptions.IgnoreCase | System.Globalization.CompareOptions.IgnoreNonSpace;
        var palabras = texto.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return datos.Rows.Cast<DataRow>().Where(f =>
        {
            var fila = $"{f["id"]} {f["descripcion"]}";
            return palabras.All(p => comparar.IndexOf(fila, p, opciones) >= 0);
        }).ToList();
    }

    public static void Teclas(KeyEventArgs e, ListBox lista, Action aceptar)
    {
        switch (e.KeyCode)
        {
            case Keys.Down when lista.SelectedIndex < lista.Items.Count - 1: lista.SelectedIndex++; break;
            case Keys.Up when lista.SelectedIndex > 0: lista.SelectedIndex--; break;
            case Keys.Enter: aceptar(); break;
            default: return;
        }
        e.Handled = e.SuppressKeyPress = true;
    }
}
