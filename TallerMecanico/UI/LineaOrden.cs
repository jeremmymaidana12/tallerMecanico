using System.ComponentModel;
using System.Globalization;
using TallerMecanico.Modelos;

namespace TallerMecanico.UI;

/// <summary>
/// Fila editable del detalle de una orden de trabajo (mismo estilo que LineaVenta).
/// Articulo: nombre, cantidad, precio y quitar. Servicio: nombre, precio, quitar y una observacion debajo.
/// </summary>
[DesignerCategory("Code")]
public sealed class LineaOrden : PanelDibujo
{
    private const int Maximo = 999_999_999;
    private readonly CajaTexto _precio = new(new TextBox { TextAlign = HorizontalAlignment.Right, MaxLength = 16 }, false);
    private readonly TextBox? _cantidad;
    private readonly CajaTexto? _observacion;

    public OrdenTrabajoArticulo? Articulo { get; }
    public OrdenTrabajoServicio? Servicio { get; }
    public event Action? Cambio;
    public event Action<LineaOrden>? Quitar;

    public LineaOrden(OrdenTrabajoArticulo articulo) : this()
    {
        Articulo = articulo;
        Height = Lienzo.Px(46.5f);
        _cantidad = new TextBox
        {
            TextAlign = HorizontalAlignment.Center, BorderStyle = BorderStyle.None, MaxLength = 9, Font = Lienzo.Fuente(14),
            Text = Lienzo.Numero(articulo.Cantidad)
        };
        var menos = Boton(Iconos.Menos);
        var mas = Boton(Iconos.Agregar);
        menos.Click += (_, _) => CambiarCantidad(articulo.Cantidad - 1);
        mas.Click += (_, _) => CambiarCantidad(articulo.Cantidad + 1);
        _cantidad.TextChanged += (_, _) => Confirmar();
        _cantidad.Leave += (_, _) => Normalizar();
        _precio.Entrada.Text = Lienzo.Numero(articulo.Precio);
        Controls.AddRange([menos, _cantidad, mas]);

        Layout += (_, _) =>
        {
            int y = Lienzo.Px(8), alto = Lienzo.Px(30), b = Lienzo.Px(28), x = Width - Lienzo.Px(218);
            menos.SetBounds(x + 1, y + 1, b, alto - 2);
            _cantidad.SetBounds(x + b + 3, y + (alto - _cantidad.Height) / 2, Lienzo.Px(27), _cantidad.Height);
            mas.SetBounds(x + Lienzo.Px(86) - b - 1, y + 1, b, alto - 2);
        };
    }

    public LineaOrden(OrdenTrabajoServicio servicio) : this()
    {
        Servicio = servicio;
        Height = Lienzo.Px(82);
        _observacion = new CajaTexto(new TextBox { PlaceholderText = "Observación (opcional)...", MaxLength = 500 }, false, 13);
        _observacion.Entrada.Text = servicio.Observacion ?? "";
        _observacion.Entrada.TextChanged += (_, _) => servicio.Observacion = _observacion.Entrada.Text;
        _precio.Entrada.Text = Lienzo.Numero(servicio.Precio);
        Controls.Add(_observacion);

        Layout += (_, _) => _observacion.SetBounds(0, Lienzo.Px(44), Width - Lienzo.Px(41), Lienzo.Px(28));
    }

    private LineaOrden()
    {
        BackColor = Tema.Superficie;
        Margin = Padding.Empty;

        var borrar = new BotonRedondo
        {
            Icono = Iconos.Borrar, TamanoIcono = 15, ForeColor = Tema.Peligro,
            Fondo = Color.FromArgb(254, 226, 226), FondoHover = Color.FromArgb(254, 202, 202),
            ColorBorde = Color.FromArgb(252, 165, 165)
        };
        borrar.Click += (_, _) => Quitar?.Invoke(this);
        _precio.Entrada.TextChanged += (_, _) => Confirmar();
        _precio.Entrada.Leave += (_, _) => Normalizar();
        Controls.AddRange([_precio, borrar]);

        Layout += (_, _) =>
        {
            int y = Lienzo.Px(8), alto = Lienzo.Px(30);
            borrar.SetBounds(Width - Lienzo.Px(33), y, Lienzo.Px(33), alto);
            _precio.SetBounds(Width - Lienzo.Px(124), y, Lienzo.Px(83), alto);
        };
    }

    /// <summary>Suma unidades (al elegir otra vez el mismo articulo).</summary>
    public void Sumar(int cantidad)
    {
        if (Articulo is not null) CambiarCantidad(Articulo.Cantidad + cantidad);
    }

    /// <summary>True si lo escrito en cantidad y precio es valido.</summary>
    public bool EsValida => LeerPrecio() is not null && (_cantidad is null || LeerCantidad() is not null);

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(BackColor);
        int fin = Width - Lienzo.Px(132);
        if (_cantidad is not null)
        {
            fin = Width - Lienzo.Px(218);
            Lienzo.Caja(g, new Rectangle(fin, Lienzo.Px(8), Lienzo.Px(86), Lienzo.Px(30)), Lienzo.Px(6), Tema.Superficie, Lienzo.Borde);
        }
        var nombre = Articulo?.Articulo ?? Servicio?.Servicio ?? "";
        Lienzo.Texto(g, nombre, Lienzo.Fuente(14), Tema.Texto, new Rectangle(0, 0, fin - Lienzo.Px(8), Lienzo.Px(46)));
        using var lapiz = new Pen(Tema.Borde);
        g.DrawLine(lapiz, 0, Height - 1, Width, Height - 1);
    }

    private static BotonRedondo Boton(string icono) => new()
    {
        Icono = icono, TamanoIcono = 11, Radio = 5, ColorBorde = null, TabStop = false
    };

    private void CambiarCantidad(int cantidad)
    {
        if (_cantidad is null || cantidad is < 1 or > Maximo) return;
        _cantidad.Text = Lienzo.Numero(cantidad);
    }

    private int? LeerCantidad() =>
        _cantidad is not null
        && int.TryParse(_cantidad.Text.Replace(".", ""), NumberStyles.None, CultureInfo.InvariantCulture, out var n)
        && n is > 0 and <= Maximo
            ? n : null;

    private decimal? LeerPrecio() =>
        decimal.TryParse(_precio.Entrada.Text.Replace(".", "").Trim(), NumberStyles.AllowDecimalPoint, Lienzo.Cultura, out var p)
        && p <= 9_999_999_999m
            ? Math.Round(p, 2) : null;

    private void Confirmar()
    {
        if (_cantidad is not null) _cantidad.ForeColor = LeerCantidad() is null ? Tema.Peligro : Tema.Texto;
        _precio.Entrada.ForeColor = LeerPrecio() is null ? Tema.Peligro : Tema.Texto;
        if (!EsValida) return;
        if (Articulo is not null)
        {
            Articulo.Cantidad = LeerCantidad()!.Value;
            Articulo.Precio = LeerPrecio()!.Value;
        }
        if (Servicio is not null) Servicio.Precio = LeerPrecio()!.Value;
        Cambio?.Invoke();
    }

    /// <summary>Al salir del campo se vuelve al ultimo valor valido, con formato.</summary>
    private void Normalizar()
    {
        if (_cantidad is not null && Articulo is not null && LeerCantidad() is null)
            _cantidad.Text = Lienzo.Numero(Articulo.Cantidad);
        _precio.Entrada.Text = Lienzo.Numero(LeerPrecio() ?? Articulo?.Precio ?? Servicio?.Precio ?? 0);
    }
}
