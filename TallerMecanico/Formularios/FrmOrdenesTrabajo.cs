using System.Data;
using System.Text;
using TallerMecanico.Modelos;
using TallerMecanico.Servicios;
using TallerMecanico.UI;
using static TallerMecanico.UI.Lienzo;

namespace TallerMecanico.Formularios;

/// <summary>
/// Ordenes de trabajo (cabecera + detalle de servicios + detalle de articulos).
/// Mismo diseno que FrmVentas: historial a la izquierda y panel de carga a la derecha.
/// </summary>
public partial class FrmOrdenesTrabajo : FrmAbmBase
{
    private const string ColOrden = "Orden / Recepción", ColVehiculo = "Vehículo / Cliente",
        ColMecanico = "Mecánico / Trabajo", ColEstado = "Estado", ColTotal = "Total", ColAcciones = "Acciones";

    private enum Pestana { Servicios, Articulos, Diagnostico }

    private readonly OrdenTrabajoService _ordenes = new();
    private readonly EmpleadoService _empleados = new();
    private readonly ArticuloService _articulos = new();

    // Filtros del historial
    private readonly FiltroOrdenes _filtro = new();
    private readonly List<BotonRedondo> _chips = [];

    // Orden en edicion
    private DataTable _tablaArticulos = new(), _tablaServicios = new();
    private (int Id, string Nombre) _empleado, _vehiculo;
    private int _idEmpresa;
    private EstadoOrdenTrabajo _estado;
    private OrdenTrabajo? _original;
    private List<DataRow> _sugeridos = [];
    private Pestana _pestana;

    private readonly CajaTexto cajaBuscarItem = new(new TextBox(), true);
    private readonly ListBox lstSugeridos = new() { Visible = false, IntegralHeight = false, BorderStyle = BorderStyle.FixedSingle };
    private readonly PanelDibujo pnlContexto = new();
    private readonly PanelDibujo pnlTitulos = new();
    private readonly Panel pnlArriba = new() { Dock = DockStyle.Top };
    private readonly FlowLayoutPanel flpServicios = NuevaLista(), flpArticulos = NuevaLista();
    private readonly TextBox txtDiagnostico = new()
    {
        Multiline = true, ScrollBars = ScrollBars.Vertical, BorderStyle = BorderStyle.FixedSingle, MaxLength = 4000,
        PlaceholderText = "Describí la falla encontrada y el trabajo a realizar...", Dock = DockStyle.Fill
    };
    private readonly Panel pnlDiagnostico = new() { Dock = DockStyle.Fill };
    private readonly BotonRedondo[] _pestanas = new BotonRedondo[3];
    private readonly PanelDibujo pnlTotales = new();
    private readonly BotonRedondo btnFiltros = new() { Text = "Filtros", Icono = Iconos.Filtro };

    public FrmOrdenesTrabajo()
    {
        InitializeComponent();

        Titulo = "Órdenes de Trabajo";
        Subtitulo = "Recepción, diagnóstico y reparación de vehículos";
        NombreEntidad = "orden de trabajo";
        NombreEntidadPlural = "órdenes de trabajo";
        TextoAlta = "Nueva Orden de Trabajo";
        TextoEdicion = "Editar Orden";

        SuspendLayout();
        ConstruirEncabezado();
        ConstruirHistorial();
        ConstruirPanelOrden();
        ResumeLayout(true);
    }

    private static FlowLayoutPanel NuevaLista() => new()
    {
        FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, Dock = DockStyle.Fill,
        BackColor = Tema.Superficie
    };

