using System.Data;
using System.Text;
using TallerMecanico.Modelos;
using TallerMecanico.Servicios;
using TallerMecanico.UI;
using static TallerMecanico.UI.Lienzo;

namespace TallerMecanico.Formularios;

/// <summary>Ventas (cabecera + detalle). Hereda el comportamiento de FrmAbmBase con un diseno propio.</summary>
public partial class FrmVentas : FrmAbmBase
{
    private const string ColComprobante = "Comprobante / Fecha", ColCliente = "Cliente / Documento",
        ColVendedor = "Vendedor / Sucursal", ColEstado = "Estado", ColTotal = "Total", ColAcciones = "Acciones";

    private readonly VentaService _ventas = new();
    private readonly ClienteService _clientes = new();
    private readonly EmpleadoService _empleados = new();
    private readonly SucursalService _sucursales = new();
    private readonly ArticuloService _articulos = new();

    // Filtros del historial
    private readonly FiltroVentas _filtro = new();
    private readonly List<BotonRedondo> _chips = [];

    // Venta en edicion
    private DataTable _tablaArticulos = new();
    private (int Id, string Nombre) _empresa, _sucursal, _empleado, _cliente;
    private EstadoVenta _estado;
    private List<DataRow> _sugeridos = [];

    private readonly CajaTexto cajaArticulo =
        new(new TextBox { PlaceholderText = "Buscar artículo por nombre o código..." }, true);
    private readonly ListBox lstSugeridos = new() { Visible = false, IntegralHeight = false, BorderStyle = BorderStyle.FixedSingle };
    private readonly PanelDibujo pnlContexto = new();
    private readonly FlowLayoutPanel flpLineas = new()
    {
        FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true
    };
    private readonly PanelDibujo pnlTotales = new();
    private readonly BotonRedondo btnFiltros = new() { Text = "Filtros", Icono = Iconos.Filtro };

    public FrmVentas()
    {
        InitializeComponent();

        Titulo = "Ventas";
        Subtitulo = "Registro y gestión de ventas de artículos";
        NombreEntidad = "venta";
        NombreEntidadPlural = "ventas";
        TextoAlta = "Generar Nueva Venta";
        TextoEdicion = "Editar Venta";

        SuspendLayout();
        ConstruirEncabezado();
        ConstruirHistorial();
        ConstruirPanelVenta();
        ResumeLayout(true);
    }

