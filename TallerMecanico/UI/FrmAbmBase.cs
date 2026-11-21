using System.ComponentModel;
using System.Data;
using System.Text;
using TallerMecanico.Datos;

namespace TallerMecanico.UI;

/// <summary>
/// Formulario base para todos los ABM (alta, baja, modificacion) del sistema.
///
/// Ya trae resuelto: diseno, grilla con busqueda, panel lateral de edicion,
/// botones, atajos de teclado, confirmaciones, mensajes de error y estado.
///
/// Para crear un ABM nuevo:
///   1. Crear un Form que herede de FrmAbmBase.
///   2. En el constructor: Titulo, Subtitulo, NombreEntidad y agregar los
///      campos con AgregarCampo(...).
///   3. Sobrescribir ObtenerDatos, LimpiarCampos, CargarRegistro,
///      ValidarCampos, Guardar y Eliminar.
/// Ver Formularios/FrmVehiculos.cs como ejemplo completo.
/// </summary>
public partial class FrmAbmBase : Form
{
    protected enum ModoAbm { Consulta, Alta, Edicion }

    private DataTable? _tabla;

    public FrmAbmBase()
    {
        InitializeComponent();
        AplicarTema();
        ConectarEventos();
        CambiarModo(ModoAbm.Consulta);
    }

    #region Configuracion (la setean los formularios hijos)

    /// <summary>Titulo grande del encabezado.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Titulo
    {
        get => lblTitulo.Text;
        set { lblTitulo.Text = value; Text = value; }
    }

    /// <summary>Texto descriptivo debajo del titulo.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Subtitulo
    {
        get => lblSubtitulo.Text;
        set => lblSubtitulo.Text = value;
    }

    /// <summary>Nombre de la columna clave en el DataTable de ObtenerDatos (se oculta en la grilla).</summary>
    protected string ColumnaId { get; set; } = "id";

    /// <summary>Nombre en singular y plural, para mensajes ("vehiculo" / "vehiculos").</summary>
    protected string NombreEntidad { get; set; } = "registro";
    protected string NombreEntidadPlural { get; set; } = "registros";

    /// <summary>Titulos del panel de edicion. Ej.: "Nueva marca" / "Editar marca".</summary>
    protected string TextoAlta { get; set; } = "Nuevo registro";
    protected string TextoEdicion { get; set; } = "Editar registro";

    /// <summary>Poner en false si la entidad no se puede eliminar.</summary>
    protected bool PermiteEliminar { get; set; } = true;

    protected ModoAbm Modo { get; private set; } = ModoAbm.Consulta;

    /// <summary>Id del registro que se esta editando (null en un alta).</summary>
    protected object? IdActual { get; private set; }

    /// <summary>Id de la fila seleccionada en la grilla.</summary>
    protected object? IdSeleccionado =>
        dgvDatos.CurrentRow?.DataBoundItem is DataRowView fila && _tabla?.Columns.Contains(ColumnaId) == true
            ? fila[ColumnaId]
            : null;

    protected static bool EsModoDiseno =>
        LicenseManager.UsageMode == LicenseUsageMode.Designtime;

    #endregion

    #region Metodos para sobrescribir en cada ABM

    /// <summary>Devuelve los datos de la grilla. Los nombres de columna son los titulos.</summary>
    protected virtual DataTable ObtenerDatos() => new();

    /// <summary>Carga combos u otros datos auxiliares (se llama al abrir y al refrescar).</summary>
    protected virtual void CargarListas() { }

    /// <summary>Deja los campos vacios para un alta.</summary>
    protected virtual void LimpiarCampos() { }

    /// <summary>Carga en los campos el registro con ese id.</summary>
    protected virtual void CargarRegistro(object id) { }

    /// <summary>Valida los campos. Usar MarcarError(...) para indicar el problema.</summary>
    protected virtual bool ValidarCampos() => true;

    /// <summary>Inserta (id == null) o actualiza. Devuelve el id guardado.</summary>
    protected virtual object? Guardar(object? id) => id;

    /// <summary>Elimina el registro.</summary>
    protected virtual void Eliminar(object id) { }

    /// <summary>Texto que identifica a la fila seleccionada en la confirmacion de borrado.</summary>
    protected virtual string DescribirSeleccion()
    {
        if (dgvDatos.CurrentRow is null) return "";
        var columna = dgvDatos.Columns.Cast<DataGridViewColumn>().FirstOrDefault(c => c.Visible);
        return columna is null ? "" : Convert.ToString(dgvDatos.CurrentRow.Cells[columna.Index].Value) ?? "";
    }

    /// <summary>Permite ajustar anchos/formatos de columnas despues de cargar la grilla.</summary>
    protected virtual void AjustarColumnas(DataGridView grilla) { }

    #endregion

    #region Ayudas para los formularios hijos

