using System.ComponentModel;
using System.Data;
using TallerMecanico.Datos;
using TallerMecanico.Modelos;
using TallerMecanico.Servicios;
using TallerMecanico.UI;

namespace TallerMecanico.Formularios;

/// <summary>ABM de clientes del taller.</summary>
[DesignerCategory("Code")]
public class FrmClientes : FrmAbmBase
{
    private readonly ClienteService _servicio = new();
    private readonly EmpresaService _empresas = new();
    private readonly SucursalService _sucursales = new();

    private readonly TextBox txtNombre = new() { MaxLength = 100 };
    private readonly TextBox txtApellido = new() { MaxLength = 100 };
    private readonly TextBox txtDocumento = new() { MaxLength = 30 };
    private readonly TextBox txtDv = new() { MaxLength = 5 };
    private readonly DateTimePicker dtpNacimiento = new()
    {
        Format = DateTimePickerFormat.Short,
        ShowCheckBox = true,
        Checked = false
    };
    private readonly TextBox txtTelefono = new() { MaxLength = 30 };
    private readonly TextBox txtCorreo = new() { MaxLength = 120 };
    private readonly TextBox txtDireccion = new() { MaxLength = 255 };
    private readonly ComboBox cboEmpresa = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox cboSucursal = new() { DropDownStyle = ComboBoxStyle.DropDownList };

    public FrmClientes()
    {
        Titulo = "Clientes";
        Subtitulo = "Personas y empresas que traen vehículos al taller";
        NombreEntidad = "cliente";
        NombreEntidadPlural = "clientes";
        TextoAlta = "Nuevo cliente";
        TextoEdicion = "Editar cliente";

        AgregarCampo("Nombre", txtNombre, obligatorio: true);
        AgregarCampo("Apellido", txtApellido, obligatorio: true);
        AgregarCampo("Nro. de documento / RUC", txtDocumento);
        AgregarCampo("DV", txtDv);
        AgregarCampo("Fecha de nacimiento", dtpNacimiento, ayuda: "Tildá la casilla para cargarla.");
        AgregarCampo("Teléfono", txtTelefono);
        AgregarCampo("Correo", txtCorreo);
        AgregarCampo("Dirección", txtDireccion);
        AgregarCampo("Empresa", cboEmpresa);
        AgregarCampo("Sucursal", cboSucursal);

        cboEmpresa.SelectionChangeCommitted += (_, _) => CargarSucursales();
    }

    protected override DataTable ObtenerDatos() => Db.ConsultarTabla(@"
        SELECT c.id,
               CONCAT(c.apellido, ', ', c.nombre)   AS `Cliente`,
               COALESCE(c.nro_documento, '')        AS `Documento`,
               COALESCE(c.telefono, '')             AS `Teléfono`,
               COALESCE(c.correo, '')               AS `Correo`,
               COALESCE(e.nombre, '')               AS `Empresa`,
               COALESCE(s.nombre, '')               AS `Sucursal`
          FROM clientes c
     LEFT JOIN empresas e   ON e.id = c.id_empresa
     LEFT JOIN sucursales s ON s.id = c.id_sucursal
      ORDER BY c.apellido, c.nombre");

    protected override void CargarListas()
    {
        Combos.Cargar(cboEmpresa, _empresas.ListarCombo(), Combos.SinEspecificar);
        CargarSucursales();
    }

    private void CargarSucursales()
    {
        var datos = Combos.Valor(cboEmpresa) is int empresa
            ? _sucursales.ListarCombo(empresa)
            : TablaVacia();
        Combos.Cargar(cboSucursal, datos, Combos.SinEspecificar);
    }

    private static DataTable TablaVacia()
    {
        var t = new DataTable();
        t.Columns.Add("id", typeof(int));
        t.Columns.Add("descripcion", typeof(string));
        return t;
    }

    protected override void LimpiarCampos()
    {
        foreach (var t in new[] { txtNombre, txtApellido, txtDocumento, txtDv, txtTelefono, txtCorreo, txtDireccion })
            t.Clear();
        dtpNacimiento.Value = DateTime.Today;
        dtpNacimiento.Checked = false;
        Combos.Seleccionar(cboEmpresa, null);
        CargarSucursales();
    }

    protected override void CargarRegistro(object id)
    {
        var c = _servicio.Obtener(Convert.ToInt64(id))
                ?? throw new InvalidOperationException("El cliente ya no existe. Actualizá la pantalla.");
        txtNombre.Text = c.Nombre;
        txtApellido.Text = c.Apellido;
        txtDocumento.Text = c.NroDocumento ?? "";
        txtDv.Text = c.Dv ?? "";
        dtpNacimiento.Value = c.Nacimiento ?? DateTime.Today;
        dtpNacimiento.Checked = c.Nacimiento is not null;
        txtTelefono.Text = c.Telefono ?? "";
        txtCorreo.Text = c.Correo ?? "";
        txtDireccion.Text = c.Direccion ?? "";
        Combos.Seleccionar(cboEmpresa, c.IdEmpresa);
        CargarSucursales();
        Combos.Seleccionar(cboSucursal, c.IdSucursal);
    }

    protected override bool ValidarCampos()
    {
        if (txtNombre.Text.Trim().Length == 0) return MarcarError(txtNombre, "Ingresá el nombre.");
        if (txtApellido.Text.Trim().Length == 0) return MarcarError(txtApellido, "Ingresá el apellido.");
        var correo = txtCorreo.Text.Trim();
        if (correo.Length > 0 && (!correo.Contains('@') || !correo.Contains('.')))
            return MarcarError(txtCorreo, "El correo no parece válido.");
        if (dtpNacimiento.Checked && dtpNacimiento.Value.Date > DateTime.Today)
            return MarcarError(dtpNacimiento, "La fecha de nacimiento no puede ser futura.");

        var documento = txtDocumento.Text.Trim();
        if (documento.Length > 0)
        {
            var repetidos = Db.Escalar(
                "SELECT COUNT(*) FROM clientes WHERE nro_documento = @doc AND (@id IS NULL OR id <> @id)",
                Db.P("@doc", documento), Db.P("@id", IdActual));
            if (Convert.ToInt32(repetidos) > 0)
                return MarcarError(txtDocumento, "Ya hay un cliente con ese documento.");
        }
        return true;
    }

    protected override object? Guardar(object? id)
    {
        var cliente = new Cliente
        {
            Id = id is null ? 0 : Convert.ToInt64(id),
            Nombre = txtNombre.Text.Trim(),
            Apellido = txtApellido.Text.Trim(),
            NroDocumento = Texto.ONulo(txtDocumento.Text),
            Dv = Texto.ONulo(txtDv.Text),
            Nacimiento = dtpNacimiento.Checked ? dtpNacimiento.Value.Date : null,
            Telefono = Texto.ONulo(txtTelefono.Text),
            Correo = Texto.ONulo(txtCorreo.Text),
            Direccion = Texto.ONulo(txtDireccion.Text),
            IdEmpresa = Combos.Valor(cboEmpresa),
            IdSucursal = Combos.Valor(cboSucursal)
        };
        return (int)_servicio.Procesar(id is null ? Operacion.Crear : Operacion.Editar, cliente);
    }

    protected override void Eliminar(object id) => _servicio.Eliminar(Convert.ToInt64(id));

    protected override string DescribirSeleccion() =>
        Convert.ToString(dgvDatos.CurrentRow?.Cells["Cliente"].Value) ?? "";
}
