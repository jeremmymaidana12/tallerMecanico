using System.ComponentModel;
using System.Data;
using TallerMecanico.Modelos;
using TallerMecanico.Servicios;
using TallerMecanico.UI;

namespace TallerMecanico.Formularios;

/// <summary>ABM de ventas (cabecera + detalle). Hereda el diseno y comportamiento de FrmAbmBase.</summary>
public partial class FrmVentas : FrmAbmBase
{
    private readonly VentaService _ventas = new();
    private readonly ClienteService _clientes = new();
    private readonly EmpleadoService _empleados = new();
    private readonly EmpresaService _empresas = new();
    private readonly SucursalService _sucursales = new();
    private readonly ArticuloService _articulos = new();

    private readonly BindingList<VentaDetalle> _detalles = [];

    private readonly ComboBox cboEmpresa = CrearCombo();
    private readonly ComboBox cboSucursal = CrearCombo();
    private readonly ComboBox cboEmpleado = CrearCombo();
    private readonly ComboBox cboCliente = CrearCombo();
    private readonly ComboBox cboArticulo = CrearCombo();
    private readonly NumericUpDown nudCantidad = new()
    {
        DecimalPlaces = 0, Minimum = 1, Maximum = 999_999_999, Value = 1
    };
    private readonly NumericUpDown nudPrecio = new()
    {
        DecimalPlaces = 2, Minimum = 0, Maximum = 9_999_999_999, ThousandsSeparator = true
    };
    private readonly CheckBox chkExenta = new() { Text = "Exenta de IVA", AutoSize = true };
    private readonly Button btnAgregar = new() { Text = "＋  Agregar" };
    private readonly Button btnQuitar = new() { Text = "Quitar línea" };
    private readonly DataGridView dgvDetalle = new();
    private readonly Label lblTotales = new() { AutoSize = false, TextAlign = ContentAlignment.MiddleRight };

    public FrmVentas()
    {
        InitializeComponent();

        Titulo = "Ventas";
        Subtitulo = "Registro de ventas de artículos";
        NombreEntidad = "venta";
        NombreEntidadPlural = "ventas";
        TextoAlta = "Nueva venta";
        TextoEdicion = "Editar venta";

        // El detalle necesita un panel mas ancho que el de los ABM simples.
        pnlEdicion.Width = 540;
        pnlEdicion.PerformLayout();

        AgregarCampo("Empresa", cboEmpresa, obligatorio: true);
        AgregarCampo("Sucursal", cboSucursal, obligatorio: true);
        AgregarCampo("Empleado", cboEmpleado, obligatorio: true);
        AgregarCampo("Cliente", cboCliente, obligatorio: true);
        AgregarCampo("Artículo", cboArticulo);
        AgregarCampo("Cantidad / Precio (IVA incluido)", CrearFilaCarga(), alto: 70);
        ConfigurarDetalle();
        AgregarCampo("Detalle", dgvDetalle, obligatorio: true, alto: 170);
        AgregarCampo("Totales", lblTotales, alto: 44);
        lblTotales.Font = Tema.FuenteNegrita;

        Tema.Boton(btnAgregar, Tema.EstiloBoton.Primario);
        Tema.Boton(btnQuitar, Tema.EstiloBoton.Peligro);

        cboEmpresa.SelectedIndexChanged += (_, _) => CargarDependientes(null);
        cboArticulo.SelectedIndexChanged += (_, _) =>
            nudPrecio.Value = cboArticulo.SelectedItem is DataRowView f ? Convert.ToDecimal(f["precio"]) : 0;
        btnAgregar.Click += (_, _) => AgregarLinea();
        btnQuitar.Click += (_, _) => QuitarLinea();
        _detalles.ListChanged += (_, _) => ActualizarTotales();
        ActualizarTotales();
    }

    protected override DataTable ObtenerDatos() => _ventas.Listar();

    protected override void CargarListas()
    {
        Enlazar(cboEmpresa, _empresas.ListarCombo());
        Enlazar(cboCliente, _clientes.ListarCombo());
        Enlazar(cboArticulo, _articulos.ListarCombo());
        CargarDependientes(null);
    }

