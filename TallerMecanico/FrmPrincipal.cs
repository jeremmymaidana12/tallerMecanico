using System.ComponentModel;
using TallerMecanico.Datos;
using TallerMecanico.Formularios;
using TallerMecanico.Servicios;
using TallerMecanico.UI;

namespace TallerMecanico;

/// <summary>
/// Ventana principal: menu lateral (hamburguesa) + area donde se abren los ABM.
/// Para sumar un modulo nuevo, agregar una linea en RegistrarModulos().
/// </summary>
[DesignerCategory("Code")]
public sealed class FrmPrincipal : Form
{
    private static readonly int AnchoExpandido = Lienzo.Px(183);
    private static readonly int AnchoColapsado = Lienzo.Px(58);

    private readonly Panel pnlLateral = new();
    private readonly Panel pnlMarca = new();
    private readonly FlowLayoutPanel flpMenu = new();
    private readonly Panel pnlContenido = new();
    private readonly Label lblConexion = new();
    private readonly BotonMenu btnHamburguesa = new(Iconos.Menu, "");
    private readonly Label lblMarcaTitulo = new();
    private readonly Label lblMarcaSubtitulo = new();
    private readonly ToolTip tipMenu = new() { InitialDelay = 250 };
    private readonly System.Windows.Forms.Timer tmrMenu = new() { Interval = 12 };
    private readonly List<BotonMenu> _botones = [];
    private readonly List<Label> _secciones = [];
    private BotonMenu? _botonActivo;
    private Form? _moduloActual;
    private bool _colapsado;
    private bool _conectado;

