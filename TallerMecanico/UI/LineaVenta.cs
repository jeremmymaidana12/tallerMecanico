using System.ComponentModel;
using System.Globalization;
using TallerMecanico.Modelos;

namespace TallerMecanico.UI;

/// <summary>Fila editable del detalle de una venta: articulo, cantidad, precio y quitar.</summary>
[DesignerCategory("Code")]
public sealed class LineaVenta : PanelDibujo
{
    private const int Maximo = 999_999_999;
    private readonly CajaTexto _precio = new(new TextBox { TextAlign = HorizontalAlignment.Right, MaxLength = 16 }, false);
    private readonly TextBox _cantidad = new()
    {
        TextAlign = HorizontalAlignment.Center, BorderStyle = BorderStyle.None, MaxLength = 9, Font = Lienzo.Fuente(14)
    };
    private bool _exenta;

    public VentaDetalle Detalle { get; }
    public event Action? Cambio;
    public event Action<LineaVenta>? Quitar;

    public LineaVenta(VentaDetalle detalle)
    {
        Detalle = detalle;
        _exenta = detalle.Excentas > 0;
        BackColor = Tema.Superficie;
        Height = Lienzo.Px(46.5f);
        Margin = Padding.Empty;

        var menos = Boton(Iconos.Menos);
        var mas = Boton(Iconos.Agregar);
        var borrar = new BotonRedondo
        {
            Icono = Iconos.Borrar, TamanoIcono = 15, ForeColor = Tema.Peligro,
            Fondo = Color.FromArgb(254, 226, 226), FondoHover = Color.FromArgb(254, 202, 202),
            ColorBorde = Color.FromArgb(252, 165, 165)
        };
        menos.Click += (_, _) => CambiarCantidad(Detalle.Cantidad - 1);
        mas.Click += (_, _) => CambiarCantidad(Detalle.Cantidad + 1);
        borrar.Click += (_, _) => Quitar?.Invoke(this);

        _cantidad.Text = Lienzo.Numero(detalle.Cantidad);
        _precio.Entrada.Text = Lienzo.Numero(detalle.Precio);
        _cantidad.TextChanged += (_, _) => Confirmar();
        _precio.Entrada.TextChanged += (_, _) => Confirmar();
        _cantidad.Leave += (_, _) => Normalizar();
        _precio.Entrada.Leave += (_, _) => Normalizar();

        Controls.AddRange([menos, _cantidad, mas, _precio, borrar]);
        Layout += (_, _) =>
        {
            int y = Lienzo.Px(8), alto = Lienzo.Px(30), b = Lienzo.Px(28);
            borrar.SetBounds(Width - Lienzo.Px(33), y, Lienzo.Px(33), alto);
            _precio.SetBounds(Width - Lienzo.Px(124), y, Lienzo.Px(83), alto);
            int x = Width - Lienzo.Px(218);
            menos.SetBounds(x + 1, y + 1, b, alto - 2);
            _cantidad.SetBounds(x + b + 3, y + (alto - _cantidad.Height) / 2, Lienzo.Px(27), _cantidad.Height);
            mas.SetBounds(x + Lienzo.Px(86) - b - 1, y + 1, b, alto - 2);
        };

        var menu = new ContextMenuStrip();
        var exenta = new ToolStripMenuItem("Exenta de IVA") { CheckOnClick = true, Checked = _exenta };
        exenta.CheckedChanged += (_, _) => { _exenta = exenta.Checked; Recalcular(); Invalidate(); };
        menu.Items.Add(exenta);
        ContextMenuStrip = menu;
        Disposed += (_, _) => menu.Dispose();
    }

    /// <summary>Suma unidades (al elegir otra vez el mismo articulo).</summary>
    public void Sumar(decimal cantidad) => CambiarCantidad(Detalle.Cantidad + cantidad);

    /// <summary>True si lo escrito en cantidad y precio es valido.</summary>
    public bool EsValida => LeerCantidad() is not null && LeerPrecio() is not null;

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(BackColor);
        int x = Width - Lienzo.Px(218);
        Lienzo.Caja(g, new Rectangle(x, Lienzo.Px(8), Lienzo.Px(86), Lienzo.Px(30)), Lienzo.Px(6), Tema.Superficie, Lienzo.Borde);
        Lienzo.Texto(g, Detalle.Articulo + (_exenta ? " (exenta)" : ""), Lienzo.Fuente(14), Tema.Texto,
            new Rectangle(0, 0, x - Lienzo.Px(8), Height));
        using var lapiz = new Pen(Tema.Borde);
        g.DrawLine(lapiz, 0, Height - 1, Width, Height - 1);
    }

    private static BotonRedondo Boton(string icono) => new()
    {
        Icono = icono, TamanoIcono = 11, Radio = 5, ColorBorde = null, TabStop = false
    };

    private void CambiarCantidad(decimal cantidad)
    {
        if (cantidad is < 1 or > Maximo) return;
        _cantidad.Text = Lienzo.Numero(cantidad);
    }

    private decimal? LeerCantidad() =>
        int.TryParse(_cantidad.Text.Replace(".", ""), NumberStyles.None, CultureInfo.InvariantCulture, out var n) && n is > 0 and <= Maximo
            ? n : null;

    private decimal? LeerPrecio() =>
        decimal.TryParse(_precio.Entrada.Text.Replace(".", "").Trim(), NumberStyles.AllowDecimalPoint, Lienzo.Cultura, out var p)
        && p <= 9_999_999_999m
            ? Math.Round(p, 2) : null;

    private void Confirmar()
    {
        _cantidad.ForeColor = LeerCantidad() is null ? Tema.Peligro : Tema.Texto;
        _precio.Entrada.ForeColor = LeerPrecio() is null ? Tema.Peligro : Tema.Texto;
        if (!EsValida) return;
        Detalle.Cantidad = LeerCantidad()!.Value;
        Detalle.Precio = LeerPrecio()!.Value;
        Recalcular();
    }

    /// <summary>Al salir del campo se vuelve al ultimo valor valido, con formato.</summary>
    private void Normalizar()
    {
        if (LeerCantidad() is null) _cantidad.Text = Lienzo.Numero(Detalle.Cantidad);
        _precio.Entrada.Text = Lienzo.Numero(LeerPrecio() ?? Detalle.Precio);
    }

    private void Recalcular()
    {
        Detalle.Calcular(_exenta);
        Cambio?.Invoke();
    }
}
