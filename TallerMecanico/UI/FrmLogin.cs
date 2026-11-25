using System.ComponentModel;
using TallerMecanico.Datos;
using TallerMecanico.Servicios;

namespace TallerMecanico.UI;

/// <summary>
/// Pantalla de ingreso. Si el correo y la clave son correctos deja el usuario en
/// Sesion.Actual y cierra con DialogResult.OK.
/// </summary>
[DesignerCategory("Code")]
public sealed class FrmLogin : Form
{
    private const int IntentosMaximos = 5;
    private const int SegundosBloqueo = 30;

    private readonly AutenticacionService _servicio = new();
    private readonly TextBox txtCorreo = new() { MaxLength = 120 };
    private readonly TextBox txtClave = new() { MaxLength = 100, UseSystemPasswordChar = true };
    private readonly CheckBox chkMostrar = new() { Text = "Mostrar clave", AutoSize = true };
    private readonly Button btnIngresar = new() { Text = "Ingresar" };
    private readonly Label lblError = new();
    private readonly Label lblAyuda = new();
    private readonly Label lblConexion = new();
    private readonly System.Windows.Forms.Timer tmrBloqueo = new() { Interval = 1000 };
    private bool _conectado;
    private int _fallidos;
    private int _segundosRestantes;

    public FrmLogin(string? correoInicial = null)
    {
        Text = "Taller Mecánico — Iniciar sesión";
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(Lienzo.Px(820), Lienzo.Px(500));
        BackColor = Tema.Superficie;
        Font = Tema.Fuente;
        AcceptButton = btnIngresar;

        Controls.Add(ConstruirFormulario());
        Controls.Add(ConstruirMarca());

        txtCorreo.Text = correoInicial ?? "";
        chkMostrar.CheckedChanged += (_, _) => txtClave.UseSystemPasswordChar = !chkMostrar.Checked;
        btnIngresar.Click += (_, _) => Ingresar();
        txtCorreo.TextChanged += (_, _) => lblError.Visible = false;
        txtClave.TextChanged += (_, _) => lblError.Visible = false;
        tmrBloqueo.Tick += (_, _) => AvanzarBloqueo();
        Shown += (_, _) => VerificarConexion();
    }

    #region Diseno

    private Panel ConstruirMarca()
    {
        var pnl = new Panel { Dock = DockStyle.Left, Width = Lienzo.Px(320), BackColor = Tema.Lateral };
        pnl.Paint += (_, e) =>
        {
            var g = e.Graphics;
            int x = Lienzo.Px(40);
            Lienzo.Caja(g, new Rectangle(x, Lienzo.Px(130), Lienzo.Px(64), Lienzo.Px(64)), Lienzo.Px(14), Tema.Primario);
            Lienzo.Icono(g, Iconos.Llave, 28, Color.White,
                new Rectangle(x, Lienzo.Px(130), Lienzo.Px(64), Lienzo.Px(64)));
            Lienzo.Texto(g, "Taller Mecánico", Lienzo.Fuente(26, true), Color.White,
                new Rectangle(x, Lienzo.Px(215), pnl.Width - x, Lienzo.Px(40)));
            Lienzo.Texto(g, "Sistema de gestión", Lienzo.Fuente(15), Tema.LateralTexto,
                new Rectangle(x, Lienzo.Px(255), pnl.Width - x, Lienzo.Px(26)));
            Lienzo.Texto(g, "Vehículos · Ventas · Personal", Lienzo.Fuente(12.5f), Tema.LateralTextoSuave,
                new Rectangle(x, pnl.Height - Lienzo.Px(60), pnl.Width - x, Lienzo.Px(24)));
        };
        return pnl;
    }