    protected override void LimpiarCampos()
    {
        // Con una sola opcion se elige sola.
        cboEmpresa.SelectedIndex = cboEmpresa.Items.Count == 1 ? 0 : -1;
        CargarDependientes(null);
        cboCliente.SelectedIndex = -1;
        LimpiarLinea();
        _detalles.Clear();
    }

    protected override void CargarRegistro(object id)
    {
        var v = _ventas.Obtener(Convert.ToInt64(id))
                ?? throw new InvalidOperationException("La venta ya no existe. Actualizá la pantalla.");

        cboEmpresa.SelectedValue = v.IdEmpresa;
        CargarDependientes(v.IdEmpleado);
        cboSucursal.SelectedValue = v.IdSucursal;
        cboEmpleado.SelectedValue = v.IdEmpleado;
        cboCliente.SelectedValue = v.IdCliente;
        foreach (var d in v.Detalles) _detalles.Add(d);
    }

    protected override bool ValidarCampos()
    {
        if (Seleccionado(cboEmpresa) is null)
            return MarcarError(cboEmpresa, "Seleccioná la empresa.");
        if (Seleccionado(cboSucursal) is null)
            return MarcarError(cboSucursal, "Seleccioná la sucursal.");
        if (Seleccionado(cboEmpleado) is null)
            return MarcarError(cboEmpleado, "Seleccioná el empleado.");
        if (Seleccionado(cboCliente) is null)
            return MarcarError(cboCliente, "Seleccioná el cliente.");
        if (Seleccionado(cboArticulo) is not null)
            return MarcarError(cboArticulo, "Hay un artículo elegido sin agregar al detalle.");
        if (_detalles.Count == 0)
            return MarcarError(dgvDetalle, "Agregá al menos un artículo.");
        return true;
    }

    protected override object? Guardar(object? id)
    {
        var venta = new VentaCabecera
        {
            Id = id is null ? 0 : Convert.ToInt64(id),
            IdEmpresa = Seleccionado(cboEmpresa)!.Value,
            IdSucursal = Seleccionado(cboSucursal)!.Value,
            IdEmpleado = Seleccionado(cboEmpleado)!.Value,
            IdCliente = Seleccionado(cboCliente)!.Value,
            Detalles = [.. _detalles]
        };
        return _ventas.Procesar(id is null ? Operacion.Crear : Operacion.Editar, venta);
    }

    protected override void Eliminar(object id) =>
        _ventas.Procesar(Operacion.Eliminar, new VentaCabecera { Id = Convert.ToInt64(id) });

    protected override string DescribirSeleccion() =>
        $"Nro. {dgvDatos.CurrentRow?.Cells["Nro."].Value} - {dgvDatos.CurrentRow?.Cells["Cliente"].Value}";

    protected override void AjustarColumnas(DataGridView grilla)
    {
        void Peso(string columna, float peso, int minimo)
        {
            if (grilla.Columns[columna] is not { } c) return;
            c.FillWeight = peso;
            c.MinimumWidth = minimo;
        }
        Peso("Nro.", 45, 70);
        Peso("Fecha", 95, 135);
        Peso("Cliente", 120, 130);
        Peso("Empleado", 100, 110);
        Peso("Empresa", 90, 100);
        Peso("Sucursal", 85, 100);
        Peso("Gravado", 80, 110);
        Peso("Exento", 70, 100);
        Peso("IVA", 70, 100);
        Peso("Total", 85, 115);

        if (grilla.Columns["Fecha"] is { } fecha)
            fecha.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
        if (grilla.Columns["Total"] is { } total)
            total.DefaultCellStyle.Font = Tema.FuenteNegrita;
        // En orden numerico "Nro." no debe mostrarse con decimales.
        if (grilla.Columns["Nro."] is { } nro)
            nro.DefaultCellStyle.Format = "N0";
    }

    #region Detalle

    private Panel CrearFilaCarga()
    {
        var panel = new Panel();
        nudCantidad.SetBounds(0, 0, 110, 25);
        nudPrecio.SetBounds(118, 0, 150, 25);
        chkExenta.Location = new Point(280, 3);
        btnAgregar.SetBounds(0, 34, 120, 34);
        btnQuitar.SetBounds(128, 34, 120, 34);
        panel.Controls.AddRange([nudCantidad, nudPrecio, chkExenta, btnAgregar, btnQuitar]);
        return panel;
    }

