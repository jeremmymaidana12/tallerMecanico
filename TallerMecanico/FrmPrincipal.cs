using System.ComponentModel;
using TallerMecanico.Datos;
using TallerMecanico.Formularios;
using TallerMecanico.UI;

namespace TallerMecanico;

/// <summary>
/// Ventana principal: menu lateral + area donde se abren los ABM.
/// Para sumar un modulo nuevo, agregar una linea en RegistrarModulos().
/// </summary>
[DesignerCategory("Code")]
public sealed class FrmPrincipal : Form
{
    private readonly Panel pnlLateral = new();
    private readonly FlowLayoutPanel flpMenu = new();
    private readonly Panel pnlContenido = new();
    private readonly Label lblConexion = new();
    private Button? _botonActivo;
    private Form? _moduloActual;

    public FrmPrincipal()
    {
        Text = "Taller Mecánico — Sistema de gestión";
        StartPosition = FormStartPosition.CenterScreen;
        var pantalla = Screen.PrimaryScreen!.WorkingArea.Size;
        Size = new Size(Math.Min(Lienzo.Px(1423), pantalla.Width), Math.Min(Lienzo.Px(770), pantalla.Height));
        MinimumSize = new Size(Math.Min(Lienzo.Px(1180), pantalla.Width), Math.Min(Lienzo.Px(640), pantalla.Height));
        BackColor = Tema.Fondo;
        Font = Tema.Fuente;

        ConstruirLateral();
        pnlContenido.Dock = DockStyle.Fill;
        pnlContenido.BackColor = Tema.Fondo;
        Controls.Add(pnlContenido);
        Controls.Add(pnlLateral);

        RegistrarModulos();
    }

    /// <summary>
    /// Lista de modulos del menu. Cuando un modulo este listo, reemplazar
    /// "null" por la creacion de su formulario: () => new FrmMarcas()
    /// </summary>
    private void RegistrarModulos()
    {
        AgregarSeccion("TALLER");
        var vehiculos = AgregarModulo(Iconos.Auto, "Vehículos", () => new FrmVehiculos());

        AgregarSeccion("CATÁLOGOS");
        AgregarModulo(Iconos.Etiqueta, "Marcas", null);
        AgregarModulo(Iconos.Paquete, "Artículos", null);

        AgregarSeccion("PERSONAL");
        AgregarModulo(Iconos.Tarjeta, "Tipos de empleado", null);
        AgregarModulo(Iconos.Persona, "Empleados", null);

        AgregarSeccion("EMPRESA");
        AgregarModulo(Iconos.Edificio, "Empresas", null);
        AgregarModulo(Iconos.Ubicacion, "Sucursales", null);

        AgregarSeccion("VENTAS");
        AgregarModulo(Iconos.Recibo, "Ventas", () => new FrmVentas());

        Shown += (_, _) =>
        {
            if (VerificarConexion()) vehiculos.PerformClick();
            else MostrarAviso("Sin conexión a la base de datos",
                "Iniciá MySQL desde el panel de XAMPP e importá sql/taller_mecanico.sql.\n" +
                "Después hacé clic en el estado de conexión (abajo a la izquierda) para reintentar.");
        };
    }

    #region Construccion del menu

    private void ConstruirLateral()
    {
        pnlLateral.Dock = DockStyle.Left;
        pnlLateral.Width = Lienzo.Px(183);
        pnlLateral.BackColor = Tema.Lateral;

        var pnlMarca = new Panel { Dock = DockStyle.Top, Height = Lienzo.Px(70) };
        pnlMarca.Controls.Add(new Label
        {
            Text = "Sistema de gestión",
            ForeColor = Tema.LateralTextoSuave,
            Font = Lienzo.Fuente(13.5f),
            AutoSize = true,
            Location = new Point(Lienzo.Px(16), Lienzo.Px(38))
        });
        pnlMarca.Controls.Add(new Label
        {
            Text = "Taller Mecánico",
            ForeColor = Color.White,
            Font = Lienzo.Fuente(16.5f, true),
            AutoSize = true,
            Location = new Point(Lienzo.Px(43), Lienzo.Px(15))
        });
        pnlMarca.Paint += (_, e) => Lienzo.Icono(e.Graphics, Iconos.Llave, 20, Color.White,
            new Rectangle(Lienzo.Px(18), Lienzo.Px(14), Lienzo.Px(26), Lienzo.Px(26)));

        flpMenu.Dock = DockStyle.Fill;
        flpMenu.FlowDirection = FlowDirection.TopDown;
        flpMenu.WrapContents = false;
        flpMenu.AutoScroll = true;
        flpMenu.Padding = new Padding(Lienzo.Px(9), 0, 0, 0);

        lblConexion.Dock = DockStyle.Bottom;
        lblConexion.Height = Lienzo.Px(26);
        lblConexion.Padding = new Padding(Lienzo.Px(14), 0, 0, 0);
        lblConexion.TextAlign = ContentAlignment.MiddleLeft;
        lblConexion.Font = Lienzo.Fuente(10.5f);
        lblConexion.ForeColor = Tema.LateralTextoSuave;
        lblConexion.Cursor = Cursors.Hand;
        lblConexion.Text = "●  Verificando conexión...";
        lblConexion.Click += (_, _) => VerificarConexion();

        pnlLateral.Controls.Add(flpMenu);
        pnlLateral.Controls.Add(lblConexion);
        pnlLateral.Controls.Add(pnlMarca);
    }

