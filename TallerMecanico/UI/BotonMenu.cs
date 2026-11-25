using System.ComponentModel;

namespace TallerMecanico.UI;

/// <summary>
/// Boton del menu lateral. Al pasar el cursor se anima: se aclara el fondo,
/// aparece una barra azul a la izquierda y el icono y el texto se corren un poco.
/// Colapsado (menu hamburguesa cerrado) muestra solo el icono.
/// </summary>
[DesignerCategory("Code")]
public sealed class BotonMenu : Button
{
    private const float Paso = 0.18f;          // velocidad de la animacion por tick
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 15 };
    private float _progreso;                   // 0 = reposo, 1 = cursor encima
    private bool _encima;
    private bool _activo;
    private bool _colapsado;

    public string Icono { get; }

    public BotonMenu(string icono, string texto)
    {
        Icono = icono;
        Text = texto;
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Cursor = Cursors.Hand;
        TabStop = false;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);

        _timer.Tick += (_, _) =>
        {
            _progreso = Math.Clamp(_progreso + (_encima ? Paso : -Paso), 0, 1);
            if (_progreso is 0 or 1) _timer.Stop();
            Invalidate();
        };
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Activo
    {
        get => _activo;
        set { _activo = value; Invalidate(); }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Colapsado
    {
        get => _colapsado;
        set { _colapsado = value; Invalidate(); }
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        _encima = true;
        _timer.Start();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _encima = false;
        _timer.Start();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(Tema.Lateral);

        float p = _activo ? 1 : _progreso;
        var fondo = _activo
            ? Mezclar(Tema.Primario, Tema.PrimarioHover, _progreso)
            : Mezclar(Tema.Lateral, Tema.LateralHover, _progreso);
        var rect = new Rectangle(0, 0, Width, Height);
        if (_activo || _progreso > 0)
            Lienzo.Caja(g, rect, Lienzo.Px(6), fondo);

        // Barra de acento a la izquierda (crece con el hover).
        if (!_activo && _progreso > 0)
        {
            int alto = (int)(Height * 0.55f * _progreso);
            using var barra = new SolidBrush(Tema.Primario);
            g.FillRectangle(barra, 0, (Height - alto) / 2, Lienzo.Px(3), alto);
        }

        var color = _activo ? Color.White : Mezclar(Tema.LateralTexto, Color.White, p);
        int corrimiento = _activo || _colapsado ? 0 : (int)(Lienzo.Px(4) * _progreso);

        if (_colapsado)
        {
            Lienzo.Icono(g, Icono, 16, color, rect);
            return;
        }

        Lienzo.Icono(g, Icono, 15, color,
            new Rectangle(Lienzo.Px(10) + corrimiento, 0, Lienzo.Px(20), Height));
        Lienzo.Texto(g, Text, Lienzo.Fuente(13.5f, _activo), color,
            new Rectangle(Lienzo.Px(40) + corrimiento, 0, Width - Lienzo.Px(44), Height));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _timer.Dispose();
        base.Dispose(disposing);
    }

    private static Color Mezclar(Color a, Color b, float t) => Color.FromArgb(
        (int)(a.R + (b.R - a.R) * t),
        (int)(a.G + (b.G - a.G) * t),
        (int)(a.B + (b.B - a.B) * t));
}