    public FrmPrincipal()
    {
        Text = "Taller Mecánico — Sistema de gestión" +
               (Sesion.Actual is { } u ? $"   ·   {u.Nombre} ({u.Rol})" : "");
        StartPosition = FormStartPosition.CenterScreen;
        var pantalla = Screen.PrimaryScreen!.WorkingArea.Size;
        Size = new Size(Math.Min(Lienzo.Px(1423), pantalla.Width), Math.Min(Lienzo.Px(770), pantalla.Height));
        MinimumSize = new Size(Math.Min(Lienzo.Px(1000), pantalla.Width), Math.Min(Lienzo.Px(640), pantalla.Height));
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
    /// Lista de modulos del menu. Para un modulo nuevo: AgregarModulo(icono, "Nombre", () => new FrmNuevo()).
    /// </summary>
    private void RegistrarModulos()
    {
        AgregarSeccion("TALLER");
        AgregarModulo(Iconos.Llave, "Órdenes de trabajo", () => new FrmOrdenesTrabajo());
        var vehiculos = AgregarModulo(Iconos.Auto, "Vehículos", () => new FrmVehiculos());
        AgregarModulo(Iconos.Clientes, "Clientes", () => new FrmClientes());

        AgregarSeccion("CATÁLOGOS");
        AgregarModulo(Iconos.Etiqueta, "Marcas", FrmAbmDescripcion.Marcas);
        AgregarModulo(Iconos.Capas, "Modelos", () => new FrmModelos());
        AgregarModulo(Iconos.Lista, "Categorías", FrmAbmDescripcion.Categorias);
        AgregarModulo(Iconos.Paquete, "Artículos", () => new FrmArticulos());

        AgregarSeccion("VENTAS");
        AgregarModulo(Iconos.Recibo, "Ventas", () => new FrmVentas());

        // Solo el rol Administrador ve esta seccion.
        if (Sesion.EsAdmin)
        {
            AgregarSeccion("ADMINISTRACIÓN");
            AgregarModulo(Iconos.Edificio, "Empresas", () => new FrmEmpresas());
            AgregarModulo(Iconos.Ubicacion, "Sucursales", () => new FrmSucursales());
            AgregarModulo(Iconos.Persona, "Usuarios", () => new FrmEmpleados());
            AgregarModulo(Iconos.Tarjeta, "Roles", FrmAbmDescripcion.TiposEmpleado);
        }

        Shown += (_, _) =>
        {
            if (VerificarConexion()) vehiculos.PerformClick();
            else MostrarAvisoSinConexion();
        };
    }

    private void MostrarAvisoSinConexion() =>
        MostrarAviso("Sin conexión a la base de datos",
            "Iniciá MySQL desde el panel de XAMPP e importá sql/taller_mecanico.sql.\n" +
            "Después hacé clic en el estado de conexión (abajo a la izquierda) para reintentar.");

    #region Construccion del menu

    private void ConstruirLateral()
    {
        pnlLateral.Dock = DockStyle.Left;
        pnlLateral.Width = AnchoExpandido;
        pnlLateral.BackColor = Tema.Lateral;

        pnlMarca.Dock = DockStyle.Top;
        pnlMarca.Height = Lienzo.Px(70);

        btnHamburguesa.Bounds = new Rectangle(Lienzo.Px(12), Lienzo.Px(12), Lienzo.Px(34), Lienzo.Px(30));
        btnHamburguesa.Colapsado = true; // solo icono
        btnHamburguesa.Click += (_, _) => AlternarMenu();
        tipMenu.SetToolTip(btnHamburguesa, "Mostrar / ocultar el menú");

        lblMarcaTitulo.Text = "Taller Mecánico";
        lblMarcaTitulo.ForeColor = Color.White;
        lblMarcaTitulo.Font = Lienzo.Fuente(16.5f, true);
        lblMarcaTitulo.AutoSize = true;
        lblMarcaTitulo.Location = new Point(Lienzo.Px(50), Lienzo.Px(15));

        lblMarcaSubtitulo.Text = "Sistema de gestión";
        lblMarcaSubtitulo.ForeColor = Tema.LateralTextoSuave;
        lblMarcaSubtitulo.Font = Lienzo.Fuente(13.5f);
        lblMarcaSubtitulo.AutoSize = true;
        lblMarcaSubtitulo.Location = new Point(Lienzo.Px(51), Lienzo.Px(40));

        pnlMarca.Controls.Add(btnHamburguesa);
        pnlMarca.Controls.Add(lblMarcaTitulo);
        pnlMarca.Controls.Add(lblMarcaSubtitulo);

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
        lblConexion.Click += (_, _) =>
        {
            bool estabaConectado = _conectado;
            if (VerificarConexion() && !estabaConectado && _botones.Count > 0) _botones[0].PerformClick();
        };

        ConstruirUsuario();

        pnlLateral.Controls.Add(flpMenu);
        pnlLateral.Controls.Add(pnlUsuario);
        pnlLateral.Controls.Add(lblConexion);
        pnlLateral.Controls.Add(pnlMarca);

        tmrMenu.Tick += (_, _) => AnimarMenu();
    }

    private void AgregarSeccion(string titulo)
    {
        var lbl = new Label
        {
            Text = titulo,
            Tag = titulo,
            AutoSize = false,
            Width = Lienzo.Px(165),
            Height = Lienzo.Px(24),
            TextAlign = ContentAlignment.BottomLeft,
            Padding = new Padding(Lienzo.Px(6), 0, 0, Lienzo.Px(2)),
            Margin = new Padding(0, Lienzo.Px(flpMenu.Controls.Count == 0 ? 4 : 10), 0, 0),
            Font = Lienzo.Fuente(11.5f),
            ForeColor = Tema.LateralTextoSuave
        };
        _secciones.Add(lbl);
        flpMenu.Controls.Add(lbl);
    }

    private BotonMenu AgregarModulo(string icono, string texto, Func<Form>? crear)
    {
        var boton = new BotonMenu(icono, texto)
        {
            Width = Lienzo.Px(163),
            Height = Lienzo.Px(32),
            Margin = new Padding(0, 0, 0, Lienzo.Px(2))
        };

        boton.Click += (_, _) =>
        {
            MarcarActivo(boton);
            if (crear is null)
            {
                MostrarAviso(texto, "Este módulo todavía está en desarrollo.");
                return;
            }
            if (!_conectado && !VerificarConexion())
            {
                MostrarAvisoSinConexion();
                return;
            }
            AbrirModulo(crear());
        };

        _botones.Add(boton);
        flpMenu.Controls.Add(boton);
        return boton;
    }

    private void MarcarActivo(BotonMenu boton)
    {
        if (_botonActivo is not null) _botonActivo.Activo = false;
        _botonActivo = boton;
        boton.Activo = true;
    }

    #endregion

    #region Usuario conectado

    private readonly Panel pnlUsuario = new();
    private readonly Label lblUsuarioNombre = new();
    private readonly Label lblUsuarioRol = new();
    private readonly BotonMenu btnCerrarSesion = new(Iconos.Salir, "Cerrar sesión");

    /// <summary>True si la ventana se cerro con "Cerrar sesion" (Program vuelve al login).</summary>
    public bool CerroSesion { get; private set; }

    private void ConstruirUsuario()
    {
        var u = Sesion.Actual;
        pnlUsuario.Dock = DockStyle.Bottom;
        pnlUsuario.Height = Lienzo.Px(96);
        pnlUsuario.Padding = new Padding(Lienzo.Px(9), 0, 0, Lienzo.Px(4));

        // Linea separadora + avatar con iniciales
        pnlUsuario.Paint += (_, e) =>
        {
            using var lapiz = new Pen(Tema.LateralHover);
            e.Graphics.DrawLine(lapiz, Lienzo.Px(12), 0, pnlUsuario.Width - Lienzo.Px(12), 0);
            var avatar = new Rectangle(Lienzo.Px(_colapsado ? 13 : 14), Lienzo.Px(12), Lienzo.Px(32), Lienzo.Px(32));
            Lienzo.Caja(e.Graphics, avatar, Lienzo.Px(16), Sesion.EsAdmin ? Tema.Primario : Tema.LateralHover);
            Lienzo.Texto(e.Graphics, u?.Iniciales ?? "?", Lienzo.Fuente(12.5f, true), Color.White, avatar,
                TextFormatFlags.HorizontalCenter);
        };

        lblUsuarioNombre.Text = u?.Nombre ?? "";
        lblUsuarioNombre.ForeColor = Color.White;
        lblUsuarioNombre.Font = Lienzo.Fuente(13, true);
        lblUsuarioNombre.AutoEllipsis = true;
        lblUsuarioNombre.Bounds = new Rectangle(Lienzo.Px(54), Lienzo.Px(10), Lienzo.Px(122), Lienzo.Px(20));

        lblUsuarioRol.Text = u is null ? "" : $"{u.Rol} · {u.Sucursal}";
        lblUsuarioRol.ForeColor = Tema.LateralTextoSuave;
        lblUsuarioRol.Font = Lienzo.Fuente(11);
        lblUsuarioRol.AutoEllipsis = true;
        lblUsuarioRol.Bounds = new Rectangle(Lienzo.Px(54), Lienzo.Px(29), Lienzo.Px(122), Lienzo.Px(18));
        if (u is not null) tipMenu.SetToolTip(lblUsuarioRol, $"{u.Correo}\n{u.Empresa} - {u.Sucursal}");

        btnCerrarSesion.Bounds = new Rectangle(Lienzo.Px(9), Lienzo.Px(56), Lienzo.Px(163), Lienzo.Px(32));
        btnCerrarSesion.Click += (_, _) =>
        {
            var r = MessageBox.Show(this, "¿Cerrar la sesión de " + (u?.Nombre ?? "") + "?", "Cerrar sesión",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (r != DialogResult.Yes) return;
            CerroSesion = true;
            Close();
        };

        pnlUsuario.Controls.Add(lblUsuarioNombre);
        pnlUsuario.Controls.Add(lblUsuarioRol);
        pnlUsuario.Controls.Add(btnCerrarSesion);
    }

    #endregion

    #region Menu hamburguesa

    /// <summary>Abre o cierra el menu lateral con una animacion corta.</summary>
    private void AlternarMenu()
    {
        _colapsado = !_colapsado;
        if (_colapsado) AplicarColapsado(true); // al cerrar, primero se ocultan los textos
        tmrMenu.Start();
    }

    private void AnimarMenu()
    {
        int destino = _colapsado ? AnchoColapsado : AnchoExpandido;
        int paso = Math.Max(Lienzo.Px(4), Math.Abs(destino - pnlLateral.Width) / 3);
        pnlLateral.Width = pnlLateral.Width < destino
            ? Math.Min(destino, pnlLateral.Width + paso)
            : Math.Max(destino, pnlLateral.Width - paso);

        if (pnlLateral.Width != destino) return;
        tmrMenu.Stop();
        if (!_colapsado) AplicarColapsado(false); // al abrir, los textos aparecen al final
    }

    private void AplicarColapsado(bool colapsado)
    {
        SuspendLayout();
        lblMarcaTitulo.Visible = !colapsado;
        lblMarcaSubtitulo.Visible = !colapsado;
        flpMenu.Padding = new Padding(colapsado ? Lienzo.Px(7) : Lienzo.Px(9), 0, 0, 0);

        foreach (var lbl in _secciones)
        {
            lbl.Text = colapsado ? "" : (string)lbl.Tag!;
            lbl.Width = colapsado ? Lienzo.Px(44) : Lienzo.Px(165);
            lbl.Height = colapsado ? Lienzo.Px(12) : Lienzo.Px(24);
        }
        foreach (var b in _botones)
        {
            b.Colapsado = colapsado;
            b.Width = colapsado ? Lienzo.Px(44) : Lienzo.Px(163);
            tipMenu.SetToolTip(b, colapsado ? b.Text : null);
        }
        lblUsuarioNombre.Visible = !colapsado;
        lblUsuarioRol.Visible = !colapsado;
        btnCerrarSesion.Colapsado = colapsado;
        btnCerrarSesion.Width = colapsado ? Lienzo.Px(44) : Lienzo.Px(163);
        btnCerrarSesion.Left = colapsado ? Lienzo.Px(7) : Lienzo.Px(9);
        tipMenu.SetToolTip(btnCerrarSesion, colapsado ? btnCerrarSesion.Text : null);
        pnlUsuario.Invalidate();
        ActualizarTextoConexion();
        ResumeLayout(true);
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
        _conectado = ok;
        _errorConexion = error;
        ActualizarTextoConexion();
        return ok;
    }

    private string _errorConexion = "";

    private void ActualizarTextoConexion()
    {
        string texto = _conectado ? "Conectado a taller_mecanico" : "Sin conexión (clic para reintentar)";
        lblConexion.Text = _colapsado ? "●" : "●  " + texto;
        lblConexion.ForeColor = _conectado ? Color.FromArgb(74, 222, 128) : Color.FromArgb(248, 113, 113);
        tipMenu.SetToolTip(lblConexion, _conectado
            ? Db.CadenaConexion.Split(";Pwd")[0]
            : texto + (_errorConexion.Length > 0 ? "\n" + _errorConexion : ""));
    }

    #endregion
}
