using System.Reflection;

namespace TallerMecanico.UI;

/// <summary>
/// Paleta de colores, fuentes y estilos comunes de toda la aplicacion.
/// Cambiando algo aca cambia en todos los formularios.
/// </summary>
public static class Tema
{
    // Colores principales
    public static readonly Color Primario = Color.FromArgb(37, 99, 235);
    public static readonly Color PrimarioHover = Color.FromArgb(29, 78, 216);
    public static readonly Color PrimarioSuave = Color.FromArgb(219, 234, 254);
    public static readonly Color Peligro = Color.FromArgb(220, 38, 38);
    public static readonly Color PeligroHover = Color.FromArgb(185, 28, 28);
    public static readonly Color PeligroSuave = Color.FromArgb(254, 242, 242);
    public static readonly Color Exito = Color.FromArgb(22, 163, 74);

    // Superficies y textos
    public static readonly Color Fondo = Color.FromArgb(241, 245, 249);
    public static readonly Color Superficie = Color.White;
    public static readonly Color SuperficieAlterna = Color.FromArgb(248, 250, 252);
    public static readonly Color Borde = Color.FromArgb(226, 232, 240);
    public static readonly Color Texto = Color.FromArgb(15, 23, 42);
    public static readonly Color TextoSuave = Color.FromArgb(100, 116, 139);
    public static readonly Color Deshabilitado = Color.FromArgb(203, 213, 225);

    // Menu lateral
    public static readonly Color Lateral = Color.FromArgb(15, 23, 42);
    public static readonly Color LateralHover = Color.FromArgb(30, 41, 59);
    public static readonly Color LateralTexto = Color.FromArgb(203, 213, 225);
    public static readonly Color LateralTextoSuave = Color.FromArgb(100, 116, 139);

    // Fuentes
    public static readonly Font Fuente = new("Segoe UI", 9.75F);
    public static readonly Font FuenteNegrita = new("Segoe UI Semibold", 9.75F);
    public static readonly Font FuenteEtiqueta = new("Segoe UI Semibold", 9F);
    public static readonly Font FuenteChica = new("Segoe UI", 8.25F);
    public static readonly Font FuenteSubtitulo = new("Segoe UI Semibold", 13F);
    public static readonly Font FuenteTitulo = new("Segoe UI Semibold", 18F);

    public enum EstiloBoton { Primario, Secundario, Peligro }

    /// <summary>Boton plano con colores del tema; se ve atenuado cuando esta deshabilitado.</summary>
    public static void Boton(Button boton, EstiloBoton estilo)
    {
        boton.FlatStyle = FlatStyle.Flat;
        boton.UseVisualStyleBackColor = false;
        boton.Cursor = Cursors.Hand;
        boton.Font = FuenteNegrita;
        boton.FlatAppearance.BorderSize = estilo == EstiloBoton.Secundario ? 1 : 0;
        boton.FlatAppearance.BorderColor = Borde;

        void Pintar()
        {
            var activo = boton.Enabled;
            switch (estilo)
            {
                case EstiloBoton.Primario:
                    boton.BackColor = activo ? Primario : Deshabilitado;
                    boton.ForeColor = Color.White;
                    boton.FlatAppearance.MouseOverBackColor = PrimarioHover;
                    boton.FlatAppearance.MouseDownBackColor = PrimarioHover;
                    break;
                case EstiloBoton.Peligro:
                    boton.BackColor = activo ? Superficie : SuperficieAlterna;
                    boton.ForeColor = activo ? Peligro : Deshabilitado;
                    boton.FlatAppearance.BorderSize = 1;
                    boton.FlatAppearance.BorderColor = activo ? Color.FromArgb(254, 202, 202) : Borde;
                    boton.FlatAppearance.MouseOverBackColor = PeligroSuave;
                    boton.FlatAppearance.MouseDownBackColor = Color.FromArgb(254, 226, 226);
                    break;
                default:
                    boton.BackColor = activo ? Superficie : SuperficieAlterna;
                    boton.ForeColor = activo ? Texto : Deshabilitado;
                    boton.FlatAppearance.MouseOverBackColor = SuperficieAlterna;
                    boton.FlatAppearance.MouseDownBackColor = Borde;
                    break;
            }
        }

        Pintar();
        boton.EnabledChanged += (_, _) => Pintar();
    }

    /// <summary>Estilo de grilla limpio: sin bordes, filas altas, cabecera suave.</summary>
    public static void Grilla(DataGridView g)
    {
        g.BorderStyle = BorderStyle.None;
        g.BackgroundColor = Superficie;
        g.GridColor = Borde;
        g.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        g.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        g.EnableHeadersVisualStyles = false;
        g.RowHeadersVisible = false;
        g.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        g.MultiSelect = false;
        g.ReadOnly = true;
        g.AllowUserToAddRows = false;
        g.AllowUserToDeleteRows = false;
        g.AllowUserToResizeRows = false;
        g.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        g.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        g.ColumnHeadersHeight = 42;
        g.RowTemplate.Height = 36;

        g.ColumnHeadersDefaultCellStyle.BackColor = SuperficieAlterna;
        g.ColumnHeadersDefaultCellStyle.ForeColor = TextoSuave;
        g.ColumnHeadersDefaultCellStyle.Font = FuenteEtiqueta;
        g.ColumnHeadersDefaultCellStyle.SelectionBackColor = SuperficieAlterna;
        g.ColumnHeadersDefaultCellStyle.Padding = new Padding(10, 0, 10, 0);

        g.DefaultCellStyle.BackColor = Superficie;
        g.DefaultCellStyle.ForeColor = Texto;
        g.DefaultCellStyle.Font = Fuente;
        g.DefaultCellStyle.SelectionBackColor = PrimarioSuave;
        g.DefaultCellStyle.SelectionForeColor = Texto;
        g.DefaultCellStyle.Padding = new Padding(10, 0, 10, 0);
        g.AlternatingRowsDefaultCellStyle.BackColor = SuperficieAlterna;

        // Evita el parpadeo al desplazarse.
        typeof(DataGridView)
            .GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)?
            .SetValue(g, true);
    }

    /// <summary>Estilo para cajas de texto y combos de los formularios de edicion.</summary>
    public static void Entrada(Control c)
    {
        c.Font = Fuente;
        c.ForeColor = Texto;
        switch (c)
        {
            case TextBox t:
                t.BorderStyle = BorderStyle.FixedSingle;
                t.BackColor = Superficie;
                break;
            case ComboBox cb:
                cb.FlatStyle = FlatStyle.System;
                break;
        }
    }
}