    private Panel ConstruirFormulario()
    {
        var pnl = new Panel { Dock = DockStyle.Fill, BackColor = Tema.Superficie };
        int x = Lienzo.Px(70), ancho = Lienzo.Px(360), y = Lienzo.Px(70);

        Label Etiqueta(string texto, Font fuente, Color color, int alto)
        {
            var l = new Label { Text = texto, Font = fuente, ForeColor = color, AutoSize = false,
                                Location = new Point(x, y), Size = new Size(ancho, alto) };
            pnl.Controls.Add(l);
            y += alto;
            return l;
        }

        Etiqueta("Iniciar sesión", Lienzo.Fuente(26, true), Tema.Texto, Lienzo.Px(44));
        Etiqueta("Ingresá con tu correo y clave de empleado.", Lienzo.Fuente(13.5f), Tema.TextoSuave, Lienzo.Px(40));

        Etiqueta("Correo", Tema.FuenteEtiqueta, Tema.TextoSuave, Lienzo.Px(24));
        Entrada(pnl, txtCorreo, x, ref y, ancho);
        y += Lienzo.Px(14);
        Etiqueta("Clave", Tema.FuenteEtiqueta, Tema.TextoSuave, Lienzo.Px(24));
        Entrada(pnl, txtClave, x, ref y, ancho);

        chkMostrar.Location = new Point(x, y + Lienzo.Px(8));
        chkMostrar.ForeColor = Tema.TextoSuave;
        chkMostrar.Cursor = Cursors.Hand;
        pnl.Controls.Add(chkMostrar);
        y += Lienzo.Px(40);

        lblError.Location = new Point(x, y);
        lblError.Size = new Size(ancho, Lienzo.Px(34));
        lblError.BackColor = Tema.PeligroSuave;
        lblError.ForeColor = Tema.Peligro;
        lblError.TextAlign = ContentAlignment.MiddleLeft;
        lblError.Padding = new Padding(Lienzo.Px(8), 0, 0, 0);
        lblError.Visible = false;
        pnl.Controls.Add(lblError);
        y += Lienzo.Px(42);

        btnIngresar.Location = new Point(x, y);
        btnIngresar.Size = new Size(ancho, Lienzo.Px(44));
        Tema.Boton(btnIngresar, Tema.EstiloBoton.Primario);
        btnIngresar.Font = Lienzo.Fuente(15, true);
        pnl.Controls.Add(btnIngresar);
        y += Lienzo.Px(56);

        lblAyuda.Location = new Point(x, y);
        lblAyuda.Size = new Size(ancho, Lienzo.Px(40));
        lblAyuda.ForeColor = Tema.Primario;
        lblAyuda.Font = Tema.FuenteChica;
        pnl.Controls.Add(lblAyuda);

        lblConexion.Dock = DockStyle.Bottom;
        lblConexion.Height = Lienzo.Px(30);
        lblConexion.Padding = new Padding(x - Lienzo.Px(4), 0, 0, Lienzo.Px(6));
        lblConexion.TextAlign = ContentAlignment.MiddleLeft;
        lblConexion.Font = Tema.FuenteChica;
        lblConexion.Cursor = Cursors.Hand;
        lblConexion.Click += (_, _) => VerificarConexion();
        pnl.Controls.Add(lblConexion);
        return pnl;
    }

    private static void Entrada(Panel pnl, TextBox txt, int x, ref int y, int ancho)
    {
        Tema.Entrada(txt);
        txt.Font = Lienzo.Fuente(15);
        txt.Location = new Point(x, y);
        txt.Width = ancho;
        pnl.Controls.Add(txt);
        y += txt.Height;
    }

    #endregion

    #region Ingreso

    private void VerificarConexion()
    {
        _conectado = Db.ProbarConexion(out var error);
        lblConexion.Text = _conectado ? "●  Conectado a la base" : "●  Sin conexión a la base (clic para reintentar)";
        lblConexion.ForeColor = _conectado ? Tema.Exito : Tema.Peligro;
        if (!_conectado)
        {
            MostrarError(error);
            return;
        }

        lblError.Visible = false;
        try
        {
            if (_servicio.AsegurarAdministrador())
                lblAyuda.Text = "Primer ingreso: se creó el usuario administrador\n" +
                                $"{AutenticacionService.CorreoAdminInicial}  /  clave {AutenticacionService.ClaveAdminInicial}  (cambiala después).";
        }
        catch (Exception ex)
        {
            MostrarError(Db.TraducirError(ex));
        }
        (txtCorreo.Text.Length == 0 ? txtCorreo : txtClave).Focus();
    }

    private void Ingresar()
    {
        if (_segundosRestantes > 0) return;
        if (!_conectado)
        {
            VerificarConexion();
            if (!_conectado) return;
        }
        if (txtCorreo.Text.Trim().Length == 0) { MostrarError("Ingresá tu correo."); txtCorreo.Focus(); return; }
        if (txtClave.Text.Length == 0) { MostrarError("Ingresá tu clave."); txtClave.Focus(); return; }

        try
        {
            UseWaitCursor = true;
            Sesion.Iniciar(_servicio.Ingresar(txtCorreo.Text, txtClave.Text));
            DialogResult = DialogResult.OK;
        }
        catch (InvalidOperationException ex)
        {
            _fallidos++;
            txtClave.Clear();
            txtClave.Focus();
            if (_fallidos >= IntentosMaximos) IniciarBloqueo();
            else MostrarError(ex.Message + (_fallidos >= 3 ? $"  (quedan {IntentosMaximos - _fallidos} intentos)" : ""));
        }
        catch (Exception ex)
        {
            MostrarError(Db.TraducirError(ex));
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    /// <summary>Despues de varios intentos fallidos se espera un rato antes de dejar probar de nuevo.</summary>
    private void IniciarBloqueo()
    {
        _segundosRestantes = SegundosBloqueo;
        btnIngresar.Enabled = false;
        AvanzarBloqueo();
        tmrBloqueo.Start();
    }

    private void AvanzarBloqueo()
    {
        if (_segundosRestantes <= 0)
        {
            tmrBloqueo.Stop();
            _fallidos = 0;
            btnIngresar.Enabled = true;
            lblError.Visible = false;
            return;
        }
        MostrarError($"Demasiados intentos. Esperá {_segundosRestantes} s para volver a probar.");
        _segundosRestantes--;
    }

    private void MostrarError(string mensaje)
    {
        lblError.Text = "⚠  " + mensaje;
        lblError.Visible = true;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) tmrBloqueo.Dispose();
        base.Dispose(disposing);
    }

    #endregion
}
