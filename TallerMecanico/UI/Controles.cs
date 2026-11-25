using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Globalization;

namespace TallerMecanico.UI;

/// <summary>Iconos de la fuente Segoe MDL2 Assets (incluida en Windows 10 y 11).</summary>
public static class Iconos
{
    public const string Buscar = "", Agregar = "", Exportar = "", Refrescar = "",
        Cerrar = "", Tilde = "", Ver = "", Imprimir = "", Borrar = "",
        Llave = "", Auto = "", Etiqueta = "", Paquete = "", Tarjeta = "",
        Persona = "", Edificio = "", Ubicacion = "", Recibo = "", Menos = "";
    /// <summary>Hamburguesa, clientes, lista (categorias) y capas (modelos).</summary>
    public const string Menu = "", Clientes = "", Lista = "", Capas = "", Salir = "";
    /// <summary>No existe en la fuente: lo dibuja Lienzo.Icono.</summary>
    public const string Filtro = "filtro", Puntos = "puntos";
}

/// <summary>Medidas, formatos y dibujo comun de las vistas modernas.</summary>
public static class Lienzo
{
    public static readonly Color Azul = Color.FromArgb(26, 86, 219);
    public static readonly Color AzulHover = Color.FromArgb(30, 66, 175);
    public static readonly Color Borde = Color.FromArgb(203, 213, 225);
    public static readonly Color Gris = Color.FromArgb(241, 245, 249);

    private static readonly float Escala = ObtenerEscala();
    private static readonly Dictionary<(string, float, bool), Font> Fuentes = [];

    /// <summary>Guaranies: punto para miles y coma para decimales, sin depender de Windows.</summary>
    public static readonly CultureInfo Cultura = CrearCultura();

    /// <summary>Convierte pixeles del diseno (100%) a la escala de la pantalla.</summary>
    public static int Px(float v) => (int)Math.Round(v * Escala);

    public static Font Fuente(float px, bool negrita = false) =>
        Cache(negrita ? "Segoe UI Semibold" : "Segoe UI", px);

    public static string Numero(decimal v) =>
        v.ToString(v == decimal.Truncate(v) ? "N0" : "N2", Cultura);

    public static string Gs(decimal v) => "Gs. " + v.ToString("N0", Cultura);

    public static GraphicsPath Redondeado(RectangleF r, float radio)
    {
        var d = Math.Max(1, Math.Min(radio * 2, Math.Min(r.Width, r.Height)));
        var p = new GraphicsPath();
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }

    public static void Caja(Graphics g, Rectangle r, float radio, Color fondo, Color? borde = null, float grosor = 1)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var camino = Redondeado(new RectangleF(r.X + .5f, r.Y + .5f, r.Width - 1.5f, r.Height - 1.5f), radio);
        using var brocha = new SolidBrush(fondo);
        g.FillPath(brocha, camino);
        if (borde is not { } color) return;
        using var lapiz = new Pen(color, grosor);
        g.DrawPath(lapiz, camino);
    }

    public static void Texto(Graphics g, string texto, Font fuente, Color color, Rectangle r,
                             TextFormatFlags alineacion = TextFormatFlags.Left) =>
        TextRenderer.DrawText(g, texto, fuente, r, color, alineacion | TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);

    public static int Ancho(string texto, Font fuente) =>
        TextRenderer.MeasureText(texto, fuente, Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix).Width;

    /// <summary>Dibuja un icono centrado en el rectangulo.</summary>
    public static void Icono(Graphics g, string icono, float px, Color color, Rectangle r)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        float cx = r.X + r.Width / 2f, cy = r.Y + r.Height / 2f;
        switch (icono)
        {
            case Iconos.Filtro:
                using (var lapiz = new Pen(color, Px(1.5f)))
                {
                    for (int i = 0; i < 3; i++)
                    {
                        float mitad = Px(7 - i * 2.5f), y = cy + Px((i - 1) * 4);
                        g.DrawLine(lapiz, cx - mitad, y, cx + mitad, y);
                    }
                }
                break;
            case Iconos.Puntos:
                using (var brocha = new SolidBrush(color))
                {
                    float d = Px(3);
                    for (int i = -1; i <= 1; i++)
                        g.FillEllipse(brocha, cx - d / 2, cy + i * Px(6) - d / 2, d, d);
                }
                break;
            default:
                TextRenderer.DrawText(g, icono, Cache("Segoe MDL2 Assets", px), r, color,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                break;
        }
    }

    private static Font Cache(string familia, float px)
    {
        var clave = (familia, px, false);
        if (!Fuentes.TryGetValue(clave, out var fuente))
            Fuentes[clave] = fuente = new Font(familia, px * Escala, GraphicsUnit.Pixel);
        return fuente;
    }

    private static float ObtenerEscala()
    {
        using var g = Graphics.FromHwnd(IntPtr.Zero);
        return g.DpiX / 96f;
    }

    private static CultureInfo CrearCultura()
    {
        var c = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        c.NumberFormat.NumberGroupSeparator = ".";
        c.NumberFormat.NumberDecimalSeparator = ",";
        return c;
    }
}

