using System.ComponentModel;
using System.Data;
using TallerMecanico.Datos;
using TallerMecanico.Modelos;
using TallerMecanico.Servicios;
using TallerMecanico.UI;

namespace TallerMecanico.Formularios;

/// <summary>ABM de sucursales: cada empresa puede tener N sucursales.</summary>
[DesignerCategory("Code")]
public class FrmSucursales : FrmAbmBase
{
    private readonly SucursalService _servicio = new();
    private readonly EmpresaService _empresas = new();

    private readonly ComboBox cboEmpresa = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox txtNombre = new() { MaxLength = 120 };
    private readonly TextBox txtDireccion = new() { MaxLength = 255 };
    private readonly TextBox txtTimbrado = new() { MaxLength = 50 };
    private readonly TextBox txtPunto = new() { MaxLength = 20 };
    private readonly NumericUpDown nudUltimo = new() { Maximum = 9_999_999, TextAlign = HorizontalAlignment.Right };

    public FrmSucursales()
    {
        Titulo = "Sucursales";
        Subtitulo = "Locales de cada empresa, con sus datos de facturación";
        NombreEntidad = "sucursal";
        NombreEntidadPlural = "sucursales";
        TextoAlta = "Nueva sucursal";
        TextoEdicion = "Editar sucursal";

        AgregarCampoConAlta("Empresa", cboEmpresa, "Crear una empresa nueva", CrearEmpresa, obligatorio: true);
        AgregarCampo("Nombre", txtNombre, obligatorio: true, ayuda: "Ej.: Sucursal Norte.");
        AgregarCampo("Dirección", txtDireccion);
        AgregarCampo("Timbrado", txtTimbrado);
        AgregarCampo("Punto de expedición", txtPunto, ayuda: "Ej.: 001-001.");
        AgregarCampo("Último número de factura", nudUltimo);
    }

    protected override DataTable ObtenerDatos() => Db.ConsultarTabla(@"
        SELECT s.id,
               e.nombre                          AS `Empresa`,
               s.nombre                          AS `Sucursal`,
               COALESCE(s.direccion, '')         AS `Dirección`,
               COALESCE(s.timbrado, '')          AS `Timbrado`,
               COALESCE(s.punto_expedicion, '')  AS `Punto exp.`,
               COALESCE(s.ultimo_numero, 0)      AS `Último nro.`
          FROM sucursales s
          JOIN empresas e ON e.id = s.id_empresa
      ORDER BY e.nombre, s.id");

    protected override void CargarListas() => Combos.Cargar(cboEmpresa, _empresas.ListarCombo());

    protected override void LimpiarCampos()
    {
        foreach (var t in new[] { txtNombre, txtDireccion, txtTimbrado, txtPunto }) t.Clear();
        nudUltimo.Value = 0;
        cboEmpresa.SelectedIndex = -1;
    }

    protected override void CargarRegistro(object id)
    {
        var s = _servicio.Obtener(Convert.ToInt64(id))
                ?? throw new InvalidOperationException("La sucursal ya no existe. Actualizá la pantalla.");
        Combos.Seleccionar(cboEmpresa, s.IdEmpresa);
        txtNombre.Text = s.Nombre;
        txtDireccion.Text = s.Direccion ?? "";
        txtTimbrado.Text = s.Timbrado ?? "";
        txtPunto.Text = s.PuntoExpedicion ?? "";
        nudUltimo.Value = Math.Clamp(s.UltimoNumero, 0, (int)nudUltimo.Maximum);
    }

    protected override bool ValidarCampos()
    {
        if (Combos.Valor(cboEmpresa) is not int empresa)
            return MarcarError(cboEmpresa, "Seleccioná la empresa.");
        var nombre = txtNombre.Text.Trim();
        if (nombre.Length == 0) return MarcarError(txtNombre, "Ingresá el nombre de la sucursal.");

        var repetidos = Db.Escalar(@"
            SELECT COUNT(*) FROM sucursales
             WHERE id_empresa = @e AND UPPER(TRIM(nombre)) = UPPER(@n) AND (@id IS NULL OR id <> @id)",
            Db.P("@e", empresa), Db.P("@n", nombre), Db.P("@id", IdActual));
        if (Convert.ToInt32(repetidos) > 0)
            return MarcarError(txtNombre, "Esa empresa ya tiene una sucursal con ese nombre.");
        return true;
    }

    protected override object? Guardar(object? id)
    {
        var sucursal = new Sucursal
        {
            Id = id is null ? 0 : Convert.ToInt64(id),
            IdEmpresa = Combos.Valor(cboEmpresa) ?? 0,
            Nombre = txtNombre.Text.Trim(),
            Direccion = Texto.ONulo(txtDireccion.Text),
            Timbrado = Texto.ONulo(txtTimbrado.Text),
            PuntoExpedicion = Texto.ONulo(txtPunto.Text),
            UltimoNumero = (int)nudUltimo.Value
        };
        return (int)_servicio.Procesar(id is null ? Operacion.Crear : Operacion.Editar, sucursal);
    }

    protected override void Eliminar(object id) => _servicio.Eliminar(Convert.ToInt64(id));

    protected override string DescribirSeleccion() =>
        $"{dgvDatos.CurrentRow?.Cells["Sucursal"].Value} ({dgvDatos.CurrentRow?.Cells["Empresa"].Value})";

    private void CrearEmpresa()
    {
        using var frm = new FrmEmpresas();
        if (frm.CrearEnDialogo(TopLevelControl) is { } nuevo)
        {
            CargarListas();
            Combos.Seleccionar(cboEmpresa, nuevo);
        }
    }
}