    protected override void OnLoad(EventArgs e)
    {
        ElegirPeriodo(1);
        base.OnLoad(e);
        if (!EsModoDiseno && !DesignMode) BeginInvoke(IniciarAlta);
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

    protected override DataTable ObtenerDatos() => _ventas.Listar(_filtro);

    protected override void CargarListas() => _tablaArticulos = _articulos.ListarCombo();

    protected override void LimpiarCampos()
    {
        QuitarLineas();
        cajaArticulo.Entrada.Clear();
        _estado = EstadoVenta.Completado;
        _cliente = default;
        // Sucursal y vendedor quedan como en la venta anterior.
        if (_sucursal.Id == 0 && _sucursales.ListarTodas() is { Rows.Count: > 0 } t) ElegirSucursal(t.Rows[0]);
        pnlContexto.Invalidate();
    }

    protected override void CargarRegistro(object id)
    {
        var v = _ventas.Obtener(Convert.ToInt64(id))
                ?? throw new InvalidOperationException("La venta ya no existe. Actualizá la pantalla.");
        if (v.Estado == EstadoVenta.Anulado)
            throw new InvalidOperationException("Una venta anulada no se puede editar.");

        _estado = v.Estado;
        _empresa = (v.IdEmpresa, "");
        _sucursal = (v.IdSucursal, _sucursales.Obtener(v.IdSucursal)?.Nombre ?? "—");
        _empleado = (v.IdEmpleado, _empleados.Obtener(v.IdEmpleado)?.Nombre ?? "—");
        var c = _clientes.Obtener(v.IdCliente);
        _cliente = (v.IdCliente, c is null ? "—" : $"{c.Apellido}, {c.Nombre}");
        foreach (var d in v.Detalles) AgregarLinea(d);
        pnlContexto.Invalidate();
    }

    protected override bool ValidarCampos()
    {
        if (_sucursal.Id == 0) return MarcarError(pnlContexto, "Seleccioná la sucursal.");
        if (_empleado.Id == 0) return MarcarError(pnlContexto, "Seleccioná el vendedor.");
        if (_cliente.Id == 0) return MarcarError(pnlContexto, "Seleccioná el cliente en la barra celeste.");
        if (!Lineas.Any()) return MarcarError(cajaArticulo, "Agregá al menos un artículo.");
        if (Lineas.Any(l => !l.EsValida)) return MarcarError(cajaArticulo, "Revisá las cantidades y los precios.");
        return true;
    }

    protected override object? Guardar(object? id)
    {
        var venta = new VentaCabecera
        {
            Id = id is null ? 0 : Convert.ToInt64(id),
            IdEmpresa = _empresa.Id,
            IdSucursal = _sucursal.Id,
            IdEmpleado = _empleado.Id,
            IdCliente = _cliente.Id,
            Estado = _estado,
            Detalles = Lineas.Select(l => l.Detalle).ToList()
        };
        return _ventas.Procesar(id is null ? Operacion.Crear : Operacion.Editar, venta);
    }

    protected override void Eliminar(object id) => _ventas.Eliminar(Convert.ToInt64(id));

    protected override string DescribirSeleccion() =>
        Convert.ToString(dgvDatos.CurrentRow?.Cells[ColComprobante].Value)?.Split('\n')[0] ?? "";

    /// <summary>Vuelve a modo consulta; si hay articulos cargados pide confirmar.</summary>
    private bool CerrarPanel()
    {
        if (Modo == ModoAbm.Consulta) return true;
        if (Lineas.Any() && MessageBox.Show(this, "Se van a perder los datos de la venta sin guardar. ¿Continuar?", Titulo,
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

        txtBuscar.PlaceholderText = "Buscar por cliente, Nro. o artículo...";
        var cajaBuscar = new CajaTexto(txtBuscar, true);
        // La base deshabilita la busqueda y la grilla al editar; aca se usan a la vez.
        txtBuscar.EnabledChanged += (_, _) => { if (!txtBuscar.Enabled) txtBuscar.Enabled = true; };
        dgvDatos.EnabledChanged += (_, _) => { if (!dgvDatos.Enabled) dgvDatos.Enabled = true; };

        var nueva = new BotonRedondo { Text = "Nueva Venta", Icono = Iconos.Agregar }.Primario();
        var exportar = new BotonRedondo { Text = "Exportar", Icono = Iconos.Exportar };
        var refrescar = new BotonRedondo { Icono = Iconos.Refrescar, TamanoIcono = 15 };
        nueva.Click += (_, _) => { if (CerrarPanel()) IniciarAlta(); };
        btnFiltros.Click += (_, _) => MostrarFiltros();
        exportar.Click += (_, _) => Exportar();
        refrescar.Click += (_, _) => { RefrescarGrilla(); Notificar("Datos actualizados."); };
        tipAyuda.SetToolTip(nueva, "Nueva venta (Ctrl+N)");
        tipAyuda.SetToolTip(refrescar, "Volver a leer los datos (F5)");

        pnlHeader.Controls.AddRange([cajaBuscar, nueva, btnFiltros, exportar, refrescar]);

        var anchos = new[] { 51, 105, 83, 95, 64 };
        var textos = new[] { "Hoy", "Esta semana", "Este mes", "Pendientes", "Todas" };
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
        _filtro.Estado = indice == 3 ? EstadoVenta.Pendiente : null;
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
                bool activo = _filtro.IdSucursal is not null || _filtro.IdEmpleado is not null;
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
            foreach (var estado in Enum.GetValues<EstadoVenta>())
                Opcion(estados, estado.ATexto(), _filtro.Estado == estado, () => _filtro.Estado = estado);

            var sucursales = new ToolStripMenuItem("Sucursal");
            Opcion(sucursales, "Todas", _filtro.IdSucursal is null, () => _filtro.IdSucursal = null);
            foreach (DataRow f in _sucursales.ListarTodas().Rows)
            {
                int id = Convert.ToInt32(f["id"]);
                Opcion(sucursales, $"{f["descripcion"]}", _filtro.IdSucursal == id, () => _filtro.IdSucursal = id);
            }

            var vendedores = new ToolStripMenuItem("Vendedor");
            Opcion(vendedores, "Todos", _filtro.IdEmpleado is null, () => _filtro.IdEmpleado = null);
            foreach (DataRow f in _empleados.Listar().Rows)
            {
                int id = Convert.ToInt32(f["id"]);
                Opcion(vendedores, $"{f["nombre"]}", _filtro.IdEmpleado == id, () => _filtro.IdEmpleado = id);
            }
            menu.Items.AddRange([estados, sucursales, vendedores]);
        }
        catch (Exception ex)
        {
            MostrarError("No se pudieron cargar los filtros.", ex);
            return;
        }
        menu.Closed += (_, _) => BeginInvoke(menu.Dispose);
        menu.Show(btnFiltros, 0, btnFiltros.Height);
    }

    /// <summary>Guarda en CSV (se abre con Excel) las ventas que se ven en la grilla.</summary>
    private void Exportar()
    {
        using var dialogo = new SaveFileDialog
        {
            Filter = "CSV para Excel|*.csv", FileName = $"ventas_{DateTime.Today:yyyy-MM-dd}.csv"
        };
        if (dialogo.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            File.WriteAllText(dialogo.FileName, CrearCsv(), new UTF8Encoding(true));
            Notificar($"Se exportaron {dgvDatos.Rows.Count} ventas.");
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

        var csv = new StringBuilder("Nro;Fecha;Cliente;Documento;Vendedor;Sucursal;Estado;Total\r\n");
        foreach (DataGridViewRow f in dgvDatos.Rows)
        {
            string[] Partes(string columna) => $"{f.Cells[columna].Value}\n".Split('\n');
            var comprobante = Partes(ColComprobante);
            var cliente = Partes(ColCliente);
            var vendedor = Partes(ColVendedor);
            csv.AppendLine(string.Join(";", new[]
            {
                comprobante[0].TrimStart('#'), comprobante[1], cliente[0], cliente[1], vendedor[0], vendedor[1],
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
            Text = "Historial de Ventas", Font = Fuente(18, true), AutoSize = true, Location = new Point(Px(8), Px(19))
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
            [ColComprobante] = (156, 146), [ColCliente] = (155, 115), [ColVendedor] = (144, 110),
            [ColEstado] = (106, 112), [ColTotal] = (118, 108), [ColAcciones] = (101, 95)
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
            var color = texto switch
            {
                "Completado" => Color.FromArgb(146, 230, 173),
                "Anulado" => Color.FromArgb(250, 166, 166),
                _ => Color.FromArgb(254, 226, 130)
            };
            int ancho = Ancho(texto, Fuente(14)) + Px(18), alto = Px(23);
            var pastilla = new Rectangle(area.X, r.Y + (r.Height - alto) / 2, ancho, alto);
            Caja(g, pastilla, alto / 2f, color);
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
            bool vendedor = columna == ColVendedor;
            int mitad = r.Height / 2;
            Texto(g, partes[0], Fuente(15, !vendedor), Tema.Texto, new Rectangle(area.X, r.Y + Px(8), area.Width, mitad - Px(6)));
            Texto(g, partes[1], Fuente(14), vendedor ? Tema.TextoSuave : Tema.Texto,
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
        dgvDatos.CurrentCell = dgvDatos.Rows[e.RowIndex].Cells[ColComprobante];
        if (accion == 2)
        {
            MostrarOpciones(dgvDatos.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false));
            return;
        }
        try
        {
            var v = _ventas.Obtener(Convert.ToInt64(IdSeleccionado))
                    ?? throw new InvalidOperationException("La venta ya no existe. Actualizá la pantalla.");
            var fila = dgvDatos.Rows[e.RowIndex];
            string Linea(string columna) => $"{fila.Cells[columna].Value}".Replace("\n", "  -  ");
            string[] datos =
            [
                "Fecha: " + $"{fila.Cells[ColComprobante].Value}\n".Split('\n')[1],
                "Cliente: " + Linea(ColCliente), "Vendedor: " + Linea(ColVendedor), "Estado: " + v.Estado.ATexto()
            ];
            if (accion == 0) ComprobanteVenta.Ver(this, v, datos);
            else ComprobanteVenta.Imprimir(this, v, datos);
        }
        catch (Exception ex)
        {
            MostrarError("No se pudo abrir la venta.", ex);
        }
    }

    private void MostrarOpciones(Rectangle celda)
    {
        var id = Convert.ToInt64(IdSeleccionado);
        var estado = Estados.Desde<EstadoVenta>(Convert.ToString(dgvDatos.CurrentRow!.Cells[ColEstado].Value));
        var menu = new ContextMenuStrip { Font = Fuente(13) };
        void Opcion(string texto, bool habilitada, Action accion) =>
            menu.Items.Add(new ToolStripMenuItem(texto, null, (_, _) => accion()) { Enabled = habilitada });

        bool activa = estado != EstadoVenta.Anulado;
        Opcion("Editar", activa, () => { if (CerrarPanel()) IniciarEdicion(); });
        Opcion("Marcar como completada", estado == EstadoVenta.Pendiente, () => CambiarEstado(id, EstadoVenta.Completado));
        Opcion("Marcar como pendiente", estado == EstadoVenta.Completado, () => CambiarEstado(id, EstadoVenta.Pendiente));
        Opcion("Anular (devuelve el stock)", activa, () =>
        {
            if (MessageBox.Show(this, $"¿Anular la venta #{id}?\n\nSe devuelve el stock y no se puede deshacer.", "Confirmar anulación",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
                CambiarEstado(id, EstadoVenta.Anulado);
        });
        menu.Items.Add(new ToolStripSeparator());
        Opcion("Eliminar", true, () => { if (CerrarPanel()) EliminarSeleccionado(); });
        menu.Closed += (_, _) => BeginInvoke(menu.Dispose);
        menu.Show(dgvDatos, celda.Right - Px(30), celda.Y + Px(40));
    }

    private void CambiarEstado(long id, EstadoVenta estado)
    {
        // Si justo se esta editando esa venta, lo que hay en el panel quedaria viejo.
        if (Modo == ModoAbm.Edicion && Convert.ToInt64(IdActual) == id && !CerrarPanel()) return;
        try
        {
            _ventas.CambiarEstado(id, estado);
            RefrescarGrilla(id);
            Notificar($"Venta #{id}: {estado.ATexto()}.");
        }
        catch (Exception ex)
        {
            MostrarError("No se pudo cambiar el estado.", ex);
        }
    }

    #endregion

    #region Panel de venta

    private IEnumerable<LineaVenta> Lineas => flpLineas.Controls.OfType<LineaVenta>();

    private void ConstruirPanelVenta()
    {
        pnlEdicion.Width = Px(396);
        pnlEdicion.Padding = new Padding(Px(20), 0, Px(20), 0);
        pnlCampos.Visible = false;
        pnlAccionesEdicion.Visible = false;

        // Arriba: titulo, contexto y busqueda
        var pnlArriba = new Panel { Dock = DockStyle.Top, Height = Px(196) };
        lblEdicionTitulo.Dock = DockStyle.None;
        lblEdicionTitulo.Font = Fuente(18, true);
        lblEdicionTitulo.SetBounds(0, Px(12), Px(300), Px(32));
        var cerrar = new BotonRedondo { Icono = Iconos.Cerrar, TamanoIcono = 15, ColorBorde = null, Fondo = Tema.Superficie };
        cerrar.Click += (_, _) => CerrarPanel();
        var lblBuscar = new Label { Text = "Buscar Artículo", Font = Fuente(14, true), AutoSize = true, Location = new Point(-Px(3), Px(98)) };
        pnlContexto.Cursor = Cursors.Hand;
        pnlContexto.Paint += (_, e) => PintarContexto(e.Graphics);
        pnlContexto.MouseClick += (_, e) => ClicEnContexto(e.X);
        tipAyuda.SetToolTip(pnlContexto, "Clic en sucursal, vendedor o cliente para cambiarlo");
        var pnlTitulos = new PanelDibujo();
        pnlTitulos.Paint += (_, e) =>
        {
            e.Graphics.Clear(Tema.SuperficieAlterna);
            int w = pnlTitulos.Width, h = pnlTitulos.Height;
            Texto(e.Graphics, "Artículo", Fuente(13, true), Tema.Texto, new Rectangle(0, 0, Px(120), h));
            Texto(e.Graphics, "Cantidad", Fuente(13, true), Tema.Texto, new Rectangle(w - Px(218), 0, Px(86), h), TextFormatFlags.HorizontalCenter);
            Texto(e.Graphics, "Precio", Fuente(13, true), Tema.Texto, new Rectangle(w - Px(124), 0, Px(83), h), TextFormatFlags.Right);
            using var lapiz = new Pen(Tema.Borde);
            e.Graphics.DrawLine(lapiz, 0, h - 1, w, h - 1);
        };
        pnlArriba.Controls.AddRange([lblEdicionTitulo, cerrar, pnlContexto, lblBuscar, cajaArticulo, pnlTitulos]);
        void UbicarArriba()
        {
            int w = pnlArriba.Width;
            cerrar.SetBounds(w - Px(27), Px(14), Px(28), Px(28));
            pnlContexto.SetBounds(0, Px(56), w, Px(29));
            cajaArticulo.SetBounds(0, Px(123), w, Px(32));
            pnlTitulos.SetBounds(0, Px(169), w, Px(27));
            lstSugeridos.SetBounds(pnlEdicion.Padding.Left, Px(157), w, Px(190));
        }
        pnlArriba.Resize += (_, _) => UbicarArriba();

        // Abajo: totales y botones
        pnlTotales.Dock = DockStyle.Bottom;
        pnlTotales.Height = Px(225);
        pnlTotales.BackColor = Tema.Superficie;
        pnlTotales.Paint += (_, e) => PintarTotales(e.Graphics);
        var guardar = new BotonRedondo { Text = "Guardar Venta (Ctrl+S)", Icono = Iconos.Tilde }.Primario();
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

        flpLineas.Dock = DockStyle.Fill;
        flpLineas.BackColor = Tema.Superficie;
        flpLineas.Resize += (_, _) => AjustarLineas();

        lblMensajeEdicion.Height = Px(40);
        pnlEdicion.Controls.AddRange([flpLineas, pnlArriba, pnlTotales, lstSugeridos]);
        // Orden de acople: los bordes primero, las lineas ocupan el resto.
        lblMensajeEdicion.SendToBack();
        pnlTotales.SendToBack();
        pnlArriba.SendToBack();
        flpLineas.BringToFront();
        lstSugeridos.BringToFront();
        UbicarArriba();
        UbicarBotones();

        lstSugeridos.Font = Fuente(14);
        lstSugeridos.MouseClick += (_, _) => AgregarSugerido();
        cajaArticulo.Entrada.TextChanged += (_, _) => Sugerir();
        cajaArticulo.Entrada.KeyDown += (_, e) => Selector.Teclas(e, lstSugeridos, AgregarSugerido);
        cajaArticulo.Entrada.Leave += (_, _) =>
        {
            if (IsHandleCreated) BeginInvoke(() =>
            {
                if (!lstSugeridos.Focused && !cajaArticulo.Entrada.Focused) lstSugeridos.Visible = false;
            });
        };
        pnlEdicion.VisibleChanged += (_, _) =>
        {
            if (pnlEdicion.Visible && IsHandleCreated) BeginInvoke(() => cajaArticulo.Entrada.Focus());
        };
    }

    private void Sugerir()
    {
        var texto = cajaArticulo.Entrada.Text.Trim();
        if (texto.Length == 0)
        {
            lstSugeridos.Visible = false;
            return;
        }
        _sugeridos = Selector.Buscar(_tablaArticulos, texto);
        lstSugeridos.Items.Clear();
        foreach (var f in _sugeridos) lstSugeridos.Items.Add($"{f["descripcion"]}");
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
        if (Lineas.FirstOrDefault(l => l.Detalle.IdArticulo == id) is { } existente)
        {
            existente.Sumar(1);
        }
        else
        {
            var detalle = new VentaDetalle
            {
                IdArticulo = id, Articulo = $"{f["nombre"]}", Cantidad = 1, Precio = Convert.ToDecimal(f["precio"])
            };
            detalle.Calcular(exenta: false);
            AgregarLinea(detalle);
        }
        cajaArticulo.Entrada.Clear();
        cajaArticulo.Entrada.Focus();
    }

    private void AgregarLinea(VentaDetalle detalle)
    {
        var linea = new LineaVenta(detalle);
        linea.Cambio += () => pnlTotales.Invalidate();
        linea.Quitar += l =>
        {
            flpLineas.Controls.Remove(l);
            BeginInvoke(l.Dispose); // el clic todavia se esta procesando
            AjustarLineas();
            pnlTotales.Invalidate();
        };
        tipAyuda.SetToolTip(linea, detalle.Articulo + "\nClic derecho: marcar como exenta de IVA");
        flpLineas.Controls.Add(linea);
        AjustarLineas();
        flpLineas.ScrollControlIntoView(linea);
        pnlTotales.Invalidate();
    }

    private void QuitarLineas()
    {
        foreach (var l in Lineas.ToList()) l.Dispose();
        pnlTotales.Invalidate();
    }

    private void AjustarLineas()
    {
        int alto = Lineas.Sum(l => l.Height);
        int ancho = flpLineas.Width - (alto > flpLineas.Height ? SystemInformation.VerticalScrollBarWidth + Px(4) : 0);
        foreach (var l in Lineas) l.Width = ancho;
    }

    private void PintarTotales(Graphics g)
    {
        var v = new VentaCabecera { Detalles = Lineas.Select(l => l.Detalle).ToList() };
        v.CalcularTotales();
        int w = pnlTotales.Width;
        void Fila(int y, string titulo, string valor)
        {
            Texto(g, titulo, Fuente(15), Tema.Texto, new Rectangle(0, Px(y), w / 2, Px(22)));
            Texto(g, valor, Fuente(15), Tema.Texto, new Rectangle(w / 3, Px(y), w - w / 3, Px(22)), TextFormatFlags.Right);
        }
        Fila(18, "Subtotal:", Gs(v.Grav));
        Fila(40, "IVA (10%):", Gs(v.Iva));
        Fila(62, "Exento:", Gs(v.Exc));
        using var lapiz = new Pen(Tema.Borde);
        g.DrawLine(lapiz, 0, Px(92), w, Px(92));

        // Con importes grandes se achica para que no se corte.
        var fuente = Fuente(27, true);
        foreach (var px in new[] { 27f, 23f, 19f, 16f })
        {
            fuente = Fuente(px, true);
            if (Ancho("Total a Pagar:", fuente) + Ancho(Gs(v.Total), fuente) + Px(12) <= w) break;
        }
        Texto(g, "Total a Pagar:", fuente, Tema.Texto, new Rectangle(0, Px(99), w, Px(40)));
        Texto(g, Gs(v.Total), fuente, Tema.Texto, new Rectangle(0, Px(99), w, Px(40)), TextFormatFlags.Right);
    }

    #endregion

    #region Sucursal, vendedor y cliente

    private string[] TextosContexto() =>
    [
        "Sucursal: " + (_sucursal.Id == 0 ? "Elegir" : _sucursal.Nombre),
        "Vendedor: " + (_empleado.Id == 0 ? "Elegir" : _empleado.Nombre),
        "Cliente: " + (_cliente.Id == 0 ? "Elegir" : _cliente.Nombre)
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
        var ids = new[] { _sucursal.Id, _empleado.Id, _cliente.Id };
        for (int i = 0; i < 3; i++)
        {
            Texto(g, textos[i], FuenteContexto(), ids[i] == 0 ? Azul : Tema.Texto, areas[i]);
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
        var posicion = new Point(Math.Min(areas[indice].X, pnlContexto.Width - Px(300)), pnlContexto.Height);
        try
        {
            switch (indice)
            {
                case 0:
                    Selector.Mostrar(pnlContexto, posicion, _sucursales.ListarTodas(), ElegirSucursal);
                    break;
                case 1:
                    Selector.Mostrar(pnlContexto, posicion, _empleados.ListarCombo(_empresa.Id, _empleado.Id), f =>
                    {
                        _empleado = (Convert.ToInt32(f["id"]), $"{f["descripcion"]}");
                        pnlContexto.Invalidate();
                    });
                    break;
                default:
                    Selector.Mostrar(pnlContexto, posicion, _clientes.ListarCombo(), f =>
                    {
                        _cliente = (Convert.ToInt32(f["id"]), $"{f["descripcion"]}".Split("  -  ")[0]);
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

    /// <summary>La empresa sale de la sucursal; si cambia, el vendedor debe ser de la nueva.</summary>
    private void ElegirSucursal(DataRow f)
    {
        int idEmpresa = Convert.ToInt32(f["id_empresa"]);
        if (idEmpresa != _empresa.Id || _empleado.Id == 0)
        {
            var vendedores = _empleados.ListarCombo(idEmpresa);
            _empleado = vendedores.Rows.Count == 0
                ? default
                : (Convert.ToInt32(vendedores.Rows[0]["id"]), $"{vendedores.Rows[0]["descripcion"]}");
        }
        _empresa = (idEmpresa, "");
        _sucursal = (Convert.ToInt32(f["id"]), $"{f["nombre"]}");
        pnlContexto.Invalidate();
    }

    #endregion
}