    private void ConfigurarDetalle()
    {
        void Columna(string propiedad, string titulo, float peso, string? formato)
        {
            var columna = new DataGridViewTextBoxColumn
            {
                DataPropertyName = propiedad,
                HeaderText = titulo,
                FillWeight = peso
            };
            if (formato is not null)
            {
                columna.DefaultCellStyle.Format = formato;
                columna.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
            dgvDetalle.Columns.Add(columna);
        }

        Tema.Grilla(dgvDetalle);
        dgvDetalle.DefaultCellStyle.Padding = new Padding(4, 0, 4, 0);
        dgvDetalle.ColumnHeadersDefaultCellStyle.Padding = new Padding(4, 0, 4, 0);
        dgvDetalle.ColumnHeadersHeight = 32;
        dgvDetalle.RowTemplate.Height = 28;
        dgvDetalle.AutoGenerateColumns = false;
        Columna(nameof(VentaDetalle.Articulo), "Artículo", 130, null);
        Columna(nameof(VentaDetalle.Cantidad), "Cant.", 60, "0.###");
        Columna(nameof(VentaDetalle.Precio), "Precio", 90, "N2");
        Columna(nameof(VentaDetalle.Iva), "IVA", 75, "N2");
        Columna(nameof(VentaDetalle.Subtotal), "Subtotal", 95, "N2");
        dgvDetalle.DataSource = _detalles;
    }

    private void AgregarLinea()
    {
        errorProvider.Clear();
        lblMensajeEdicion.Visible = false;
        if (cboArticulo.SelectedItem is not DataRowView articulo)
        {
            MarcarError(cboArticulo, "Seleccioná un artículo.");
            return;
        }

        var detalle = new VentaDetalle
        {
            IdArticulo = Convert.ToInt32(articulo["id"]),
            Articulo = Convert.ToString(articulo["nombre"]) ?? "",
            Cantidad = nudCantidad.Value,
            Precio = nudPrecio.Value
        };
        detalle.Calcular(chkExenta.Checked);
        _detalles.Add(detalle);
        LimpiarLinea();
        cboArticulo.Focus();
    }

    private void QuitarLinea()
    {
        if (dgvDetalle.CurrentRow?.DataBoundItem is VentaDetalle detalle)
            _detalles.Remove(detalle);
    }

    private void LimpiarLinea()
    {
        cboArticulo.SelectedIndex = -1;
        nudCantidad.Value = 1;
        nudPrecio.Value = 0;
        chkExenta.Checked = false;
    }

    private void ActualizarTotales()
    {
        var venta = new VentaCabecera { Detalles = [.. _detalles] };
        venta.CalcularTotales();
        lblTotales.Text = $"Gravado {venta.Grav:N2}   Exento {venta.Exc:N2}   IVA {venta.Iva:N2}\n" +
                          $"TOTAL {venta.Total:N2}";
    }

    #endregion

    #region Auxiliares

    /// <summary>Sucursales y empleados dependen de la empresa elegida.</summary>
    private void CargarDependientes(int? incluirEmpleado)
    {
        var idEmpresa = Seleccionado(cboEmpresa) ?? 0;
        Enlazar(cboSucursal, _sucursales.ListarCombo(idEmpresa));
        Enlazar(cboEmpleado, _empleados.ListarCombo(idEmpresa, incluirEmpleado));
        if (cboSucursal.Items.Count == 1) cboSucursal.SelectedIndex = 0;
        if (cboEmpleado.Items.Count == 1) cboEmpleado.SelectedIndex = 0;
    }

    private static ComboBox CrearCombo() => new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        DisplayMember = "descripcion",
        ValueMember = "id"
    };

    private static void Enlazar(ComboBox combo, DataTable datos)
    {
        combo.DisplayMember = "descripcion";
        combo.ValueMember = "id";
        combo.DataSource = datos;
        combo.SelectedIndex = -1;
    }

    private static int? Seleccionado(ComboBox combo) =>
        combo.SelectedItem is DataRowView fila ? Convert.ToInt32(fila["id"]) : null;

    #endregion
}