    /// <summary>
    /// Agrega un campo (etiqueta + control) al panel de edicion, uno debajo del otro.
    /// </summary>
    protected T AgregarCampo<T>(string etiqueta, T control, bool obligatorio = false,
                                int alto = 0, string? ayuda = null) where T : Control
    {
        int ancho = pnlCampos.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 4;

        var lbl = new Label
        {
            AutoSize = false,
            Width = ancho,
            Height = 20,
            Margin = new Padding(0, pnlCampos.Controls.Count == 0 ? 4 : 12, 0, 4),
            Text = obligatorio ? etiqueta + "  *" : etiqueta,
            Font = Tema.FuenteEtiqueta,
            ForeColor = Tema.TextoSuave
        };
        pnlCampos.Controls.Add(lbl);

        control.Width = ancho - 18; // lugar para el icono de error
        if (alto > 0) control.Height = alto;
        control.Margin = new Padding(0);
        Tema.Entrada(control);
        pnlCampos.Controls.Add(control);
        errorProvider.SetIconPadding(control, 4);

        if (!string.IsNullOrWhiteSpace(ayuda))
        {
            pnlCampos.Controls.Add(new Label
            {
                AutoSize = false,
                Width = ancho,
                Height = 18,
                Margin = new Padding(0, 3, 0, 0),
                Text = ayuda,
                Font = Tema.FuenteChica,
                ForeColor = Tema.TextoSuave
            });
        }

        return control;
    }

    /// <summary>Marca un campo con error, le pasa el foco y devuelve false.</summary>
    protected bool MarcarError(Control control, string mensaje)
    {
        errorProvider.SetError(control, mensaje);
        lblMensajeEdicion.Text = "⚠  " + mensaje;
        lblMensajeEdicion.Visible = true;
        control.Focus();
        return false;
    }

    /// <summary>Mensaje de exito temporal en la barra de estado.</summary>
    protected void Notificar(string mensaje)
    {
        lblEstado.ForeColor = Tema.Exito;
        lblEstado.Text = "✓  " + mensaje;
        tmrEstado.Stop();
        tmrEstado.Start();
    }

    protected void MostrarError(string contexto, Exception ex) =>
        MessageBox.Show(this, $"{contexto}\n\n{Db.TraducirError(ex)}", Titulo,
            MessageBoxButtons.OK, MessageBoxIcon.Error);

    /// <summary>Recarga la grilla y deja seleccionada la fila indicada (o la actual).</summary>
    protected void RefrescarGrilla(object? idSeleccionar = null)
    {
        idSeleccionar ??= IdSeleccionado;
        try
        {
            UseWaitCursor = true;
            CargarListas();
            _tabla = ObtenerDatos();
            dgvDatos.DataSource = _tabla;
            ConfigurarColumnas();
            AplicarFiltro();
            SeleccionarFila(idSeleccionar);
        }
        catch (Exception ex)
        {
            MostrarError($"No se pudieron cargar los {NombreEntidadPlural}.", ex);
        }
        finally
        {
            UseWaitCursor = false;
            ActualizarEstado();
            ActualizarBotones();
        }
    }

    #endregion

    #region Ciclo de vida

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        if (EsModoDiseno || DesignMode) return;