/// <summary>Boton redondeado con icono opcional.</summary>
[DesignerCategory("Code")]
public sealed class BotonRedondo : Button
{
    private bool _encima;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Icono { get; set; } = "";
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public float TamanoIcono { get; set; } = 14;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public float Radio { get; set; } = 6;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color Fondo { get; set; } = Color.FromArgb(241, 245, 249);
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color FondoHover { get; set; } = Color.FromArgb(226, 232, 240);
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color? ColorBorde { get; set; } = Lienzo.Borde;

    public BotonRedondo()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Cursor = Cursors.Hand;
        ForeColor = Tema.Texto;
        Font = Lienzo.Fuente(14);
    }

    public BotonRedondo Primario()
    {
        Fondo = Lienzo.Azul;
        FondoHover = Lienzo.AzulHover;
        ColorBorde = null;
        ForeColor = Color.White;
        return this;
    }

    protected override void OnMouseEnter(EventArgs e) { _encima = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _encima = false; Invalidate(); base.OnMouseLeave(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(Parent?.BackColor ?? Tema.Superficie);
        Lienzo.Caja(g, ClientRectangle, Lienzo.Px(Radio), _encima ? FondoHover : Fondo,
            Focused && ShowFocusCues ? Lienzo.Azul : ColorBorde);

        int anchoTexto = Text.Length == 0 ? 0 : Lienzo.Ancho(Text, Font);
        int anchoIcono = Icono.Length == 0 ? 0 : Lienzo.Px(TamanoIcono + 2);
        int separacion = anchoTexto > 0 && anchoIcono > 0 ? Lienzo.Px(7) : 0;
        int x = (Width - anchoTexto - anchoIcono - separacion) / 2;
        if (anchoIcono > 0)
            Lienzo.Icono(g, Icono, TamanoIcono, ForeColor, new Rectangle(x, 0, anchoIcono, Height));
        if (anchoTexto > 0)
            Lienzo.Texto(g, Text, Font, ForeColor, new Rectangle(x + anchoIcono + separacion, 0, anchoTexto + 2, Height));
    }
}

/// <summary>Borde redondeado alrededor de un TextBox, con icono de busqueda opcional.</summary>
[DesignerCategory("Code")]
public sealed class CajaTexto : Panel
{
    private readonly bool _icono;
    public TextBox Entrada { get; }

    public CajaTexto(TextBox entrada, bool icono, float px = 14)
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        _icono = icono;
        Entrada = entrada;
        entrada.BorderStyle = BorderStyle.None;
        entrada.Dock = DockStyle.None;
        entrada.Anchor = AnchorStyles.None;
        entrada.Font = Lienzo.Fuente(px);
        entrada.BackColor = Tema.Superficie;
        Controls.Add(entrada);
        Cursor = Cursors.IBeam;
        Click += (_, _) => entrada.Focus();
        entrada.GotFocus += (_, _) => Invalidate();
        entrada.LostFocus += (_, _) => Invalidate();
    }

    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);
        int izquierda = Lienzo.Px(_icono ? 32 : 7), derecha = Lienzo.Px(7);
        Entrada.SetBounds(izquierda, (Height - Entrada.Height) / 2, Math.Max(10, Width - izquierda - derecha), Entrada.Height);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(Parent?.BackColor ?? Tema.Superficie);
        Lienzo.Caja(e.Graphics, ClientRectangle, Lienzo.Px(6), Tema.Superficie,
            Entrada.Focused ? Lienzo.Azul : Color.FromArgb(148, 163, 184), Entrada.Focused ? Lienzo.Px(1.5f) : 1);
        if (_icono)
            Lienzo.Icono(e.Graphics, Iconos.Buscar, 15, Tema.TextoSuave, new Rectangle(Lienzo.Px(8), 0, Lienzo.Px(18), Height));
    }
}

/// <summary>Panel sin parpadeo que se dibuja a mano con el evento Paint.</summary>
[DesignerCategory("Code")]
public class PanelDibujo : Panel
{
    public PanelDibujo() =>
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw, true);
}