    protected override void OnLoad(EventArgs e)
    {
        ElegirPeriodo(3); // "En curso": lo que hay en el taller ahora
        base.OnLoad(e);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        switch (keyData)
        {
            case Keys.Escape when lstSugeridos.Visible:
                lstSugeridos.Visible = false;
                return true;
            case Keys.Escape when Modo != ModoAbm.Consulta:
                CerrarPanel();
                return true;
            case Keys.F5:
                RefrescarGrilla();
                return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    #region ABM

    protected override DataTable ObtenerDatos() => _ordenes.Listar(_filtro);

    protected override void CargarListas()
    {
        _tablaArticulos = _articulos.ListarCombo();
        _tablaServicios = _ordenes.ListarServicios();
    }

    protected override void LimpiarCampos()
    {
        QuitarLineas();
        cajaBuscarItem.Entrada.Clear();
        txtDiagnostico.Clear();
        _original = null;
        _estado = EstadoOrdenTrabajo.Recibido;
        _vehiculo = default;

        // El mecanico queda como en la orden anterior; la primera vez, el usuario conectado.
        if (_empleado.Id == 0 || _idEmpresa == 0)
        {
            _idEmpresa = OrdenTrabajoService.EmpresaPorDefecto();
            var mecanicos = _empleados.ListarCombo(_idEmpresa);
            var propio = mecanicos.Rows.Cast<DataRow>()
                .FirstOrDefault(f => Convert.ToInt32(f["id"]) == Sesion.Actual?.Id);
            var fila = propio ?? (mecanicos.Rows.Count > 0 ? mecanicos.Rows[0] : null);
            _empleado = fila is null ? default : (Convert.ToInt32(fila["id"]), $"{fila["descripcion"]}");
        }
        ElegirPestana(Pestana.Servicios);
        pnlContexto.Invalidate();
    }

    protected override void CargarRegistro(object id)
    {
        var o = _ordenes.Obtener(Convert.ToInt64(id))
                ?? throw new InvalidOperationException("La orden ya no existe. Actualizá la pantalla.");
        if (o.Estado == EstadoOrdenTrabajo.Cancelado)
            throw new InvalidOperationException("Una orden cancelada no se puede editar.");

        _original = o;
        _estado = o.Estado;
        _idEmpresa = o.IdEmpresa;
        _empleado = (o.IdEmpleado, _empleados.Obtener(o.IdEmpleado)?.Nombre ?? "—");
        var v = _ordenes.ListarVehiculos().Rows.Cast<DataRow>().FirstOrDefault(f => Convert.ToInt32(f["id"]) == o.IdVehiculo);
        _vehiculo = (o.IdVehiculo, v is null ? "—" : NombreVehiculo(v));
        txtDiagnostico.Text = o.Diagnostico ?? "";
        foreach (var s in o.Servicios) AgregarLinea(new LineaOrden(s));
        foreach (var a in o.Articulos) AgregarLinea(new LineaOrden(a));
        ElegirPestana(o.Servicios.Count == 0 && o.Articulos.Count > 0 ? Pestana.Articulos : Pestana.Servicios);
        pnlContexto.Invalidate();
    }

    protected override bool ValidarCampos()
    {
        if (_empleado.Id == 0) return MarcarError(pnlContexto, "Seleccioná el mecánico responsable.");
        if (_vehiculo.Id == 0) return MarcarError(pnlContexto, "Seleccioná el vehículo en la barra celeste.");
        if (!Lineas(flpServicios).Any() && !Lineas(flpArticulos).Any())
            return MarcarError(cajaBuscarItem, "Agregá al menos un servicio o un artículo.");
        foreach (var (lista, pestana) in new[] { (flpServicios, Pestana.Servicios), (flpArticulos, Pestana.Articulos) })
        {
            if (Lineas(lista).All(l => l.EsValida)) continue;
            ElegirPestana(pestana);
            return MarcarError(cajaBuscarItem, "Revisá las cantidades y los precios.");
        }
        return true;
    }

    protected override object? Guardar(object? id)
    {
        var orden = new OrdenTrabajo
        {
            Id = id is null ? 0 : Convert.ToInt64(id),
            IdEmpresa = _idEmpresa,
            IdEmpleado = _empleado.Id,
            IdVehiculo = _vehiculo.Id,
            Estado = _estado,
            Diagnostico = txtDiagnostico.Text,
            FechaRecepcion = _original?.FechaRecepcion ?? DateTime.Now,
            FechaDiagnostico = _original?.FechaDiagnostico,
            FechaFinalizacion = _original?.FechaFinalizacion,
            Servicios = Lineas(flpServicios).Select(l => l.Servicio!).ToList(),
            Articulos = Lineas(flpArticulos).Select(l => l.Articulo!).ToList()
        };
        return _ordenes.Procesar(id is null ? Operacion.Crear : Operacion.Editar, orden);
    }

    protected override void Eliminar(object id) => _ordenes.Eliminar(Convert.ToInt64(id));

    protected override string DescribirSeleccion() =>
        Convert.ToString(dgvDatos.CurrentRow?.Cells[ColOrden].Value)?.Split('\n')[0] ?? "";

    /// <summary>Vuelve a modo consulta; si hay datos cargados pide confirmar.</summary>
    private bool CerrarPanel()
    {
        if (Modo == ModoAbm.Consulta) return true;
        bool hayDatos = Lineas(flpServicios).Any() || Lineas(flpArticulos).Any() || txtDiagnostico.TextLength > 0;
        if (hayDatos && MessageBox.Show(this, "Se van a perder los datos de la orden sin guardar. ¿Continuar?", Titulo,
                MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            return false;
        CambiarModo(ModoAbm.Consulta);
        return true;
    }

    #endregion

    #region Encabezado

    private void ConstruirEncabezado()
    {
        pnlToolbar.Visible = false;
        pnlFooter.Visible = false;
        pnlHeader.Padding = Padding.Empty;
        pnlHeader.Height = Px(122);

        lblTitulo.Font = Fuente(22, true);
        lblTitulo.Location = new Point(Px(22), Px(11));
        lblSubtitulo.Font = Fuente(14);
        lblSubtitulo.ForeColor = Tema.Texto;
        lblSubtitulo.Location = new Point(Px(23), Px(42));
        lblSubtitulo.BringToFront();

        txtBuscar.PlaceholderText = "Buscar por chapa, cliente, Nro. o servicio...";
        var cajaBuscar = new CajaTexto(txtBuscar, true);
        // La base deshabilita la busqueda y la grilla al editar; aca se usan a la vez.
        txtBuscar.EnabledChanged += (_, _) => { if (!txtBuscar.Enabled) txtBuscar.Enabled = true; };
        dgvDatos.EnabledChanged += (_, _) => { if (!dgvDatos.Enabled) dgvDatos.Enabled = true; };

        var nueva = new BotonRedondo { Text = "Nueva Orden", Icono = Iconos.Agregar }.Primario();
        var exportar = new BotonRedondo { Text = "Exportar", Icono = Iconos.Exportar };
        var refrescar = new BotonRedondo { Icono = Iconos.Refrescar, TamanoIcono = 15 };
        nueva.Click += (_, _) => { if (CerrarPanel()) IniciarAlta(); };
        btnFiltros.Click += (_, _) => MostrarFiltros();
        exportar.Click += (_, _) => Exportar();
        refrescar.Click += (_, _) => { RefrescarGrilla(); Notificar("Datos actualizados."); };
        tipAyuda.SetToolTip(nueva, "Nueva orden de trabajo (Ctrl+N)");
        tipAyuda.SetToolTip(refrescar, "Volver a leer los datos (F5)");

        pnlHeader.Controls.AddRange([cajaBuscar, nueva, btnFiltros, exportar, refrescar]);

        var anchos = new[] { 51, 105, 83, 83, 64 };
        var textos = new[] { "Hoy", "Esta semana", "Este mes", "En curso", "Todas" };
        int x = Px(22);
        for (int i = 0; i < textos.Length; i++)
        {
            int indice = i;
            var chip = new BotonRedondo { Text = textos[i], Radio = 16 };
            chip.SetBounds(x, Px(78), Math.Max(Px(anchos[i]), Ancho(textos[i], chip.Font) + Px(24)), Px(32));
            chip.Click += (_, _) => { ElegirPeriodo(indice); RefrescarGrilla(); };
            x = chip.Right + Px(8);
            _chips.Add(chip);
            pnlHeader.Controls.Add(chip);
        }
        tipAyuda.SetToolTip(_chips[3], "Órdenes que todavía no se entregaron ni se cancelaron");

        void Ubicar()
        {
            int y = Px(24), alto = Px(37), r = pnlHeader.Width - Px(20);
            void Poner(Control c, int ancho, int separacion)
            {
                c.SetBounds(r - ancho, y, ancho, alto);
                r -= ancho + separacion;
            }
            Poner(refrescar, Px(32), Px(8));
            Poner(exportar, Px(94), Px(8));
            Poner(btnFiltros, Px(80), Px(8));
            Poner(nueva, Px(128), Px(20));
            int ancho = Math.Min(Px(318), r - Math.Max(lblSubtitulo.Right, lblTitulo.Right) - Px(12));
            Poner(cajaBuscar, Math.Max(Px(110), ancho), 0);
        }
        pnlHeader.Resize += (_, _) => Ubicar();
        lblSubtitulo.SizeChanged += (_, _) => Ubicar();
        Ubicar();
    }

    private void ElegirPeriodo(int indice)
    {
        var hoy = DateTime.Today;
        _filtro.Desde = indice switch
        {
            0 => hoy,
            1 => hoy.AddDays(-(((int)hoy.DayOfWeek + 6) % 7)), // lunes
            2 => new DateTime(hoy.Year, hoy.Month, 1),
            _ => null
        };
        _filtro.SoloEnCurso = indice == 3;
        if (indice == 3) _filtro.Estado = null;
        for (int i = 0; i < _chips.Count; i++)
        {
            _chips[i].Fondo = i == indice ? Color.FromArgb(214, 224, 236) : Color.FromArgb(244, 247, 251);
            _chips[i].Invalidate();
        }
    }

    private void MostrarFiltros()
    {
        var menu = new ContextMenuStrip { Font = Fuente(13) };
        void Opcion(ToolStripMenuItem padre, string texto, bool marcado, Action aplicar)
        {
            var item = new ToolStripMenuItem(texto) { Checked = marcado };
            item.Click += (_, _) =>
            {
                aplicar();
                bool activo = _filtro.Estado is not null || _filtro.IdEmpleado is not null;
                btnFiltros.ColorBorde = activo ? Azul : Lienzo.Borde;
                btnFiltros.ForeColor = activo ? Azul : Tema.Texto;
                RefrescarGrilla();
            };
            padre.DropDownItems.Add(item);
        }

        try
        {
            var estados = new ToolStripMenuItem("Estado");
            Opcion(estados, "Todos", _filtro.Estado is null, () => _filtro.Estado = null);
            foreach (var estado in Enum.GetValues<EstadoOrdenTrabajo>())
                Opcion(estados, estado.ATexto(), _filtro.Estado == estado, () =>
                {
                    _filtro.Estado = estado;
                    // "En curso" esconderia las entregadas y canceladas que se piden aca.
                    if (_filtro.SoloEnCurso) ElegirPeriodo(4);
                });

            var mecanicos = new ToolStripMenuItem("Mecánico");
            Opcion(mecanicos, "Todos", _filtro.IdEmpleado is null, () => _filtro.IdEmpleado = null);
            foreach (DataRow f in _empleados.Listar().Rows)
            {
                int id = Convert.ToInt32(f["id"]);
                Opcion(mecanicos, $"{f["nombre"]}", _filtro.IdEmpleado == id, () => _filtro.IdEmpleado = id);
            }
            menu.Items.AddRange([estados, mecanicos]);
        }
        catch (Exception ex)
        {
            MostrarError("No se pudieron cargar los filtros.", ex);
            return;
        }
        menu.Closed += (_, _) => BeginInvoke(menu.Dispose);
        menu.Show(btnFiltros, 0, btnFiltros.Height);
    }

    /// <summary>Guarda en CSV (se abre con Excel) las ordenes que se ven en la grilla.</summary>
    private void Exportar()
    {
        using var dialogo = new SaveFileDialog
        {
            Filter = "CSV para Excel|*.csv", FileName = $"ordenes_trabajo_{DateTime.Today:yyyy-MM-dd}.csv"
        };
        if (dialogo.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            File.WriteAllText(dialogo.FileName, CrearCsv(), new UTF8Encoding(true));
            Notificar($"Se exportaron {dgvDatos.Rows.Count} órdenes.");
        }
        catch (Exception ex)
        {
            MostrarError("No se pudo exportar.", ex);
        }
    }

    private string CrearCsv()
    {
        static string Celda(string texto)
        {
            // Excel ejecutaria como formula un texto que empiece asi.
            if (texto.Length > 0 && "=+-@".Contains(texto[0])) texto = "'" + texto;
            return "\"" + texto.Replace("\"", "\"\"") + "\"";
        }

        var csv = new StringBuilder("Nro;Recepción;Vehículo;Cliente;Mecánico;Trabajo;Estado;Total\r\n");
        foreach (DataGridViewRow f in dgvDatos.Rows)
        {
            string[] Partes(string columna) => $"{f.Cells[columna].Value}\n".Split('\n');
            var orden = Partes(ColOrden);
            var vehiculo = Partes(ColVehiculo);
            var mecanico = Partes(ColMecanico);
            csv.AppendLine(string.Join(";", new[]
            {
                orden[0].TrimStart('#'), orden[1], vehiculo[0], vehiculo[1], mecanico[0], mecanico[1],
                $"{f.Cells[ColEstado].Value}", Convert.ToDecimal(f.Cells[ColTotal].Value).ToString("0.##", Cultura)
            }.Select(Celda)));
        }
        return csv.ToString();
    }

    #endregion

    #region Historial

    private void ConstruirHistorial()
    {
        pnlGrid.Padding = new Padding(Px(22), 0, Px(22), Px(21));
        var pnlTitulo = new Panel { Dock = DockStyle.Top, Height = Px(53), BackColor = Tema.Fondo };
        var lblHistorial = new Label
        {
            Text = "Historial de Órdenes", Font = Fuente(18, true), AutoSize = true, Location = new Point(Px(8), Px(19))
        };
        // El estado de la base solo se muestra cuando es un aviso de exito.
        lblEstado.Dock = DockStyle.Right;
        lblEstado.Width = Px(300);
        lblEstado.TextAlign = ContentAlignment.MiddleRight;
        void Avisos() => lblEstado.Visible = lblEstado.Text.StartsWith('✓');
        lblEstado.TextChanged += (_, _) => Avisos();
        pnlTitulo.Controls.Add(lblHistorial);
        pnlTitulo.Controls.Add(lblEstado);
        pnlGrid.Controls.Add(pnlTitulo);
        pnlTitulo.SendToBack(); // se acopla antes que la grilla
        Avisos();

        dgvDatos.ColumnHeadersHeight = Px(27);
        dgvDatos.RowTemplate.Height = Px(56);
        dgvDatos.ScrollBars = ScrollBars.Vertical;
        dgvDatos.AlternatingRowsDefaultCellStyle.BackColor = Tema.Superficie;
        dgvDatos.CellPainting += PintarCelda;
        dgvDatos.CellMouseClick += ClicEnGrilla;
        dgvDatos.CellMouseMove += (_, e) =>
            dgvDatos.Cursor = AccionEn(e.RowIndex, e.ColumnIndex, e.X) >= 0 ? Cursors.Hand : Cursors.Default;
        dgvDatos.CellMouseLeave += (_, _) => dgvDatos.Cursor = Cursors.Default;
        dgvDatos.CellToolTipTextNeeded += (_, e) =>
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            e.ToolTipText = dgvDatos.Columns[e.ColumnIndex].Name == ColAcciones ? "Ver · Imprimir · Más opciones" : "";
        };
        dgvDatos.Resize += (_, _) =>
        {
            if (dgvDatos.Width < 20 || dgvDatos.Height < 20) return;
            using var camino = Redondeado(new RectangleF(0, 0, dgvDatos.Width, dgvDatos.Height), Px(10));
            var anterior = dgvDatos.Region;
            dgvDatos.Region = new Region(camino);
            anterior?.Dispose();
        };
    }

    protected override void AjustarColumnas(DataGridView grilla)
    {
        var pesos = new Dictionary<string, (float Peso, int Minimo)>
        {
            [ColOrden] = (130, 120), [ColVehiculo] = (175, 125), [ColMecanico] = (150, 115),
            [ColEstado] = (110, 112), [ColTotal] = (118, 108), [ColAcciones] = (101, 95)
        };
        foreach (DataGridViewColumn c in grilla.Columns)
        {
            c.SortMode = DataGridViewColumnSortMode.NotSortable;
            c.Visible = pesos.TryGetValue(c.Name, out var p);
            if (!c.Visible) continue;
            c.FillWeight = p.Peso;
            c.MinimumWidth = Px(p.Minimo);
        }
    }

    private static Color ColorEstado(string estado) => estado switch
    {
        "Recibido" => Color.FromArgb(226, 232, 240),
        "Diagnosticado" or "Presupuestado" => Color.FromArgb(191, 219, 254),
        "Aceptado" or "En reparacion" => Color.FromArgb(254, 226, 130),
        "Finalizado" => Color.FromArgb(146, 230, 173),
        "Entregado" => Color.FromArgb(187, 247, 208),
        "Cancelado" => Color.FromArgb(250, 166, 166),
        _ => Color.FromArgb(226, 232, 240)
    };

    private void PintarCelda(object? sender, DataGridViewCellPaintingEventArgs e)
    {
        if (e.ColumnIndex < 0 || e.Graphics is not { } g) return;
        var columna = dgvDatos.Columns[e.ColumnIndex].Name;
        var r = e.CellBounds;
        bool seleccionada = e.RowIndex >= 0 && (e.State & DataGridViewElementStates.Selected) != 0;
        using (var fondo = new SolidBrush(e.RowIndex < 0 || seleccionada ? Tema.SuperficieAlterna : Tema.Superficie))
            g.FillRectangle(fondo, r);
        using (var lapiz = new Pen(Tema.Borde))
            g.DrawLine(lapiz, r.Left, r.Bottom - 1, r.Right, r.Bottom - 1);

        var area = new Rectangle(r.X + Px(12), r.Y, r.Width - Px(16), r.Height);
        if (e.RowIndex < 0)
        {
            bool derecha = columna is ColTotal or ColAcciones;
            if (derecha) area.Width -= Px(8);
            Texto(g, columna, Fuente(13, true), Tema.Texto, area, derecha ? TextFormatFlags.Right : TextFormatFlags.Left);
        }
        else if (columna == ColTotal)
        {
            area = new Rectangle(r.X, r.Y, r.Width - Px(12), r.Height);
            Texto(g, Gs(Convert.ToDecimal(e.Value)), Fuente(15.5f, true), Tema.Texto, area, TextFormatFlags.Right);
        }
        else if (columna == ColEstado)
        {
            var texto = $"{e.Value}";
            int ancho = Ancho(texto, Fuente(14)) + Px(18), alto = Px(23);
            var pastilla = new Rectangle(area.X, r.Y + (r.Height - alto) / 2, Math.Min(ancho, area.Width), alto);
            Caja(g, pastilla, alto / 2f, ColorEstado(texto));
            Texto(g, texto, Fuente(14), Color.FromArgb(31, 41, 55), pastilla, TextFormatFlags.HorizontalCenter);
        }
        else if (columna == ColAcciones)
        {
            string[] iconos = [Iconos.Ver, Iconos.Imprimir, Iconos.Puntos];
            for (int i = 0; i < 3; i++)
                Icono(g, iconos[i], 17, Color.FromArgb(51, 65, 85), AreaAccion(r, i));
        }
        else
        {
            var partes = $"{e.Value}\n".Split('\n');
            bool suave = columna == ColMecanico;
            int mitad = r.Height / 2;
            Texto(g, partes[0], Fuente(15, !suave), Tema.Texto, new Rectangle(area.X, r.Y + Px(8), area.Width, mitad - Px(6)));
            Texto(g, partes[1], Fuente(14), suave ? Tema.TextoSuave : Tema.Texto,
                new Rectangle(area.X, r.Y + mitad - Px(1), area.Width, mitad - Px(6)));
        }
        e.Handled = true;
    }

    /// <summary>Los tres iconos ocupan 29 px cada uno, pegados a la derecha.</summary>
    private static Rectangle AreaAccion(Rectangle celda, int indice) =>
        new(celda.Right - Px(96) + indice * Px(29.5f), celda.Y, Px(29), celda.Height);

    private int AccionEn(int fila, int columna, int x)
    {
        if (fila < 0 || columna < 0 || dgvDatos.Columns[columna].Name != ColAcciones) return -1;
        var celda = new Rectangle(0, 0, dgvDatos.Columns[columna].Width, Px(56));
        for (int i = 0; i < 3; i++)
            if (AreaAccion(celda, i).Contains(x, 1)) return i;
        return -1;
    }

    private void ClicEnGrilla(object? sender, DataGridViewCellMouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        int accion = AccionEn(e.RowIndex, e.ColumnIndex, e.X);
        if (accion < 0) return;
        dgvDatos.CurrentCell = dgvDatos.Rows[e.RowIndex].Cells[ColOrden];
        if (accion == 2)
        {
            MostrarOpciones(dgvDatos.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false));
            return;
        }
        try
        {
            var o = _ordenes.Obtener(Convert.ToInt64(IdSeleccionado))
                    ?? throw new InvalidOperationException("La orden ya no existe. Actualizá la pantalla.");
            var fila = dgvDatos.Rows[e.RowIndex];
            var vehiculo = $"{fila.Cells[ColVehiculo].Value}\n".Split('\n');
            string[] datos =
            [
                $"Recepción: {o.FechaRecepcion:dd/MM/yyyy HH:mm}",
                "Vehículo: " + vehiculo[0], "Cliente: " + vehiculo[1],
                "Mecánico: " + $"{fila.Cells[ColMecanico].Value}\n".Split('\n')[0],
                "Estado: " + o.Estado.ATexto() + (o.FechaFinalizacion is { } fin ? $" (finalizada {fin:dd/MM/yyyy})" : "")
            ];
            if (accion == 0) ComprobanteOrden.Ver(this, o, datos);
            else ComprobanteOrden.Imprimir(this, o, datos);
        }
        catch (Exception ex)
        {
            MostrarError("No se pudo abrir la orden.", ex);
        }
    }

    private void MostrarOpciones(Rectangle celda)
    {
        var id = Convert.ToInt64(IdSeleccionado);
        var estado = Estados.Desde<EstadoOrdenTrabajo>(Convert.ToString(dgvDatos.CurrentRow!.Cells[ColEstado].Value));
        var menu = new ContextMenuStrip { Font = Fuente(13) };
        ToolStripMenuItem Opcion(string texto, bool habilitada, Action accion)
        {
            var item = new ToolStripMenuItem(texto, null, (_, _) => accion()) { Enabled = habilitada };
            menu.Items.Add(item);
            return item;
        }

        bool activa = estado != EstadoOrdenTrabajo.Cancelado;
        Opcion("Editar", activa, () => { if (CerrarPanel()) IniciarEdicion(); });

        // Paso siguiente del circuito del taller, a un clic.
        if (estado < EstadoOrdenTrabajo.Entregado)
        {
            var siguiente = estado + 1;
            Opcion($"Pasar a «{siguiente.ATexto()}»", true, () => CambiarEstado(id, siguiente)).Font = Fuente(13, true);
        }
        var cambiar = Opcion("Cambiar estado", activa, () => { });
        foreach (var e in Enum.GetValues<EstadoOrdenTrabajo>().Where(e => e != EstadoOrdenTrabajo.Cancelado))
        {
            var valor = e;
            cambiar.DropDownItems.Add(new ToolStripMenuItem(e.ATexto(), null, (_, _) => CambiarEstado(id, valor))
            {
                Checked = e == estado, Enabled = e != estado
            });
        }
        Opcion("Cancelar orden (devuelve el stock)", activa, () =>
        {
            if (MessageBox.Show(this, $"¿Cancelar la orden #{id}?\n\nSe devuelve el stock de los artículos y no se puede deshacer.",
                    "Confirmar cancelación", MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2) == DialogResult.Yes)
                CambiarEstado(id, EstadoOrdenTrabajo.Cancelado);
        });
        menu.Items.Add(new ToolStripSeparator());
        Opcion("Eliminar", true, () => { if (CerrarPanel()) EliminarSeleccionado(); });
        menu.Closed += (_, _) => BeginInvoke(menu.Dispose);
        menu.Show(dgvDatos, celda.Right - Px(30), celda.Y + Px(40));
    }

    private void CambiarEstado(long id, EstadoOrdenTrabajo estado)
    {
        // Si justo se esta editando esa orden, lo que hay en el panel quedaria viejo.
        if (Modo == ModoAbm.Edicion && Convert.ToInt64(IdActual) == id && !CerrarPanel()) return;
        try
        {
            _ordenes.CambiarEstado(id, estado);
            RefrescarGrilla(id);
            Notificar($"Orden #{id}: {estado.ATexto()}.");
        }
        catch (Exception ex)
        {
            MostrarError("No se pudo cambiar el estado.", ex);
        }
    }

    #endregion

    #region Panel de la orden

    private static IEnumerable<LineaOrden> Lineas(FlowLayoutPanel lista) => lista.Controls.OfType<LineaOrden>();

    private FlowLayoutPanel ListaActual => _pestana == Pestana.Articulos ? flpArticulos : flpServicios;

    private void ConstruirPanelOrden()
    {
        pnlEdicion.Width = Px(440);
        pnlEdicion.Padding = new Padding(Px(20), 0, Px(20), 0);
        pnlCampos.Visible = false;
        pnlAccionesEdicion.Visible = false;

        // Arriba: titulo, contexto, pestanas y busqueda
        lblEdicionTitulo.Dock = DockStyle.None;
        lblEdicionTitulo.Font = Fuente(18, true);
        lblEdicionTitulo.SetBounds(0, Px(12), Px(340), Px(32));
        var cerrar = new BotonRedondo { Icono = Iconos.Cerrar, TamanoIcono = 15, ColorBorde = null, Fondo = Tema.Superficie };
        cerrar.Click += (_, _) => CerrarPanel();
        pnlContexto.Cursor = Cursors.Hand;
        pnlContexto.Paint += (_, e) => PintarContexto(e.Graphics);
        pnlContexto.MouseClick += (_, e) => ClicEnContexto(e.X);
        tipAyuda.SetToolTip(pnlContexto, "Clic en mecánico, vehículo o estado para cambiarlo");

        string[] titulos = ["Servicios", "Artículos", "Diagnóstico"];
        for (int i = 0; i < 3; i++)
        {
            var pestana = (Pestana)i;
            _pestanas[i] = new BotonRedondo { Text = titulos[i], Radio = 16 };
            _pestanas[i].Click += (_, _) => ElegirPestana(pestana);
        }

        pnlTitulos.Paint += (_, e) => PintarTitulos(e.Graphics);
        pnlArriba.Controls.AddRange([lblEdicionTitulo, cerrar, pnlContexto, .. _pestanas, cajaBuscarItem, pnlTitulos]);
        void UbicarArriba()
        {
            int w = pnlArriba.Width;
            cerrar.SetBounds(w - Px(27), Px(14), Px(28), Px(28));
            pnlContexto.SetBounds(0, Px(56), w, Px(29));
            int x = 0, ancho = (w - Px(16)) / 3;
            foreach (var p in _pestanas)
            {
                p.SetBounds(x, Px(97), ancho, Px(32));
                x += ancho + Px(8);
            }
            cajaBuscarItem.SetBounds(0, Px(141), w, Px(32));
            pnlTitulos.SetBounds(0, Px(185), w, Px(27));
            lstSugeridos.SetBounds(pnlEdicion.Padding.Left, Px(175), w, Px(190));
        }
        pnlArriba.Resize += (_, _) => UbicarArriba();

        // Diagnostico: texto libre que ocupa el lugar de las listas
        txtDiagnostico.Font = Fuente(14);
        pnlDiagnostico.Padding = new Padding(0, 0, 0, Px(10));
        pnlDiagnostico.Controls.Add(txtDiagnostico);
        bool teniaTexto = false;
        txtDiagnostico.TextChanged += (_, _) =>
        {
            if (teniaTexto == txtDiagnostico.TextLength > 0) return;
            teniaTexto = !teniaTexto;
            ActualizarPestanas();
        };

        // Abajo: totales y botones
        pnlTotales.Dock = DockStyle.Bottom;
        pnlTotales.Height = Px(170);
        pnlTotales.BackColor = Tema.Superficie;
        pnlTotales.Paint += (_, e) => PintarTotales(e.Graphics);
        var guardar = new BotonRedondo { Text = "Guardar Orden (Ctrl+S)", Icono = Iconos.Tilde }.Primario();
        var cancelar = new BotonRedondo { Text = "Cancelar" };
        guardar.Click += (_, _) => GuardarActual();
        cancelar.Click += (_, _) => CerrarPanel();
        pnlTotales.Controls.AddRange([guardar, cancelar]);
        void UbicarBotones()
        {
            int w = pnlTotales.Width, y = pnlTotales.Height - Px(55), ancho = Px(128);
            cancelar.SetBounds(w - ancho, y, ancho, Px(36));
            guardar.SetBounds(0, y, w - ancho - Px(8), Px(36));
        }
        pnlTotales.Resize += (_, _) => UbicarBotones();

        flpServicios.Resize += (_, _) => AjustarLineas(flpServicios);
        flpArticulos.Resize += (_, _) => AjustarLineas(flpArticulos);

        lblMensajeEdicion.Height = Px(40);
        pnlEdicion.Controls.AddRange([flpServicios, flpArticulos, pnlDiagnostico, pnlArriba, pnlTotales, lstSugeridos]);
        // Orden de acople: los bordes primero, el contenido ocupa el resto.
        lblMensajeEdicion.SendToBack();
        pnlTotales.SendToBack();
        pnlArriba.SendToBack();
        flpServicios.BringToFront();
        flpArticulos.BringToFront();
        pnlDiagnostico.BringToFront();
        lstSugeridos.BringToFront();
        pnlArriba.Height = Px(212);
        UbicarArriba();
        UbicarBotones();

        lstSugeridos.Font = Fuente(14);
        lstSugeridos.MouseClick += (_, _) => AgregarSugerido();
        cajaBuscarItem.Entrada.TextChanged += (_, _) => Sugerir();
        cajaBuscarItem.Entrada.KeyDown += (_, e) => Selector.Teclas(e, lstSugeridos, AgregarSugerido);
        cajaBuscarItem.Entrada.Leave += (_, _) =>
        {
            if (IsHandleCreated) BeginInvoke(() =>
            {
                if (!lstSugeridos.Focused && !cajaBuscarItem.Entrada.Focused) lstSugeridos.Visible = false;
            });
        };
        pnlEdicion.VisibleChanged += (_, _) =>
        {
            if (pnlEdicion.Visible && IsHandleCreated) BeginInvoke(EnfocarPestana);
        };
        ElegirPestana(Pestana.Servicios);
    }

    private void ElegirPestana(Pestana pestana)
    {
        _pestana = pestana;
        bool diagnostico = pestana == Pestana.Diagnostico;
        flpServicios.Visible = pestana == Pestana.Servicios;
        flpArticulos.Visible = pestana == Pestana.Articulos;
        pnlDiagnostico.Visible = diagnostico;
        cajaBuscarItem.Visible = pnlTitulos.Visible = !diagnostico;
        pnlArriba.Height = Px(diagnostico ? 141 : 212);
        lstSugeridos.Visible = false;
        cajaBuscarItem.Entrada.Clear();
        cajaBuscarItem.Entrada.PlaceholderText = pestana == Pestana.Articulos
            ? "Buscar artículo por nombre o código..."
            : "Buscar servicio por nombre o código...";
        ActualizarPestanas();
        pnlTitulos.Invalidate();
        AjustarLineas(ListaActual);
        if (pnlEdicion.Visible) EnfocarPestana();
    }

    private void EnfocarPestana()
    {
        if (_pestana == Pestana.Diagnostico) txtDiagnostico.Focus();
        else cajaBuscarItem.Entrada.Focus();
    }

    /// <summary>Resalta la pestana activa y muestra cuantos items tiene cada lista.</summary>
    private void ActualizarPestanas()
    {
        int servicios = Lineas(flpServicios).Count(), articulos = Lineas(flpArticulos).Count();
        _pestanas[0].Text = servicios == 0 ? "Servicios" : $"Servicios ({servicios})";
        _pestanas[1].Text = articulos == 0 ? "Artículos" : $"Artículos ({articulos})";
        _pestanas[2].Text = txtDiagnostico.TextLength == 0 ? "Diagnóstico" : "Diagnóstico ✓";
        for (int i = 0; i < 3; i++)
        {
            bool activa = i == (int)_pestana;
            _pestanas[i].Fondo = activa ? Azul : Color.FromArgb(244, 247, 251);
            _pestanas[i].FondoHover = activa ? AzulHover : Color.FromArgb(226, 232, 240);
            _pestanas[i].ForeColor = activa ? Color.White : Tema.Texto;
            _pestanas[i].ColorBorde = activa ? null : Lienzo.Borde;
            _pestanas[i].Invalidate();
        }
    }

    private void PintarTitulos(Graphics g)
    {
        g.Clear(Tema.SuperficieAlterna);
        int w = pnlTitulos.Width, h = pnlTitulos.Height;
        bool articulos = _pestana == Pestana.Articulos;
        Texto(g, articulos ? "Artículo" : "Servicio / Observación", Fuente(13, true), Tema.Texto, new Rectangle(0, 0, Px(200), h));
        if (articulos)
            Texto(g, "Cantidad", Fuente(13, true), Tema.Texto, new Rectangle(w - Px(218), 0, Px(86), h), TextFormatFlags.HorizontalCenter);
        Texto(g, "Precio", Fuente(13, true), Tema.Texto, new Rectangle(w - Px(124), 0, Px(83), h), TextFormatFlags.Right);
        using var lapiz = new Pen(Tema.Borde);
        g.DrawLine(lapiz, 0, h - 1, w, h - 1);
    }

    private void Sugerir()
    {
        var texto = cajaBuscarItem.Entrada.Text.Trim();
        if (texto.Length == 0 || _pestana == Pestana.Diagnostico)
        {
            lstSugeridos.Visible = false;
            return;
        }
        _sugeridos = Selector.Buscar(_pestana == Pestana.Articulos ? _tablaArticulos : _tablaServicios, texto);
        lstSugeridos.Items.Clear();
        foreach (var f in _sugeridos)
            lstSugeridos.Items.Add(_pestana == Pestana.Articulos ? $"{f["descripcion"]}" : $"{f["descripcion"]}  ({Gs(Convert.ToDecimal(f["precio"]))})");
        if (_sugeridos.Count == 0) lstSugeridos.Items.Add("Sin resultados");
        lstSugeridos.SelectedIndex = 0;
        lstSugeridos.Visible = true;
        lstSugeridos.BringToFront();
    }

    private void AgregarSugerido()
    {
        if (!lstSugeridos.Visible || lstSugeridos.SelectedIndex < 0 || lstSugeridos.SelectedIndex >= _sugeridos.Count) return;
        var f = _sugeridos[lstSugeridos.SelectedIndex];
        int id = Convert.ToInt32(f["id"]);
        string nombre = $"{f["nombre"]}";
        decimal precio = Convert.ToDecimal(f["precio"]);

        if (_pestana == Pestana.Articulos)
        {
            if (Lineas(flpArticulos).FirstOrDefault(l => l.Articulo!.IdArticulo == id) is { } existente)
                existente.Sumar(1);
            else
                AgregarLinea(new LineaOrden(new OrdenTrabajoArticulo { IdArticulo = id, Articulo = nombre, Cantidad = 1, Precio = precio }));
        }
        else if (Lineas(flpServicios).FirstOrDefault(l => l.Servicio!.IdServicio == id) is { } repetido)
        {
            flpServicios.ScrollControlIntoView(repetido); // un servicio va una sola vez; el detalle va en la observacion
        }
        else
        {
            AgregarLinea(new LineaOrden(new OrdenTrabajoServicio { IdServicio = id, Servicio = nombre, Precio = precio }));
        }
        cajaBuscarItem.Entrada.Clear();
        cajaBuscarItem.Entrada.Focus();
    }

    private void AgregarLinea(LineaOrden linea)
    {
        var lista = linea.Articulo is not null ? flpArticulos : flpServicios;
        linea.Cambio += () => pnlTotales.Invalidate();
        linea.Quitar += l =>
        {
            lista.Controls.Remove(l);
            BeginInvoke(l.Dispose); // el clic todavia se esta procesando
            AjustarLineas(lista);
            ActualizarPestanas();
            pnlTotales.Invalidate();
        };
        tipAyuda.SetToolTip(linea, linea.Articulo?.Articulo ?? linea.Servicio?.Servicio);
        lista.Controls.Add(linea);
        AjustarLineas(lista);
        lista.ScrollControlIntoView(linea);
        ActualizarPestanas();
        pnlTotales.Invalidate();
    }

    private void QuitarLineas()
    {
        foreach (var l in Lineas(flpServicios).Concat(Lineas(flpArticulos)).ToList()) l.Dispose();
        ActualizarPestanas();
        pnlTotales.Invalidate();
    }

    private static void AjustarLineas(FlowLayoutPanel lista)
    {
        var lineas = Lineas(lista).ToList();
        int alto = lineas.Sum(l => l.Height);
        int ancho = lista.Width - (alto > lista.Height ? SystemInformation.VerticalScrollBarWidth + Px(4) : 0);
        foreach (var l in lineas) l.Width = ancho;
    }

    private void PintarTotales(Graphics g)
    {
        var o = new OrdenTrabajo
        {
            Servicios = Lineas(flpServicios).Select(l => l.Servicio!).ToList(),
            Articulos = Lineas(flpArticulos).Select(l => l.Articulo!).ToList()
        };
        int w = pnlTotales.Width;
        using (var lapiz = new Pen(Tema.Borde))
            g.DrawLine(lapiz, 0, 0, w, 0);
        void Fila(int y, string titulo, string valor)
        {
            Texto(g, titulo, Fuente(15), Tema.Texto, new Rectangle(0, Px(y), w / 2, Px(22)));
            Texto(g, valor, Fuente(15), Tema.Texto, new Rectangle(w / 3, Px(y), w - w / 3, Px(22)), TextFormatFlags.Right);
        }
        Fila(12, "Servicios:", Gs(o.TotalServicios));
        Fila(34, "Artículos:", Gs(o.TotalArticulos));
        using (var lapiz = new Pen(Tema.Borde))
            g.DrawLine(lapiz, 0, Px(64), w, Px(64));

        // Con importes grandes se achica para que no se corte.
        var fuente = Fuente(27, true);
        foreach (var px in new[] { 27f, 23f, 19f, 16f })
        {
            fuente = Fuente(px, true);
            if (Ancho("Total:", fuente) + Ancho(Gs(o.Total), fuente) + Px(12) <= w) break;
        }
        Texto(g, "Total:", fuente, Tema.Texto, new Rectangle(0, Px(68), w, Px(40)));
        Texto(g, Gs(o.Total), fuente, Tema.Texto, new Rectangle(0, Px(68), w, Px(40)), TextFormatFlags.Right);
    }

    #endregion

    #region Mecanico, vehiculo y estado

    private static string NombreVehiculo(DataRow f) => $"{f["chapa"]} ({f["cliente"]})";

    private string[] TextosContexto() =>
    [
        "Mecánico: " + (_empleado.Id == 0 ? "Elegir" : _empleado.Nombre),
        "Vehículo: " + (_vehiculo.Id == 0 ? "Elegir" : _vehiculo.Nombre),
        "Estado: " + _estado.ATexto()
    ];

    /// <summary>Se achica un poco si los nombres son largos.</summary>
    private Font FuenteContexto()
    {
        var textos = TextosContexto();
        foreach (var px in new[] { 11.5f, 11f, 10.5f })
            if (textos.Sum(t => Ancho(t, Fuente(px)) + 1) <= pnlContexto.Width - Px(30)) return Fuente(px);
        return Fuente(10);
    }

    /// <summary>Partes de la barra celeste; si no entran, se recortan en proporcion.</summary>
    private Rectangle[] AreasContexto()
    {
        var fuente = FuenteContexto();
        int separacion = Px(9), x = Px(6);
        var anchos = TextosContexto().Select(t => Ancho(t, fuente) + 1).ToArray();
        int disponible = pnlContexto.Width - x * 2 - separacion * 2, total = anchos.Sum();
        float factor = total > disponible ? disponible / (float)total : 1;
        return anchos.Select(a =>
        {
            var r = new Rectangle(x, 0, (int)(a * factor), pnlContexto.Height);
            x = r.Right + separacion;
            return r;
        }).ToArray();
    }

    private void PintarContexto(Graphics g)
    {
        g.Clear(Tema.Superficie);
        Caja(g, pnlContexto.ClientRectangle, Px(6), Tema.PrimarioSuave);
        var textos = TextosContexto();
        var areas = AreasContexto();
        var elegidos = new[] { _empleado.Id != 0, _vehiculo.Id != 0, true };
        for (int i = 0; i < 3; i++)
        {
            Texto(g, textos[i], FuenteContexto(), elegidos[i] ? Tema.Texto : Azul, areas[i]);
            if (i == 0) continue;
            using var lapiz = new Pen(Tema.TextoSuave);
            int x = areas[i].X - Px(4.5f);
            g.DrawLine(lapiz, x, Px(8), x, pnlContexto.Height - Px(8));
        }
    }

    private void ClicEnContexto(int x)
    {
        var areas = AreasContexto();
        int indice = x < areas[1].X - Px(4) ? 0 : x < areas[2].X - Px(4) ? 1 : 2;
        var posicion = new Point(Math.Max(0, Math.Min(areas[indice].X, pnlContexto.Width - Px(300))), pnlContexto.Height);
        try
        {
            switch (indice)
            {
                case 0:
                    Selector.Mostrar(pnlContexto, posicion, _empleados.ListarCombo(_idEmpresa, _empleado.Id), f =>
                    {
                        _empleado = (Convert.ToInt32(f["id"]), $"{f["descripcion"]}");
                        pnlContexto.Invalidate();
                    });
                    break;
                case 1:
                    Selector.Mostrar(pnlContexto, posicion, _ordenes.ListarVehiculos(), f =>
                    {
                        _vehiculo = (Convert.ToInt32(f["id"]), NombreVehiculo(f));
                        pnlContexto.Invalidate();
                    });
                    break;
                default:
                    // Cancelar no esta aca: se hace desde el menu de la grilla porque devuelve el stock.
                    var estados = new DataTable();
                    estados.Columns.Add("id", typeof(int));
                    estados.Columns.Add("descripcion", typeof(string));
                    foreach (var e in Enum.GetValues<EstadoOrdenTrabajo>().Where(e => e != EstadoOrdenTrabajo.Cancelado))
                        estados.Rows.Add((int)e, e.ATexto());
                    Selector.Mostrar(pnlContexto, posicion, estados, f =>
                    {
                        _estado = (EstadoOrdenTrabajo)Convert.ToInt32(f["id"]);
                        pnlContexto.Invalidate();
                    });
                    break;
            }
        }
        catch (Exception ex)
        {
            MostrarError("No se pudo cargar la lista.", ex);
        }
    }

    #endregion
}