        tipAyuda.SetToolTip(btnNuevo, "Nuevo (Ctrl+N)");
        tipAyuda.SetToolTip(btnEditar, "Editar el seleccionado (Enter o doble clic)");
        tipAyuda.SetToolTip(btnEliminar, "Eliminar el seleccionado (Supr)");
        tipAyuda.SetToolTip(btnRefrescar, "Volver a leer los datos (F5)");
        RefrescarGrilla();
    }

    private void AplicarTema()
    {
        BackColor = Tema.Fondo;
        ForeColor = Tema.Texto;

        pnlHeader.BackColor = Tema.Superficie;
        lblTitulo.Font = Tema.FuenteTitulo;
        lblTitulo.ForeColor = Tema.Texto;
        lblSubtitulo.Font = Tema.Fuente;
        lblSubtitulo.ForeColor = Tema.TextoSuave;

        pnlToolbar.BackColor = Tema.Superficie;
        pnlSeparadorToolbar.BackColor = Tema.Borde;
        pnlSeparadorEdicion.BackColor = Tema.Borde;
        Tema.Boton(btnNuevo, Tema.EstiloBoton.Primario);
        Tema.Boton(btnEditar, Tema.EstiloBoton.Secundario);
        Tema.Boton(btnEliminar, Tema.EstiloBoton.Peligro);
        Tema.Boton(btnRefrescar, Tema.EstiloBoton.Secundario);
        Tema.Entrada(txtBuscar);

        pnlGrid.BackColor = Tema.Fondo;
        Tema.Grilla(dgvDatos);

        pnlFooter.BackColor = Tema.Fondo;
        lblEstado.ForeColor = Tema.TextoSuave;
        lblEstado.Font = Tema.Fuente;

        pnlEdicion.BackColor = Tema.Superficie;
        lblEdicionTitulo.Font = Tema.FuenteSubtitulo;
        lblEdicionTitulo.ForeColor = Tema.Texto;
        pnlCampos.BackColor = Tema.Superficie;
        lblMensajeEdicion.BackColor = Tema.PeligroSuave;
        lblMensajeEdicion.ForeColor = Tema.Peligro;
        lblMensajeEdicion.Font = Tema.Fuente;
        Tema.Boton(btnGuardar, Tema.EstiloBoton.Primario);
        Tema.Boton(btnCancelar, Tema.EstiloBoton.Secundario);
    }

    private void ConectarEventos()
    {
        btnNuevo.Click += (_, _) => IniciarAlta();
        btnEditar.Click += (_, _) => IniciarEdicion();
        btnEliminar.Click += (_, _) => EliminarSeleccionado();
        btnRefrescar.Click += (_, _) => { RefrescarGrilla(); Notificar("Datos actualizados."); };
        btnGuardar.Click += (_, _) => GuardarActual();
        btnCancelar.Click += (_, _) => CambiarModo(ModoAbm.Consulta);

        dgvDatos.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) IniciarEdicion(); };
        dgvDatos.SelectionChanged += (_, _) => ActualizarBotones();

        txtBuscar.TextChanged += (_, _) => { tmrBuscar.Stop(); tmrBuscar.Start(); };
        tmrBuscar.Tick += (_, _) => { tmrBuscar.Stop(); AplicarFiltro(); ActualizarEstado(); ActualizarBotones(); };
        tmrEstado.Tick += (_, _) => { tmrEstado.Stop(); ActualizarEstado(); };
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        bool consulta = Modo == ModoAbm.Consulta;
        switch (keyData)
        {
            case Keys.Control | Keys.N when consulta:
                IniciarAlta();
                return true;
            case Keys.Control | Keys.S when !consulta:
                GuardarActual();
                return true;
            case Keys.Escape when !consulta:
                CambiarModo(ModoAbm.Consulta);
                return true;
            case Keys.Escape when txtBuscar.Text.Length > 0:
                txtBuscar.Clear();
                return true;
            case Keys.F5 when consulta:
                RefrescarGrilla();
                return true;
            case Keys.Control | Keys.F when consulta:
                txtBuscar.Focus();
                txtBuscar.SelectAll();
                return true;
            case Keys.Enter when consulta && dgvDatos.Focused:
                IniciarEdicion();
                return true;
            case Keys.Delete when consulta && dgvDatos.Focused:
                EliminarSeleccionado();
                return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    #endregion

    #region Flujo del ABM

    private void IniciarAlta()
    {
        if (Modo != ModoAbm.Consulta) return;
        LimpiarErrores();
        LimpiarCampos();
        IdActual = null;
        CambiarModo(ModoAbm.Alta);
    }

    private void IniciarEdicion()
    {
        if (Modo != ModoAbm.Consulta) return;
        var id = IdSeleccionado;
        if (id is null) return;

        LimpiarErrores();
        LimpiarCampos();
        try
        {
            CargarRegistro(id);
        }
        catch (Exception ex)
        {
            MostrarError($"No se pudo cargar el {NombreEntidad}.", ex);
            return;
        }
        IdActual = id;
        CambiarModo(ModoAbm.Edicion);
    }

    private void GuardarActual()
    {
        if (Modo == ModoAbm.Consulta) return;
        LimpiarErrores();
        try
        {
            if (!ValidarCampos()) return;

            UseWaitCursor = true;
            bool esAlta = Modo == ModoAbm.Alta;
            var id = Guardar(IdActual);
            CambiarModo(ModoAbm.Consulta);
            RefrescarGrilla(id ?? IdActual);
            Notificar(esAlta ? "Registro creado correctamente." : "Cambios guardados correctamente.");
        }
        catch (Exception ex)
        {
            MostrarError($"No se pudo guardar el {NombreEntidad}.", ex);
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    private void EliminarSeleccionado()
    {
        if (Modo != ModoAbm.Consulta || !PermiteEliminar) return;
        var id = IdSeleccionado;
        if (id is null) return;

        var respuesta = MessageBox.Show(this,
            $"¿Eliminar {NombreEntidad} «{DescribirSeleccion()}»?\n\nEsta acción no se puede deshacer.",
            "Confirmar eliminación", MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);
        if (respuesta != DialogResult.Yes) return;

        try
        {
            Eliminar(id);
            RefrescarGrilla();
            Notificar("Registro eliminado.");
        }
        catch (Exception ex)
        {
            MostrarError($"No se pudo eliminar el {NombreEntidad}.", ex);
        }
    }

    private void CambiarModo(ModoAbm modo)
    {
        Modo = modo;
        bool editando = modo != ModoAbm.Consulta;

        pnlEdicion.Visible = editando;
        pnlSeparadorEdicion.Visible = editando;
        lblEdicionTitulo.Text = modo == ModoAbm.Alta ? TextoAlta : TextoEdicion;
        dgvDatos.Enabled = !editando;
        txtBuscar.Enabled = !editando;
        if (!editando)
        {
            LimpiarErrores();
            IdActual = null;
            dgvDatos.Focus();
        }
        else
        {
            BeginInvoke(EnfocarPrimerCampo);
        }
        ActualizarBotones();
    }

    private void EnfocarPrimerCampo()
    {
        var primero = pnlCampos.Controls.Cast<Control>()
            .FirstOrDefault(c => c is not Label && c.Enabled && c.CanFocus);
        primero?.Focus();
    }

    private void LimpiarErrores()
    {
        errorProvider.Clear();
        lblMensajeEdicion.Visible = false;
    }

    private void ActualizarBotones()
    {
        bool consulta = Modo == ModoAbm.Consulta;
        bool haySeleccion = IdSeleccionado is not null && dgvDatos.SelectedRows.Count > 0;
        btnNuevo.Enabled = consulta;
        btnEditar.Enabled = consulta && haySeleccion;
        btnEliminar.Enabled = consulta && haySeleccion && PermiteEliminar;
        btnEliminar.Visible = PermiteEliminar;
        btnRefrescar.Enabled = consulta;
    }

    private void ActualizarEstado()
    {
        lblEstado.ForeColor = Tema.TextoSuave;
        if (_tabla is null)
        {
            lblEstado.Text = "Sin datos";
            return;
        }
        int total = _tabla.Rows.Count;
        int visibles = _tabla.DefaultView.Count;
        string nombre = total == 1 ? NombreEntidad : NombreEntidadPlural;
        lblEstado.Text = visibles == total
            ? $"{total} {nombre}"
            : $"Mostrando {visibles} de {total} {NombreEntidadPlural}";
    }

    private void ConfigurarColumnas()
    {
        foreach (DataGridViewColumn col in dgvDatos.Columns)
        {
            col.SortMode = DataGridViewColumnSortMode.Automatic;
            if (string.Equals(col.DataPropertyName, ColumnaId, StringComparison.OrdinalIgnoreCase))
            {
                col.Visible = false;
                continue;
            }
            var tipo = _tabla?.Columns[col.DataPropertyName]?.DataType;
            if (tipo == typeof(decimal) || tipo == typeof(double))
            {
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                col.DefaultCellStyle.Format = "N2";
            }
        }
        AjustarColumnas(dgvDatos);
    }

    /// <summary>Filtra en memoria por todas las columnas. Varias palabras = deben estar todas.</summary>
    private void AplicarFiltro()
    {
        if (_tabla is null) return;
        var palabras = txtBuscar.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (palabras.Length == 0)
        {
            _tabla.DefaultView.RowFilter = "";
            return;
        }

        var columnas = _tabla.Columns.Cast<DataColumn>()
            .Where(c => !string.Equals(c.ColumnName, ColumnaId, StringComparison.OrdinalIgnoreCase))
            .Select(c => $"CONVERT([{c.ColumnName.Replace("]", "\\]")}], 'System.String')")
            .ToList();
        if (columnas.Count == 0) return;

        var condiciones = palabras.Select(p =>
        {
            var valor = EscaparLike(p);
            return "(" + string.Join(" OR ", columnas.Select(c => $"{c} LIKE '%{valor}%'")) + ")";
        });
        _tabla.DefaultView.RowFilter = string.Join(" AND ", condiciones);
    }

    private static string EscaparLike(string texto)
    {
        var sb = new StringBuilder();
        foreach (var ch in texto)
        {
            sb.Append(ch switch
            {
                '[' => "[[]",
                ']' => "[]]",
                '*' => "[*]",
                '%' => "[%]",
                '\'' => "''",
                _ => ch.ToString()
            });
        }
        return sb.ToString();
    }

    private void SeleccionarFila(object? id)
    {
        if (id is null) return;
        var columnaVisible = dgvDatos.Columns.Cast<DataGridViewColumn>().FirstOrDefault(c => c.Visible);
        if (columnaVisible is null) return;

        foreach (DataGridViewRow fila in dgvDatos.Rows)
        {
            if (fila.DataBoundItem is DataRowView drv && Equals(Convert.ToString(drv[ColumnaId]), Convert.ToString(id)))
            {
                dgvDatos.CurrentCell = fila.Cells[columnaVisible.Index];
                fila.Selected = true;
                return;
            }
        }
    }

    #endregion
}