    private void AgregarSeccion(string titulo)
    {
        flpMenu.Controls.Add(new Label
        {
            Text = titulo,
            AutoSize = false,
            Width = Lienzo.Px(165),
            Height = Lienzo.Px(24),
            TextAlign = ContentAlignment.BottomLeft,
            Padding = new Padding(Lienzo.Px(6), 0, 0, Lienzo.Px(2)),
            Margin = new Padding(0, Lienzo.Px(flpMenu.Controls.Count == 0 ? 4 : 10), 0, 0),
            Font = Lienzo.Fuente(11.5f),
            ForeColor = Tema.LateralTextoSuave
        });
    }

    private Button AgregarModulo(string icono, string texto, Func<Form>? crear)
    {
        var boton = new Button
        {
            Text = texto,
            Width = Lienzo.Px(163),
            Height = Lienzo.Px(30),
            Margin = new Padding(0, 0, 0, Lienzo.Px(1)),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(Lienzo.Px(34), 0, 0, 0),
            FlatStyle = FlatStyle.Flat,
            Font = Lienzo.Fuente(13.5f),
            ForeColor = Tema.LateralTexto,
            BackColor = Tema.Lateral,
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false
        };
        boton.FlatAppearance.BorderSize = 0;
        boton.FlatAppearance.MouseOverBackColor = Tema.LateralHover;
        boton.FlatAppearance.MouseDownBackColor = Tema.LateralHover;

        boton.Paint += (_, e) => Lienzo.Icono(e.Graphics, icono, 15, boton.ForeColor,
            new Rectangle(Lienzo.Px(10), 0, Lienzo.Px(20), boton.Height));

        boton.Click += (_, _) =>
        {
            MarcarActivo(boton);
            if (crear is null)
            {
                MostrarAviso(texto,
                    "Este módulo todavía está en desarrollo por otro integrante del equipo.");
                return;
            }
            AbrirModulo(crear());
        };

        flpMenu.Controls.Add(boton);
        return boton;
    }

    private void MarcarActivo(Button boton)
    {
        if (_botonActivo is not null)
        {
            _botonActivo.BackColor = Tema.Lateral;
            _botonActivo.ForeColor = Tema.LateralTexto;
            _botonActivo.Font = Lienzo.Fuente(13.5f);
        }
        _botonActivo = boton;
        boton.BackColor = Tema.Primario;
        boton.ForeColor = Color.White;
        boton.Font = Lienzo.Fuente(13.5f, true);
    }

    #endregion

    #region Contenido

    private void AbrirModulo(Form modulo)
    {
        LimpiarContenido();
        modulo.TopLevel = false;
        modulo.FormBorderStyle = FormBorderStyle.None;
        // Embebido no usa el tamano minimo de ventana suelta: si no, puede quedar
        // mas grande que el panel y cortarse.
        modulo.MinimumSize = Size.Empty;
        modulo.Dock = DockStyle.Fill;
        pnlContenido.Controls.Add(modulo);
        _moduloActual = modulo;
        modulo.Show();
        modulo.MinimumSize = Size.Empty;
        modulo.Bounds = pnlContenido.ClientRectangle;
        pnlContenido.PerformLayout();
    }

    private void MostrarAviso(string titulo, string mensaje)
    {
        LimpiarContenido();
        var tarjeta = new Panel { Size = new Size(520, 170), BackColor = Tema.Superficie };
        tarjeta.Controls.Add(new Label
        {
            Text = mensaje,
            Font = Tema.Fuente,
            ForeColor = Tema.TextoSuave,
            Location = new Point(28, 68),
            Size = new Size(464, 80)
        });
        tarjeta.Controls.Add(new Label
        {
            Text = titulo,
            Font = Tema.FuenteSubtitulo,
            ForeColor = Tema.Texto,
            AutoSize = true,
            Location = new Point(26, 26)
        });

        void Centrar() => tarjeta.Location = new Point(
            Math.Max(0, (pnlContenido.Width - tarjeta.Width) / 2),
            Math.Max(0, (pnlContenido.Height - tarjeta.Height) / 2 - 40));
        Centrar();
        pnlContenido.Resize += (_, _) => Centrar();
        pnlContenido.Controls.Add(tarjeta);
    }

    private void LimpiarContenido()
    {
        _moduloActual?.Close();
        _moduloActual = null;
        foreach (Control c in pnlContenido.Controls.Cast<Control>().ToList())
            c.Dispose();
        pnlContenido.Controls.Clear();
    }

    private bool VerificarConexion()
    {
        bool ok = Db.ProbarConexion(out var error);
        lblConexion.Text = ok ? "●  Conectado a taller_mecanico" : "●  Sin conexión (clic para reintentar)";
        lblConexion.ForeColor = ok ? Color.FromArgb(74, 222, 128) : Color.FromArgb(248, 113, 113);
        new ToolTip().SetToolTip(lblConexion, ok ? Db.CadenaConexion.Split(";Pwd")[0] : error);
        return ok;
    }

    #endregion
}
